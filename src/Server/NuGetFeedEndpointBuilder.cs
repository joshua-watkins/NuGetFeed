using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NuGetFeed.Endpoints;

namespace NuGetFeed;

public class NuGetFeedEndpointBuilder
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapServiceIndexRoutes(endpoints);
        MapPackagePublishRoutes(endpoints);
        MapSymbolRoutes(endpoints);
        MapSearchRoutes(endpoints);
        MapPackageMetadataRoutes(endpoints);
        MapPackageContentRoutes(endpoints);
        MapCatalogRoutes(endpoints);
        MapDeprecationRoutes(endpoints);
        MapVulnerabilityRoutes(endpoints);
        MapRepositorySignatureRoutes(endpoints);
    }

    public void MapCatalogRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/catalog/index.json", CatalogEndpoints.IndexAsync)
            .WithName(Routes.CatalogIndexRouteName);

        endpoints.MapGet("v3/catalog/page{page:int}.json", CatalogEndpoints.PageAsync)
            .WithName(Routes.CatalogPageRouteName);

        endpoints.MapGet("v3/catalog/data/{key:int}/{name}", CatalogEndpoints.LeafAsync)
            .WithName(Routes.CatalogLeafRouteName);
    }

    public void MapDeprecationRoutes(IEndpointRouteBuilder endpoints)
    {
        // Not part of the NuGet protocol: lets administrators deprecate packages.
        endpoints.MapPut("api/package/{id}/{version}/deprecation", PackageDeprecationEndpoints.DeprecateAsync)
            .WithName(Routes.DeprecationRouteName);

        endpoints.MapDelete("api/package/{id}/{version}/deprecation", PackageDeprecationEndpoints.UndeprecateAsync);
    }

    public void MapVulnerabilityRoutes(IEndpointRouteBuilder endpoints)
    {
        // Not part of the NuGet protocol: lets administrators manage known vulnerabilities.
        endpoints.MapPut("api/vulnerabilities", VulnerabilityEndpoints.UpsertAsync)
            .WithName(Routes.VulnerabilitiesRouteName);

        endpoints.MapDelete("api/vulnerabilities", VulnerabilityEndpoints.DeleteAsync);

        endpoints.MapGet("v3/vulnerabilities/index.json", VulnerabilityEndpoints.IndexAsync)
            .WithName(Routes.VulnerabilityIndexRouteName);

        endpoints.MapGet("v3/vulnerabilities/{name}.json", VulnerabilityEndpoints.VulnerabilityFileAsync)
            .WithName(Routes.VulnerabilityFileRouteName);
    }

    public void MapRepositorySignatureRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/repository-signatures/{resourceVersion}/index.json", ServiceIndexEndpoints.RepositorySignaturesAsync)
            .WithName(Routes.RepositorySignaturesRouteName);

        endpoints.MapGet("v3/repository-signatures/certificates/{fingerprint}.crt", ServiceIndexEndpoints.RepositorySigningCertificate)
            .WithName(Routes.RepositorySigningCertificateRouteName);
    }

    public void MapServiceIndexRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/index.json", ServiceIndexEndpoints.ServiceIndexAsync)
            .WithName(Routes.IndexRouteName);
    }

    public void MapPackagePublishRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("api/v2/package", PackagePublishEndpoints.UploadPackageAsync)
            .WithName(Routes.UploadPackageRouteName);

        endpoints.MapDelete("api/v2/package/{id}/{version}", PackagePublishEndpoints.DeleteAsync)
            .WithName(Routes.DeleteRouteName);

        endpoints.MapPost("api/v2/package/{id}/{version}", PackagePublishEndpoints.RelistAsync)
            .WithName(Routes.RelistRouteName);

        // The publish routes above are part of the V3 protocol despite their path. The
        // NuGet V2 (OData) feed is not supported: tell V2 clients to use the V3 feed.
        endpoints.MapMethods("api/v2/{**path}", new[] { "GET", "HEAD" }, ServiceIndexEndpoints.V2FeedNotSupported)
            .WithName(Routes.V2FeedRouteName);
    }

    public void MapSymbolRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("api/v2/symbol", PackagePublishEndpoints.UploadSymbolPackageAsync)
            .WithName(Routes.UploadSymbolRouteName);

        endpoints.MapGet("api/download/symbols/{file}/{key}/{file2}", PackagePublishEndpoints.DownloadSymbolsAsync)
            .WithName(Routes.SymbolDownloadRouteName);

        endpoints.MapGet("api/download/symbols/{prefix}/{file}/{key}/{file2}", PackagePublishEndpoints.DownloadSymbolsAsync)
            .WithName(Routes.PrefixedSymbolDownloadRouteName);
    }

    public void MapSearchRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/search", SearchEndpoints.SearchAsync)
            .WithName(Routes.SearchRouteName);

        endpoints.MapGet("v3/autocomplete", SearchEndpoints.AutocompleteAsync)
            .WithName(Routes.AutocompleteRouteName);

        // This is an unofficial API to find packages that depend on a given package.
        endpoints.MapGet("v3/dependents", SearchEndpoints.DependentsAsync)
            .WithName(Routes.DependentsRouteName);
    }

    public void MapPackageMetadataRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/registration/{id}/index.json", PackageMetadataEndpoints.RegistrationIndexAsync)
            .WithName(Routes.RegistrationIndexRouteName);

        endpoints.MapGet("v3/registration/{id}/{version}.json", PackageMetadataEndpoints.RegistrationLeafAsync)
            .WithName(Routes.RegistrationLeafRouteName);

        // RegistrationsBaseUrl/3.6.0 includes SemVer 2.0.0 packages. The other variants must not.
        endpoints.MapGet("v3/registration-semver2/{id}/index.json", PackageMetadataEndpoints.SemVer2RegistrationIndexAsync)
            .WithName(Routes.RegistrationSemVer2IndexRouteName);

        endpoints.MapGet("v3/registration-semver2/{id}/{version}.json", PackageMetadataEndpoints.SemVer2RegistrationLeafAsync)
            .WithName(Routes.RegistrationSemVer2LeafRouteName);
    }

    public void MapPackageContentRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v3/package/{id}/index.json", PackageContentEndpoints.GetPackageVersionsAsync)
            .WithName(Routes.PackageVersionsRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/{idVersion}.nupkg", PackageContentEndpoints.DownloadPackageAsync)
            .WithName(Routes.PackageDownloadRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/{id2}.nuspec", PackageContentEndpoints.DownloadNuspecAsync)
            .WithName(Routes.PackageDownloadManifestRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/readme", PackageContentEndpoints.DownloadReadmeAsync)
            .WithName(Routes.PackageDownloadReadmeRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/icon", PackageContentEndpoints.DownloadIconAsync)
            .WithName(Routes.PackageDownloadIconRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/license.txt", PackageContentEndpoints.DownloadLicenseTextAsync)
            .WithName(Routes.PackageDownloadLicenseTextRouteName);

        endpoints.MapGet("v3/package/{id}/{version}/license.md", PackageContentEndpoints.DownloadLicenseMarkdownAsync)
            .WithName(Routes.PackageDownloadLicenseMarkDownRouteName);
    }
}
