using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;

namespace OrchardCore.BackgroundJobs;

public static class HttpBackgroundJob
{
    private static volatile int _activeJobsCount;

    // Gets the number of active background jobs that are currently running. 
    internal static int ActiveJobsCount => _activeJobsCount;

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync(string jobName, Func<ShellScope, Task> job)
    {
        var scope = ShellScope.Current;

        // Allow a job to be triggered e.g. during a tenant setup, but later on only check if the tenant is running.
        if (!scope.ShellContext.Settings.IsRunning() && !scope.ShellContext.Settings.IsInitializing())
        {
            return Task.CompletedTask;
        }

        // Can't be executed outside of an http context.
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        if (httpContextAccessor.HttpContext == null)
        {
            return Task.CompletedTask;
        }

        // Record the current logged in user.
        var userPrincipal = httpContextAccessor.HttpContext.User.Clone();

        // Increment the active jobs count to track the number of background jobs running.
        Interlocked.Increment(ref _activeJobsCount);

        // Fire and forget in an isolated child scope.
        _ = ShellScope.UsingChildScopeAsync(async scope =>
        {
            scope.RegisterBeforeDispose(scope =>
            {
                // Decrement the active jobs count when the job is disposed.
                Interlocked.Decrement(ref _activeJobsCount);
            }, true);

            var timeoutTask = Task.Delay(60_000);

            // Wait for the current 'HttpContext' to be released with a timeout of 60s.
            while (httpContextAccessor.HttpContext != null)
            {
                await Task.Delay(1_000);

                if (timeoutTask.IsCompleted)
                {
                    return;
                }
            }

            // Retrieve the shell context that may have been reloaded.
            var shellHost = scope.ServiceProvider.GetRequiredService<IShellHost>();
            var shellContext = await shellHost.GetOrCreateShellContextAsync(scope.ShellContext.Settings);

            // Can't be executed e.g. if a tenant setup failed.
            if (!shellContext.Settings.IsRunning())
            {
                return;
            }

            // Create a new 'HttpContext' to be used in the background.
            httpContextAccessor.HttpContext = shellContext.CreateHttpContext();

            // Restore the current user.
            httpContextAccessor.HttpContext.User = userPrincipal;

            // Here the 'IActionContextAccessor.ActionContext' need to be cleared, this 'AsyncLocal'
            // field is not cleared by 'AspnetCore' and still references the previous 'HttpContext'.
            var actionContextAccessor = scope.ServiceProvider.GetService<IActionContextAccessor>();

            if (actionContextAccessor is not null)
            {
                // Clear the stale 'ActionContext' that may be used e.g.
                // by 'ILiquidTemplateManager.RenderStringAsync()'.
                actionContextAccessor.ActionContext = null;
            }

            // Use a new scope as the shell context may have been reloaded.
            await ShellScope.UsingChildScopeAsync(async scope =>
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShellScope>>();

                try
                {
                    await job(scope);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error while executing the background job '{JobName}' after the end of the request on tenant '{TenantName}'.",
                        jobName,
                        scope.ShellContext.Settings.Name);
                }
            });

            // Clear the 'HttpContext' for this async flow.
            httpContextAccessor.HttpContext = null;
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync<T1>(string jobName, T1 item1, Func<ShellScope, T1, Task> job)
        => ExecuteAfterEndOfRequestAsync(jobName, scope => job(scope, item1));

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync<T1, T2>(string jobName, T1 item1, T2 item2, Func<ShellScope, T1, T2, Task> job)
        => ExecuteAfterEndOfRequestAsync(jobName, scope => job(scope, item1, item2));

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync<T1, T2, T3>(string jobName, T1 item1, T2 item2, T3 item3, Func<ShellScope, T1, T2, T3, Task> job)
        => ExecuteAfterEndOfRequestAsync(jobName, scope => job(scope, item1, item2, item3));

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync<T1, T2, T3, T4>(string jobName, T1 item1, T2 item2, T3 item3, T4 item4, Func<ShellScope, T1, T2, T3, T4, Task> job)
        => ExecuteAfterEndOfRequestAsync(jobName, scope => job(scope, item1, item2, item3, item4));

    /// <summary>
    /// Executes a background job in an isolated <see cref="ShellScope"/> after the current HTTP request is completed.
    /// </summary>
    public static Task ExecuteAfterEndOfRequestAsync<T1, T2, T3, T4, T5>(string jobName, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, Func<ShellScope, T1, T2, T3, T4, T5, Task> job)
        => ExecuteAfterEndOfRequestAsync(jobName, scope => job(scope, item1, item2, item3, item4, item5));
}
