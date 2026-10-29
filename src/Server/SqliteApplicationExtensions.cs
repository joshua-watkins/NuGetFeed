using System;
using NuGetFeed.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGetFeed.Configuration;

namespace NuGetFeed;

public static class SqliteApplicationExtensions
{
    public static NuGetFeedApplication AddSqliteDatabase(this NuGetFeedApplication app)
    {
        app.Services.AddNuGetFeedDatabaseProvider("Sqlite", (provider, options) =>
        {
            var databaseOptions = provider.GetRequiredService<IOptionsSnapshot<DatabaseOptions>>();

            options.UseSqlite(databaseOptions.Value.ConnectionString);
        });

        return app;
    }

    public static NuGetFeedApplication AddSqliteDatabase(
        this NuGetFeedApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqliteDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
