using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NuGetFeed;
using NuGetFeed.Configuration;
using NuGetFeed.Search;
using NuGetFeed.Storage;

namespace NuGetFeed.Extensions;

public static class NuGetFeedApplicationExtensions
{
    public static NuGetFeedApplication AddFileStorage(this NuGetFeedApplication app)
    {
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<FileStorageService>());
        return app;
    }

    public static NuGetFeedApplication AddFileStorage(
        this NuGetFeedApplication app,
        Action<FileSystemStorageOptions> configure)
    {
        app.AddFileStorage();
        app.Services.Configure(configure);
        return app;
    }

    public static NuGetFeedApplication AddNullStorage(this NuGetFeedApplication app)
    {
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<NullStorageService>());
        return app;
    }

    public static NuGetFeedApplication AddNullSearch(this NuGetFeedApplication app)
    {
        app.Services.TryAddTransient<ISearchIndexer>(provider => provider.GetRequiredService<NullSearchIndexer>());
        app.Services.TryAddTransient<ISearchService>(provider => provider.GetRequiredService<NullSearchService>());
        return app;
    }
}
