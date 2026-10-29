using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

/// <summary>
/// Hosts the server in memory, the way the Windows and Linux hosts do, without the website.
/// The application starts the first time it's used.
/// </summary>
internal sealed class NuGetFeedTestApplication : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient? _upstreamClient;
    private readonly IReadOnlyDictionary<string, string?> _configuration;
    private readonly Lazy<WebApplication> _app;

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
        _app = new Lazy<WebApplication>(Start);
    }

    public IServiceProvider Services => _app.Value.Services;

    public TestServer Server => _app.Value.GetTestServer();

    public HttpClient CreateClient() => Server.CreateClient();

    /// <summary>
    /// Create a client that reaches the server over HTTPS, which some resources require.
    /// </summary>
    public HttpClient CreateHttpsClient()
    {
        var client = Server.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    public void Dispose()
    {
        if (_app.IsValueCreated)
        {
            _app.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private WebApplication Start()
    {
        // Each test application gets its own in-memory database, which the server migrates at startup,
        // and a temporary storage path.
        var testId = Guid.NewGuid().ToString("N");
        var storagePath = Path.Combine(Path.GetTempPath(), "NuGetFeedTests", testId, "Packages");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.WebHost.UseTestServer();

        // NuGetFeed uses console logging by default. This logger throws operation
        // cancelled exceptions when the host shuts down, causing the the debugger
        // to pause repeatedly if CLR exceptions are enabled.
        builder.Logging.ClearProviders();

        // Pipe logs to the xunit output.
        builder.Logging.AddProvider(new XunitLoggerProvider(_output));

        // Setup the integration test configuration.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ApiKey", "" },
            { "PackageDeletionBehavior", "HardDelete" },
            { "AllowPackageOverwrites", "true" },
            { "Database:Type", "Sqlite" },
            { "Database:ConnectionString", $"Data Source=NuGetFeedTests-{testId};Mode=Memory;Cache=Shared" },
            { "Storage:Type", "FileSystem" },
            { "Storage:Path", storagePath },
            { "Search:Type", "Database" },
            { "Mirror:Enabled", _upstreamClient != null ? "true": "false" },
            { "Mirror:PackageSource", "http://localhost/v3/index.json" },
        });

        builder.Configuration.AddInMemoryCollection(_configuration);

        builder.Services.AddNuGetFeed(feed =>
        {
            feed.AddSqliteDatabase();
            feed.AddFileStorage();
        });

        // Make time deterministic for testing purposes.
        var time = new Mock<SystemTime>();
        time
            .Setup(t => t.UtcNow)
            .Returns(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        builder.Services.AddSingleton(time.Object);
        if (_upstreamClient != null)
        {
            builder.Services.AddSingleton(_upstreamClient);
        }

        var app = builder.Build();

        app.UseNuGetFeed();
        app.UseRouting();

        app.UseCors(ConfigureNuGetFeedOptions.CorsPolicy);
        app.UseOperationCancelledMiddleware();

        app.MapNuGetFeed();

        app.RunMigrationsAsync().GetAwaiter().GetResult();
        app.StartAsync().GetAwaiter().GetResult();

        return app;
    }
}

internal static class NuGetFeedTestApplicationExtensions
{
    public static async Task AddPackageAsync(
        this NuGetFeedTestApplication app,
        Stream package,
        CancellationToken cancellationToken = default)
    {
        var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

        using var scope = scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IPackageIndexingService>();

        var result = await indexer.IndexAsync(package, cancellationToken);
        if (result != PackageIndexingResult.Success)
        {
            throw new InvalidOperationException($"Unexpected indexing result {result}");
        }
    }

    public static async Task AddSymbolPackageAsync(
        this NuGetFeedTestApplication app,
        Stream symbolPackage,
        CancellationToken cancellationToken = default)
    {
        var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

        using var scope = scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<ISymbolIndexingService>();

        var result = await indexer.IndexAsync(symbolPackage, cancellationToken);
        if (result != SymbolIndexingResult.Success)
        {
            throw new InvalidOperationException($"Unexpected indexing result {result}");
        }
    }
}
