using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGetFeed;
using NuGetFeed.Extensions;
using NuGetFeed.Upstream;

// Runs the NuGet feed server on its own: the NuGet V3 API, without the website.
if (args.Any(arg => arg is "-h" or "-?" or "--help"))
{
    Console.WriteLine("""
        A light-weight NuGet service

        Usage: nugetfeed [command] [options]

        Commands:
          import downloads    Import package downloads from nuget.org.

        Options:
          --urls <urls>       The URLs that NuGetFeed should bind to.
          --<key>=<value>     Set a configuration value, like --Database:ConnectionString="Data Source=nugetfeed.db".
          -h|--help           Show help information.

        With no command, runs the server.
        """);
    return;
}

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

switch (GetCommand(args))
{
    case []:
        break;

    case ["import", "downloads"]:
        using (var cancellation = new CancellationTokenSource())
        using (var scope = app.Services.CreateScope())
        {
            // Stop the import with Ctrl+C.
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellation.Cancel();
            };

            var importer = scope.ServiceProvider.GetRequiredService<DownloadsImporter>();

            await importer.ImportAsync(cancellation.Token);
        }

        return;

    case var unknown:
        Console.Error.WriteLine($"Unknown command '{string.Join(' ', unknown)}'. Use --help to see the commands.");
        Environment.ExitCode = 1;
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

await app.RunMigrationsAsync();
await app.RunAsync();

// The command is the arguments that aren't options. Options, like --environment Development
// or --Database:ConnectionString=..., are configuration values that the host already read.
static string[] GetCommand(string[] args)
{
    var command = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg.StartsWith('-') || arg.StartsWith('/'))
        {
            // "--key value" passes the value as the next argument; "--key=value" doesn't.
            if (!arg.Contains('=')) i++;

            continue;
        }

        command.Add(arg);
    }

    return command.ToArray();
}
