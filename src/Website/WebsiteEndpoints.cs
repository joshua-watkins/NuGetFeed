using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGetFeed.Pages;

namespace NuGetFeed;

/// <summary>
/// The website's pages.
/// </summary>
internal static class WebsiteEndpoints
{
    private const string LoggerCategory = "NuGetFeed.Website";

    // GET /?q={query}&p={page}&packageType={type}&framework={framework}&prerelease={bool}
    public static async Task<IResult> IndexAsync(
        HttpRequest request,
        IndexModel model,
        FeedConnection feed,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken,
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery(Name = "p")] int page = 1,
        string packageType = "any",
        string framework = "any",
        bool prerelease = true)
    {
        if (page < 1) return Results.BadRequest();

        model.Query = query;
        model.PageIndex = page;
        model.PackageType = packageType;
        model.Framework = framework;
        model.Prerelease = prerelease;
        model.PathBase = request.PathBase.Value ?? string.Empty;

        return await RenderAsync(
            request,
            feed,
            loggerFactory,
            () => model.OnGetAsync(cancellationToken),
            () => Results.RazorSlice<Slices.Index, IndexModel>(model));
    }

    // GET /packages/{id}/{version?}
    public static async Task<IResult> PackageAsync(
        string id,
        string? version,
        HttpRequest request,
        PackageModel model,
        FeedConnection feed,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        model.PathBase = request.PathBase.Value ?? string.Empty;

        return await RenderAsync(
            request,
            feed,
            loggerFactory,
            () => model.OnGetAsync(id, version, cancellationToken),
            () => Results.RazorSlice<Slices.Package, PackageModel>(model));
    }

    // GET /upload
    public static async Task<IResult> UploadAsync(
        HttpRequest request,
        UploadModel model,
        FeedConnection feed,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        model.PathBase = request.PathBase.Value ?? string.Empty;
        model.BaseUrl = string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent(),
            "/");

        return await RenderAsync(
            request,
            feed,
            loggerFactory,
            () => model.OnGetAsync(cancellationToken),
            () => Results.RazorSlice<Slices.Upload, UploadModel>(model));
    }

    /// <summary>
    /// Load the page's content from the feed, then render the page. If the feed can't be
    /// reached or returns something unexpected, render a page that explains the problem.
    /// </summary>
    private static async Task<IResult> RenderAsync(
        HttpRequest request,
        FeedConnection feed,
        ILoggerFactory loggerFactory,
        Func<Task> loadAsync,
        Func<IResult> render)
    {
        try
        {
            await loadAsync();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException)
        {
            string serviceIndexUrl;
            try
            {
                serviceIndexUrl = feed.ServiceIndexUrl;
            }
            catch (InvalidOperationException)
            {
                // The website doesn't know which feed to read.
                serviceIndexUrl = "(not configured)";
            }

            loggerFactory
                .CreateLogger(LoggerCategory)
                .LogError(e, "Could not read the NuGet feed at {ServiceIndexUrl}", serviceIndexUrl);

            var model = new FeedErrorModel(request.PathBase.Value ?? string.Empty, serviceIndexUrl);

            return Results.RazorSlice<Slices.FeedError, FeedErrorModel>(model, StatusCodes.Status502BadGateway);
        }

        return render();
    }
}
