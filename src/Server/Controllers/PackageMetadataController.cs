using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;
using NuGetFeed.Metadata;
using NuGetFeed.Models;

namespace NuGetFeed.Controllers;

/// <summary>
/// The Package Metadata resource, used to fetch packages' information.
/// See: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
/// </summary>
public class PackageMetadataController : Controller
{
    private readonly IPackageMetadataService _metadata;

    public PackageMetadataController(IPackageMetadataService metadata)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    // GET v3/registration/{id}/index.json or v3/registration-semver2/{id}/index.json
    [HttpGet]
    public async Task<ActionResult<NuGetFeedRegistrationIndexResponse>> RegistrationIndexAsync(string id, bool semVer2, CancellationToken cancellationToken)
    {
        var index = await _metadata.GetRegistrationIndexOrNullAsync(id, semVer2, cancellationToken);
        if (index == null)
        {
            return NotFound();
        }

        return index;
    }

    // GET v3/registration/{id}/{version}.json or v3/registration-semver2/{id}/{version}.json
    [HttpGet]
    public async Task<ActionResult<RegistrationLeafResponse>> RegistrationLeafAsync(string id, string version, bool semVer2, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var leaf = await _metadata.GetRegistrationLeafOrNullAsync(id, nugetVersion, semVer2, cancellationToken);
        if (leaf == null)
        {
            return NotFound();
        }

        return leaf;
    }
}
