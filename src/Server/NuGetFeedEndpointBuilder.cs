using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

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
        endpoints.MapControllerRoute(
            name: Routes.CatalogIndexRouteName,
            pattern: "v3/catalog/index.json",
            defaults: new { controller = "Catalog", action = "Index" });

        endpoints.MapControllerRoute(
            name: Routes.CatalogPageRouteName,
            pattern: "v3/catalog/page{page:int}.json",
            defaults: new { controller = "Catalog", action = "Page" });

        endpoints.MapControllerRoute(
            name: Routes.CatalogLeafRouteName,
            pattern: "v3/catalog/data/{key:int}/{name}",
            defaults: new { controller = "Catalog", action = "Leaf" });
    }

    public void MapDeprecationRoutes(IEndpointRouteBuilder endpoints)
    {
        // Not part of the NuGet protocol: lets administrators deprecate packages.
        endpoints.MapControllerRoute(
            name: Routes.DeprecationRouteName,
            pattern: "api/package/{id}/{version}/deprecation",
            defaults: new { controller = "PackageDeprecation", action = "Deprecation" });
    }

    public void MapVulnerabilityRoutes(IEndpointRouteBuilder endpoints)
    {
        // Not part of the NuGet protocol: lets administrators manage known vulnerabilities.
        endpoints.MapControllerRoute(
            name: Routes.VulnerabilitiesRouteName,
            pattern: "api/vulnerabilities",
            defaults: new { controller = "Vulnerability", action = "Advisories" });

        endpoints.MapControllerRoute(
            name: Routes.VulnerabilityIndexRouteName,
            pattern: "v3/vulnerabilities/index.json",
            defaults: new { controller = "Vulnerability", action = "Index" });

        endpoints.MapControllerRoute(
            name: Routes.VulnerabilityFileRouteName,
            pattern: "v3/vulnerabilities/{name}.json",
            defaults: new { controller = "Vulnerability", action = "VulnerabilityFile" });
    }

    public void MapRepositorySignatureRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.RepositorySignaturesRouteName,
            pattern: "v3/repository-signatures/{resourceVersion}/index.json",
            defaults: new { controller = "RepositorySignatures", action = "Index" });

        endpoints.MapControllerRoute(
            name: Routes.RepositorySigningCertificateRouteName,
            pattern: "v3/repository-signatures/certificates/{fingerprint}.crt",
            defaults: new { controller = "RepositorySignatures", action = "Certificate" });
    }

    public void MapServiceIndexRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.IndexRouteName,
            pattern: "v3/index.json",
            defaults: new { controller = "ServiceIndex", action = "Get" });
    }

    public void MapPackagePublishRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.UploadPackageRouteName,
            pattern: "api/v2/package",
            defaults: new { controller = "PackagePublish", action = "Upload" },
            constraints: new { httpMethod = new HttpMethodRouteConstraint("PUT") });

        endpoints.MapControllerRoute(
            name: Routes.DeleteRouteName,
            pattern: "api/v2/package/{id}/{version}",
            defaults: new { controller = "PackagePublish", action = "Delete" },
            constraints: new { httpMethod = new HttpMethodRouteConstraint("DELETE") });

        endpoints.MapControllerRoute(
            name: Routes.RelistRouteName,
            pattern: "api/v2/package/{id}/{version}",
            defaults: new { controller = "PackagePublish", action = "Relist" },
            constraints: new { httpMethod = new HttpMethodRouteConstraint("POST") });
    }

    public void MapSymbolRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.UploadSymbolRouteName,
            pattern: "api/v2/symbol",
            defaults: new { controller = "Symbol", action = "Upload" },
            constraints: new { httpMethod = new HttpMethodRouteConstraint("PUT") });

        endpoints.MapControllerRoute(
            name: Routes.SymbolDownloadRouteName,
            pattern: "api/download/symbols/{file}/{key}/{file2}",
            defaults: new { controller = "Symbol", action = "Get" });

        endpoints.MapControllerRoute(
            name: Routes.PrefixedSymbolDownloadRouteName,
            pattern: "api/download/symbols/{prefix}/{file}/{key}/{file2}",
            defaults: new { controller = "Symbol", action = "Get" });
    }

    public void MapSearchRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.SearchRouteName,
            pattern: "v3/search",
            defaults: new { controller = "Search", action = "Search" });

        endpoints.MapControllerRoute(
            name: Routes.AutocompleteRouteName,
            pattern: "v3/autocomplete",
            defaults: new { controller = "Search", action = "Autocomplete" });

        // This is an unofficial API to find packages that depend on a given package.
        endpoints.MapControllerRoute(
            name: Routes.DependentsRouteName,
            pattern: "v3/dependents",
            defaults: new { controller = "Search", action = "Dependents" });
    }

    public void MapPackageMetadataRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
           name: Routes.RegistrationIndexRouteName,
           pattern: "v3/registration/{id}/index.json",
           defaults: new { controller = "PackageMetadata", action = "RegistrationIndex" });

        endpoints.MapControllerRoute(
            name: Routes.RegistrationLeafRouteName,
            pattern: "v3/registration/{id}/{version}.json",
            defaults: new { controller = "PackageMetadata", action = "RegistrationLeaf" });

        // RegistrationsBaseUrl/3.6.0 includes SemVer 2.0.0 packages. The other variants must not.
        endpoints.MapControllerRoute(
           name: Routes.RegistrationSemVer2IndexRouteName,
           pattern: "v3/registration-semver2/{id}/index.json",
           defaults: new { controller = "PackageMetadata", action = "RegistrationIndex", semVer2 = true });

        endpoints.MapControllerRoute(
            name: Routes.RegistrationSemVer2LeafRouteName,
            pattern: "v3/registration-semver2/{id}/{version}.json",
            defaults: new { controller = "PackageMetadata", action = "RegistrationLeaf", semVer2 = true });
    }

    public void MapPackageContentRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: Routes.PackageVersionsRouteName,
            pattern: "v3/package/{id}/index.json",
            defaults: new { controller = "PackageContent", action = "GetPackageVersions" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadRouteName,
            pattern: "v3/package/{id}/{version}/{idVersion}.nupkg",
            defaults: new { controller = "PackageContent", action = "DownloadPackage" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadManifestRouteName,
            pattern: "v3/package/{id}/{version}/{id2}.nuspec",
            defaults: new { controller = "PackageContent", action = "DownloadNuspec" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadReadmeRouteName,
            pattern: "v3/package/{id}/{version}/readme",
            defaults: new { controller = "PackageContent", action = "DownloadReadme" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadIconRouteName,
            pattern: "v3/package/{id}/{version}/icon",
            defaults: new { controller = "PackageContent", action = "DownloadIcon" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadLicenseTextRouteName,
            pattern: "v3/package/{id}/{version}/license.txt",
            defaults: new { controller = "PackageContent", action = "DownloadLicense" });

        endpoints.MapControllerRoute(
            name: Routes.PackageDownloadLicenseMarkDownRouteName,
            pattern: "v3/package/{id}/{version}/license.md",
            defaults: new { controller = "PackageContent", action = "DownloadLicense" });
    }
}
