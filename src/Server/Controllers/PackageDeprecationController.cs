using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using NuGetFeed.Authentication;
using NuGetFeed.Configuration;
using NuGetFeed.Extensions;
using NuGetFeed.Indexing;
using NuGetFeed.Metadata;

namespace NuGetFeed.Controllers;

/// <summary>
/// Deprecates package versions. This isn't part of the NuGet protocol; deprecations are
/// published to clients through the package metadata, search and catalog resources.
/// </summary>
public class PackageDeprecationController : Controller
{
    private readonly IAuthenticationService _authentication;
    private readonly IPackageDeprecationService _deprecations;
    private readonly IOptionsSnapshot<NuGetFeedOptions> _options;

    public PackageDeprecationController(
        IAuthenticationService authentication,
        IPackageDeprecationService deprecations,
        IOptionsSnapshot<NuGetFeedOptions> options)
    {
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _deprecations = deprecations ?? throw new ArgumentNullException(nameof(deprecations));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    // PUT api/package/{id}/{version}/deprecation
    [HttpPut]
    [ActionName("Deprecation")]
    public async Task<IActionResult> DeprecateAsync(
        string id,
        string version,
        [FromBody] DeprecationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken)) return Unauthorized();
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return NotFound();

        var reasons = new List<string>();
        foreach (var reason in request?.Reasons ?? Array.Empty<string>())
        {
            var known = PackageModelExtensions.DeprecationReasons
                .FirstOrDefault(r => string.Equals(r, reason, StringComparison.OrdinalIgnoreCase));
            if (known == null)
            {
                return BadRequest($"Unknown deprecation reason '{reason}'. Use: {string.Join(", ", PackageModelExtensions.DeprecationReasons)}.");
            }

            if (!reasons.Contains(known)) reasons.Add(known);
        }

        if (reasons.Count == 0)
        {
            return BadRequest($"At least one deprecation reason is required: {string.Join(", ", PackageModelExtensions.DeprecationReasons)}.");
        }

        var alternateId = string.IsNullOrWhiteSpace(request.AlternatePackageId) ? null : request.AlternatePackageId.Trim();
        var alternateRange = string.IsNullOrWhiteSpace(request.AlternatePackageVersionRange) ? null : request.AlternatePackageVersionRange.Trim();
        if (alternateRange != null && !VersionRange.TryParse(alternateRange, out _))
        {
            return BadRequest($"'{alternateRange}' isn't a valid version range.");
        }

        var found = await _deprecations.DeprecateAsync(
            id,
            nugetVersion,
            reasons,
            string.IsNullOrWhiteSpace(request.Message) ? null : request.Message,
            alternateId,
            alternateRange,
            cancellationToken);

        return found ? NoContent() : NotFound();
    }

    // DELETE api/package/{id}/{version}/deprecation
    [HttpDelete]
    [ActionName("Deprecation")]
    public async Task<IActionResult> UndeprecateAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(cancellationToken)) return Unauthorized();
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return NotFound();

        return await _deprecations.UndeprecateAsync(id, nugetVersion, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private async Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken)
        => !_options.Value.IsReadOnlyMode &&
           await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken);

    public class DeprecationRequest
    {
        /// <summary>
        /// One or more of "Legacy", "CriticalBugs" or "Other".
        /// </summary>
        public string[] Reasons { get; set; }

        public string Message { get; set; }

        public string AlternatePackageId { get; set; }

        /// <summary>
        /// The alternate package's versions. Defaults to any version.
        /// </summary>
        public string AlternatePackageVersionRange { get; set; }
    }
}
