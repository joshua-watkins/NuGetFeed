using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGetFeed;
using NuGetFeed.Extensions;
using NuGetFeed.Upstream;

// Runs the NuGet feed server on its own: the NuGet V3 API, without the website.
var builder = WebApplication.CreateBuilder(args);

builder.UseNuGetFeedConfigRoot();

builder.WebHost.ConfigureKestrel(options =>
{
    // Remove the upload limit from Kestrel. If needed, an upload limit can
    // be enforced by a reverse proxy server, like IIS.
    options.Limits.MaxRequestBodySize = null;
});

builder.Services.AddNuGetFeed(feed =>
{
    // Add database providers.
    //feed.AddAzureTableDatabase();
    //feed.AddMySqlDatabase();
    //feed.AddPostgreSqlDatabase();
    feed.AddSqliteDatabase();
    //feed.AddSqlServerDatabase();

    // Add storage providers.
    feed.AddFileStorage();
    //feed.AddAliyunOssStorage();
    //feed.AddAwsS3Storage();
    //feed.AddAzureBlobStorage();
    //feed.AddGoogleCloudStorage();

    // Add search providers.
    //feed.AddAzureSearch();
});

var app = builder.Build();
if (!app.ValidateStartupOptions())
{
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseStatusCodePages();
}

app.UseNuGetFeed();
app.UseRouting();

app.UseCors(ConfigureNuGetFeedOptions.CorsPolicy);
app.UseOperationCancelledMiddleware();

app.MapNuGetFeed();

// Other arguments, like --environment or --Database:ConnectionString=..., are configuration
// values that the host already read. Let them through instead of rejecting them.
var commandLine = new CommandLineApplication
{
    Name = "nugetfeed",
    Description = "A light-weight NuGet service",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue,
};

commandLine.HelpOption(inherited: true);

commandLine.Command("import", import =>
{
    import.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;

    import.Command("downloads", downloads =>
    {
        downloads.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;

        downloads.OnExecuteAsync(async cancellationToken =>
        {
            using var scope = app.Services.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<DownloadsImporter>();

            await importer.ImportAsync(cancellationToken);
        });
    });
});

commandLine.Option("--urls", "The URLs that NuGetFeed should bind to.", CommandOptionType.SingleValue);

commandLine.OnExecuteAsync(async cancellationToken =>
{
    await app.RunMigrationsAsync(cancellationToken);
    await app.RunAsync();
});

await commandLine.ExecuteAsync(args);
