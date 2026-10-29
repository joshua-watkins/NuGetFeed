using Microsoft.AspNetCore.Builder;
using NuGetFeed.Extensions;

// Runs the website on its own. It shows the packages of the NuGet V3 feed at the
// Website:ServiceIndexUrl setting, which can be this solution's server or any other feed.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNuGetFeedUI();

var app = builder.Build();

// The pages link to their files under _content/NuGetFeed.Website, where hosts and published builds
// put them. During development the SDK serves this project's own files from the site's root instead,
// so also serve the web root under that path.
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/_content/NuGetFeed.Website",
    FileProvider = app.Environment.WebRootFileProvider,
});
app.UseStaticFiles();
app.UseRouting();

app.MapNuGetFeedUI();

app.Run();
