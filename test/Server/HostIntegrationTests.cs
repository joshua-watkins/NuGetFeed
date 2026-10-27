using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGetFeed;
using NuGetFeed.Entities;
using NuGetFeed.Extensions;
using Xunit;

namespace NuGetFeed.Server.Tests
{
    public class HostIntegrationTests
    {
        private readonly string DatabaseTypeKey = "Database:Type";
        private readonly string ConnectionStringKey = "Database:ConnectionString";

        [Fact]
        public void ThrowsIfDatabaseTypeInvalid()
        {
            var provider = BuildServiceProvider(new Dictionary<string, string>
            {
                { DatabaseTypeKey, "InvalidType" }
            });

            Assert.Throws<InvalidOperationException>(
                () => provider.GetRequiredService<IContext>());
        }

        [Fact]
        public void ReturnsDatabaseContext()
        {
            var provider = BuildServiceProvider(new Dictionary<string, string>
            {
                { DatabaseTypeKey, "Sqlite" },
                { ConnectionStringKey, "..." }
            });

            Assert.NotNull(provider.GetRequiredService<IContext>());
        }

        [Fact]
        public void ReturnsSqliteContext()
        {
            var provider = BuildServiceProvider(new Dictionary<string, string>
            {
                { DatabaseTypeKey, "Sqlite" },
                { ConnectionStringKey, "..." }
            });

            Assert.NotNull(provider.GetRequiredService<SqliteContext>());
        }

        [Fact]
        public void DefaultsToSqlite()
        {
            var provider = BuildServiceProvider();

            var context = provider.GetRequiredService<IContext>();

            Assert.IsType<SqliteContext>(context);
        }

        private IServiceProvider BuildServiceProvider(Dictionary<string, string> configs = null)
        {
            // Start from the server's own appsettings.json, which is copied to the test output.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddInMemoryCollection(configs ?? new Dictionary<string, string>())
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddNuGetFeed(feed =>
            {
                feed.AddSqliteDatabase();
                feed.AddFileStorage();
            });

            return services.BuildServiceProvider();
        }
    }
}
