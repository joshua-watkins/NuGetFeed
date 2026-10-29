using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NuGetFeed.PackageDetails;
using NuGetFeed.Pages;
using NuGetFeed.Search;
using NuGetFeed.ServiceIndex;

namespace NuGetFeed.Extensions;

public static class WebsiteExtensions
{
    /// <summary>
    /// Adds the NuGet feed website. The website reads packages from the feed configured by
    /// <see cref="NuGetWebsiteOptions.ServiceIndexUrl"/>, or from a server hosted in the same application.
    /// </summary>
    public static IServiceCollection AddNuGetFeedUI(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddHttpContextAccessor();

        services
            .AddOptions<NuGetWebsiteOptions>()
            .BindConfiguration(NuGetWebsiteOptions.DefaultSection);

        services.AddSingleton<FeedConnection>();
        services.AddSingleton<IServiceIndexClient>(provider => provider.GetRequiredService<FeedConnection>().ClientFactory.CreateServiceIndexClient());
        services.AddSingleton<ISearchClient>(provider => provider.GetRequiredService<FeedConnection>().ClientFactory.CreateSearchClient());
        services.AddSingleton<IPackageDetailsClient>(provider => provider.GetRequiredService<FeedConnection>().ClientFactory.CreatePackageDetailsClient());

        services.AddTransient<IndexModel>();
        services.AddTransient<PackageModel>();
        services.AddTransient<UploadModel>();

        return services;
    }

    /// <summary>
    /// Maps the NuGet feed website's pages and files.
    /// </summary>
    public static IEndpointRouteBuilder MapNuGetFeedUI(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

        endpoints.MapGet("/", WebsiteEndpoints.IndexAsync);
        endpoints.MapGet("/packages/{id}/{version?}", WebsiteEndpoints.PackageAsync);
        endpoints.MapGet("/upload", WebsiteEndpoints.UploadAsync);
        endpoints.MapGet(WebsiteFiles.RequestPath + "/{**path}", WebsiteFiles.GetFile);

        return endpoints;
    }
}
