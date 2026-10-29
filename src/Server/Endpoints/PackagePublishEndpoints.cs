using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using NuGetFeed.Authentication;
using NuGetFeed.Configuration;
using NuGetFeed.Extensions;
using NuGetFeed.Indexing;
using NuGetFeed.Storage;

namespace NuGetFeed.Endpoints;

/// <summary>
/// The Package Publish and Symbol Package Publish resources, used to push, delete and relist packages.
/// See: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource
/// </summary>
internal static class PackagePublishEndpoints
{
    private const string LoggerCategory = "NuGetFeed.Endpoints.PackagePublish";

    // PUT api/v2/package
    // See: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
    public static async Task<IResult> UploadPackageAsync(
        HttpRequest request,
        IAuthenticationService authentication,
        IPackageIndexingService indexer,
        IOptionsSnapshot<NuGetFeedOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await EndpointHelpers.IsAuthorizedAsync(request, authentication, options, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        try
        {
            using var uploadStream = await request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var result = await indexer.IndexAsync(uploadStream, cancellationToken);

            return Results.StatusCode(result switch
            {
                PackageIndexingResult.InvalidPackage => StatusCodes.Status400BadRequest,
                PackageIndexingResult.PackageAlreadyExists => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status201Created,
            });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            loggerFactory.CreateLogger(LoggerCategory).LogError(e, "Exception thrown during package upload");

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    // DELETE api/v2/package/{id}/{version}
    public static async Task<IResult> DeleteAsync(
        string id,
        string version,
        HttpRequest request,
        IAuthenticationService authentication,
        IPackageDeletionService deletionService,
        IOptionsSnapshot<NuGetFeedOptions> options,
        CancellationToken cancellationToken)
    {
        if (options.Value.IsReadOnlyMode) return Results.Unauthorized();
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return Results.NotFound();
        if (!await authentication.AuthenticateAsync(request.GetApiKey(), cancellationToken)) return Results.Unauthorized();

        return await deletionService.TryDeletePackageAsync(id, nugetVersion, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    // POST api/v2/package/{id}/{version}
    public static async Task<IResult> RelistAsync(
        string id,
        string version,
        HttpRequest request,
        IAuthenticationService authentication,
        IPackageDeletionService deletionService,
        IOptionsSnapshot<NuGetFeedOptions> options,
        CancellationToken cancellationToken)
    {
        if (options.Value.IsReadOnlyMode) return Results.Unauthorized();
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return Results.NotFound();
        if (!await authentication.AuthenticateAsync(request.GetApiKey(), cancellationToken)) return Results.Unauthorized();

        return await deletionService.TryRelistPackageAsync(id, nugetVersion, cancellationToken)
            ? Results.Ok()
            : Results.NotFound();
    }

    // PUT api/v2/symbol
    // See: https://docs.microsoft.com/en-us/nuget/api/symbol-package-publish-resource
    public static async Task<IResult> UploadSymbolPackageAsync(
        HttpRequest request,
        IAuthenticationService authentication,
        ISymbolIndexingService indexer,
        IOptionsSnapshot<NuGetFeedOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await EndpointHelpers.IsAuthorizedAsync(request, authentication, options, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        try
        {
            using var uploadStream = await request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var result = await indexer.IndexAsync(uploadStream, cancellationToken);

            return Results.StatusCode(result switch
            {
                SymbolIndexingResult.InvalidSymbolPackage => StatusCodes.Status400BadRequest,
                SymbolIndexingResult.PackageNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status201Created,
            });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            loggerFactory.CreateLogger(LoggerCategory).LogError(e, "Exception thrown during symbol upload");

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    // GET api/download/symbols/{file}/{key}/{file2}
    public static async Task<IResult> DownloadSymbolsAsync(string file, string key, ISymbolStorageService storage)
    {
        var pdbStream = await storage.GetPortablePdbContentStreamOrNullAsync(file, key);
        if (pdbStream == null)
        {
            return Results.NotFound();
        }

        return Results.Stream(pdbStream, "application/octet-stream");
    }
}
