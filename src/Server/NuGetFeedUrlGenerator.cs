using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using NuGetFeed.Configuration;
using System;

namespace NuGetFeed;

// TODO: This should validate the "Host" header against known valid values
public class NuGetFeedUrlGenerator : IUrlGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IOptions<NuGetFeedOptions> _options;

    public NuGetFeedUrlGenerator(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        IOptions<NuGetFeedOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string GetServiceIndexUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.IndexRouteName,
            values: null);
    }

    public string GetPackageContentResourceUrl()
    {
        return AbsoluteUrl("v3/package");
    }

    public string GetPackageMetadataResourceUrl(bool semVer2 = false)
    {
        return AbsoluteUrl(semVer2 ? "v3/registration-semver2" : "v3/registration");
    }

    public string GetPackagePublishResourceUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.UploadPackageRouteName,
            values: null);
    }

    public string GetSymbolPublishResourceUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.UploadSymbolRouteName,
            values: null);
    }

    public string GetSearchResourceUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.SearchRouteName,
            values: null);
    }

    public string GetAutocompleteResourceUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.AutocompleteRouteName,
            values: null);
    }

    public string GetRegistrationIndexUrl(string id, bool semVer2 = false)
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            semVer2 ? Routes.RegistrationSemVer2IndexRouteName : Routes.RegistrationIndexRouteName,
            values: new { Id = id.ToLowerInvariant() });
    }

    public string GetRegistrationPageUrl(string id, NuGetVersion lower, NuGetVersion upper)
    {
        // NuGetFeed does not support paging the registration resource.
        throw new NotImplementedException();
    }

    public string GetRegistrationLeafUrl(string id, NuGetVersion version, bool semVer2 = false)
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            semVer2 ? Routes.RegistrationSemVer2LeafRouteName : Routes.RegistrationLeafRouteName,
            values: new
            {
                Id = id.ToLowerInvariant(),
                Version = version.ToNormalizedString().ToLowerInvariant(),
            });
    }

    public string GetPackageVersionsUrl(string id)
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.PackageVersionsRouteName,
            values: new { Id = id.ToLowerInvariant() });
    }

    public string GetPackageDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.PackageDownloadRouteName,
            values: new
            {
                Id = id,
                Version = versionString,
                IdVersion = $"{id}.{versionString}"
            });
    }

    public string GetPackageManifestDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.PackageDownloadRouteName,
            values: new
            {
                Id = id,
                Version = versionString,
                Id2 = id,
            });
    }

    public string GetPackageIconDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.PackageDownloadIconRouteName,
            values: new
            {
                Id = id,
                Version = versionString
            });
    }

    public string GetPackageLicenseDownloadUrl(string id, NuGetVersion version, bool licenseIsMarkDown)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        if (licenseIsMarkDown)
        {
            return _linkGenerator.GetUriByRouteValues(
                   _httpContextAccessor.HttpContext,
                   Routes.PackageDownloadLicenseMarkDownRouteName,
                   values: new
                   {
                       Id = id,
                       Version = versionString
                   });
        }
        else
        {
            return _linkGenerator.GetUriByRouteValues(
                   _httpContextAccessor.HttpContext,
                   Routes.PackageDownloadLicenseTextRouteName,
                   values: new
                   {
                       Id = id,
                       Version = versionString
                   });
        }
    }

    public string GetReadmeUriTemplate()
    {
        return AbsoluteUrl("v3/package/{lower_id}/{lower_version}/readme");
    }

    public string GetPackageDependentsResourceUrl()
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.DependentsRouteName,
            values: null);
    }

    public string GetPackageReadmeDownloadUrl(string id, NuGetVersion version)
    {
        return _linkGenerator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            Routes.PackageDownloadReadmeRouteName,
            values: new
            {
                Id = id.ToLowerInvariant(),
                Version = version.ToNormalizedString().ToLowerInvariant(),
            });
    }

    public string GetCatalogIndexUrl()
    {
        return AbsoluteUrl("v3/catalog/index.json");
    }

    public string GetCatalogPageUrl(int page)
    {
        return AbsoluteUrl($"v3/catalog/page{page}.json");
    }

    public string GetCatalogLeafUrl(int key, string id, string version)
    {
        return AbsoluteUrl($"v3/catalog/data/{key}/{Uri.EscapeDataString(id.ToLowerInvariant())}.{Uri.EscapeDataString(version.ToLowerInvariant())}.json");
    }

    public string GetPackageDetailsUriTemplate()
    {
        var template = _options.Value.PackageDetailsUriTemplate;
        if (string.IsNullOrEmpty(template)) return null;

        // An absolute template points somewhere else, like a separately hosted website.
        // A relative one is resolved against this server's address.
        return Uri.IsWellFormedUriString(template, UriKind.Absolute)
            ? template
            : AbsoluteUrl(template.TrimStart('/'));
    }

    public string GetRepositorySignaturesUrl(string resourceVersion)
    {
        return AbsoluteUrl($"v3/repository-signatures/{resourceVersion}/index.json");
    }

    public string GetRepositorySigningCertificateUrl(string sha256Fingerprint)
    {
        return AbsoluteUrl($"v3/repository-signatures/certificates/{sha256Fingerprint}.crt");
    }

    public string GetVulnerabilityIndexUrl()
    {
        return AbsoluteUrl("v3/vulnerabilities/index.json");
    }

    public string GetVulnerabilityFileUrl(string name)
    {
        return AbsoluteUrl($"v3/vulnerabilities/{name}.json");
    }

    private string AbsoluteUrl(string relativePath)
    {
        var request = _httpContextAccessor.HttpContext.Request;

        return string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent(),
            "/",
            relativePath);
    }
}
