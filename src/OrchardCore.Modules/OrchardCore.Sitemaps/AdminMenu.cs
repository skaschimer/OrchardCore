using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Sitemaps;

public sealed class AdminMenu : AdminNavigationProvider
{
    internal readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        if (NavigationHelper.UseLegacyFormat())
        {
            builder
               .Add(S["Configuration"], configuration => configuration
                   .Add(S["SEO"], S["SEO"].PrefixPosition(), seo => seo
                       .Permission(SitemapsPermissions.ManageSitemaps)
                       .Add(S["Sitemaps"], S["Sitemaps"].PrefixPosition("1"), sitemaps => sitemaps
                           .Action("List", "Admin", "OrchardCore.Sitemaps")
                           .LocalNav()
                       )
                       .Add(S["Sitemap Indexes"], S["Sitemap Indexes"].PrefixPosition("2"), indexes => indexes
                           .Action("List", "SitemapIndex", "OrchardCore.Sitemaps")
                           .LocalNav()
                       )
                       .Add(S["Sitemaps Cache"], S["Sitemaps Cache"].PrefixPosition("3"), cache => cache
                           .Action("List", "SitemapCache", "OrchardCore.Sitemaps")
                           .LocalNav()
                       )
                   )
               );

            return ValueTask.CompletedTask;
        }
        builder
            .Add(S["Tools"], tools => tools
                .Add(S["Search Engine Optimization"], S["Search Engine Optimization"].PrefixPosition(), seo => seo
                    .Permission(SitemapsPermissions.ManageSitemaps)
                    .Add(S["Sitemaps"], S["Sitemaps"].PrefixPosition("1"), sitemaps => sitemaps
                        .Action("List", "Admin", "OrchardCore.Sitemaps")
                        .LocalNav()
                    )
                    .Add(S["Sitemap Indexes"], S["Sitemap Indexes"].PrefixPosition("2"), indexes => indexes
                        .Action("List", "SitemapIndex", "OrchardCore.Sitemaps")
                        .LocalNav()
                    )
                    .Add(S["Sitemaps Cache"], S["Sitemaps Cache"].PrefixPosition("3"), cache => cache
                        .Action("List", "SitemapCache", "OrchardCore.Sitemaps")
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}
