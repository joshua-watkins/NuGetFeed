using NuGetFeed.Server.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NuGetFeed.Extensions;
using NuGetFeed.Indexing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NuGetFeed.Server.Tests.Support;

internal class NuGetFeedTestApplication : WebApplicationFactory<Program>
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient? _upstreamClient;
    private readonly IReadOnlyDictionary<string, string?> _configuration;

    /// <param name="output">Receives the server's logs.</param>
    /// <param name="upstreamClient">If set, the server mirrors the upstream feed this client reaches.</param>
    /// <param name="configuration">Configuration that overrides the test defaults.</param>
    public NuGetFeedTestApplication(
        ITestOutputHelper output,
        HttpClient? upstreamClient = null,
        IReadOnlyDictionary<string, string>? configuration = null)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _upstreamClient = upstreamClient;
        _configuration = (configuration ?? new Dictionary<string, string>())
            .ToDictionary(setting => setting.Key, setting => (string?)setting.Value);
    }

    /// <summary>
    /// Create a client that reaches the server over HTTPS, which some resources require.
    /// </summary>
    public HttpClient CreateHttpsClient()
        => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Each test application gets its own in-memory database, which the server migrates at startup,
        // and a temporary storage path.
        var testId = Guid.NewGuid().ToString("N");
        var storagePath = Path.Combine(Path.GetTempPath(), "NuGetFeedTests", testId, "Packages");

        builder
            .UseEnvironment("Production")
            .ConfigureLogging(logging =>
            {
                // NuGetFeed uses console logging by default. This logger throws operation
                // cancelled exceptions when the host shuts down, causing the the debugger
                // to pause repeatedly if CLR exceptions are enabled.
                logging.ClearProviders();

                // Pipe logs to the xunit output.
                logging.AddProvider(new XunitLoggerProvider(_output));
            })
            .ConfigureAppConfiguration(config =>
            {
                // Setup the integration test configuration.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Database:Type", "Sqlite" },
                    { "Database:ConnectionString", $"Data Source=NuGetFeedTests-{testId};Mode=Memory;Cache=Shared" },
                    { "Storage:Type", "FileSystem" },
                    { "Storage:Path", storagePath },
                    { "Search:Type", "Database" },
                    { "Mirror:Enabled", _upstreamClient != null ? "true": "false" },
                    { "Mirror:PackageSource", "http://localhost/v3/index.json" },
                });

                config.AddInMemoryCollection(_configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // Make time deterministic for testing purposes.
                var time = new Mock<SystemTime>();
                time
                    .Setup(t => t.UtcNow)
                    .Returns(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

                services.AddSingleton(time.Object);
                if (_upstreamClient != null)
                {
                    services.AddSingleton(_upstreamClient);
                }
            });
    }
}

internal static class NuGetFeedTestApplicationExtensions
{
    public static async Task AddPackageAsync(
        this WebApplicationFactory<Program> factory,
        Stream package,
        CancellationToken cancellationToken = default)
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

        using var scope = scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IPackageIndexingService>();

        var result = await indexer.IndexAsync(package, cancellationToken);
        if (result != PackageIndexingResult.Success)
        {
            throw new InvalidOperationException($"Unexpected indexing result {result}");
        }
    }

    public static async Task AddSymbolPackageAsync(
        this WebApplicationFactory<Program> factory,
        Stream symbolPackage,
        CancellationToken cancellationToken = default)
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

        using var scope = scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<ISymbolIndexingService>();

        var result = await indexer.IndexAsync(symbolPackage, cancellationToken);
        if (result != SymbolIndexingResult.Success)
        {
            throw new InvalidOperationException($"Unexpected indexing result {result}");
        }
    }
}
