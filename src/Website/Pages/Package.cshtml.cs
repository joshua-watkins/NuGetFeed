using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGet.Frameworks;
using NuGet.Versioning;
using NuGetFeed.Extensions;
using NuGetFeed.Models;
using NuGetFeed.PackageDetails;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetFeed.Pages;

public class PackageModel : PageModel
{
    private static readonly MarkdownPipeline MarkdownPipeline;

    private readonly IPackageDetailsClient _packages;
    private readonly FeedConnection _feed;

    static PackageModel()
    {
        MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public PackageModel(IPackageDetailsClient packages, FeedConnection feed)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
    }

    public bool Found { get; private set; }

    public NuGetFeedPackageMetadata Package { get; private set; }
    public NuGetVersion Version { get; private set; }

    public bool IsDotnetTemplate { get; private set; }
    public bool IsDotnetTool { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public long TotalDownloads { get; private set; }

    public IReadOnlyList<PackageDependent> UsedBy { get; set; }
    public IReadOnlyList<DependencyGroupModel> DependencyGroups { get; private set; }
    public IReadOnlyList<VersionModel> Versions { get; private set; }

    public HtmlString Readme { get; private set; }

    public string IconUrl { get; private set; }
    public string LicenseUrl { get; private set; }
    public string PackageDownloadUrl { get; private set; }

    public async Task OnGetAsync(string id, string version, CancellationToken cancellationToken)
    {
        var packages = await _packages.GetPackageMetadataAsync(id, cancellationToken);
        var listedPackages = packages.Where(p => p.IsListed()).ToList();

        // Try to find the requested version.
        if (NuGetVersion.TryParse(version, out var requestedVersion))
        {
            Package = packages.SingleOrDefault(p => p.ParseVersion() == requestedVersion);
        }

        // Otherwise try to display the latest version.
        if (Package == null)
        {
            Package = listedPackages.OrderByDescending(p => p.ParseVersion()).FirstOrDefault();
        }

        if (Package == null)
        {
            Package = new NuGetFeedPackageMetadata { PackageId = id };
            Found = false;
            return;
        }

        Version = Package.ParseVersion();

        var packageTypes = Package.PackageTypes ?? Array.Empty<string>();

        Found = true;
        IsDotnetTemplate = packageTypes.Any(t => t.Equals("Template", StringComparison.OrdinalIgnoreCase));
        IsDotnetTool = packageTypes.Any(t => t.Equals("DotnetTool", StringComparison.OrdinalIgnoreCase));
        LastUpdated = packages.Max(p => p.Published).UtcDateTime;
        TotalDownloads = packages.Sum(p => p.Downloads);

        var dependents = await _packages.GetPackageDependentsAsync(Package.PackageId, cancellationToken);

        UsedBy = dependents.Data;
        DependencyGroups = ToDependencyGroups(Package);
        Versions = ToVersions(listedPackages, Version);

        if (Package.HasReadme)
        {
            Readme = await GetReadmeHtmlStringOrNullAsync(Package.PackageId, Version, cancellationToken);
        }

        IconUrl = _feed.ToPublicUrl(Package.IconUrl);
        LicenseUrl = _feed.ToPublicUrl(Package.LicenseUrl);
        PackageDownloadUrl = _feed.ToPublicUrl(Package.PackageContentUrl);
    }

    private IReadOnlyList<DependencyGroupModel> ToDependencyGroups(NuGetFeedPackageMetadata package)
    {
        return (package.DependencyGroups ?? Array.Empty<DependencyGroupItem>())
            .Select(group => new DependencyGroupModel
            {
                Name = PrettifyTargetFramework(group.TargetFramework),
                Dependencies = (group.Dependencies ?? new List<DependencyItem>())
                    .Where(d => d.Id != null)
                    .Select(d => new DependencyModel
                    {
                        PackageId = d.Id,
                        VersionSpec = (d.Range != null)
                            ? VersionRange.Parse(d.Range).PrettyPrint()
                            : string.Empty
                    })
                    .ToList()
            })
            .ToList();
    }

    private string PrettifyTargetFramework(string targetFramework)
    {
        if (targetFramework == null) return "All Frameworks";

        NuGetFramework framework;
        try
        {
            framework = NuGetFramework.Parse(targetFramework);
        }
        catch (Exception)
        {
            return targetFramework;
        }

        string frameworkName;
        if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = (framework.Version.Major >= 5)
                ? ".NET"
                : ".NET Core";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetStandard,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Standard";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.Net,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Framework";
        }
        else
        {
            frameworkName = framework.Framework;
        }

        var frameworkVersion = (framework.Version.Build == 0)
            ? framework.Version.ToString(2)
            : framework.Version.ToString(3);

        return $"{frameworkName} {frameworkVersion}";
    }

    private IReadOnlyList<VersionModel> ToVersions(IReadOnlyList<NuGetFeedPackageMetadata> packages, NuGetVersion selectedVersion)
    {
        return packages
            .Select(p => new VersionModel
            {
                Version = p.ParseVersion(),
                Downloads = p.Downloads,
                Selected = p.ParseVersion() == selectedVersion,
                LastUpdated = p.Published.UtcDateTime,
            })
            .OrderByDescending(m => m.Version)
            .ToList();
    }

    private async Task<HtmlString> GetReadmeHtmlStringOrNullAsync(
        string packageId,
        NuGetVersion packageVersion,
        CancellationToken cancellationToken)
    {
        string readme;
        using (var readmeStream = await _packages.GetPackageReadmeStreamOrNullAsync(packageId, packageVersion, cancellationToken))
        {
            if (readmeStream == null) return null;

            using (var reader = new StreamReader(readmeStream))
            {
                readme = await reader.ReadToEndAsync();
            }
        }

        var readmeHtml = Markdown.ToHtml(readme, MarkdownPipeline);
        return new HtmlString(readmeHtml);
    }

    public class DependencyGroupModel
    {
        public string Name { get; set; }
        public IReadOnlyList<DependencyModel> Dependencies { get; set; }
    }

    // TODO: Convert this to records.
    public class DependencyModel
    {
        public string PackageId { get; set; }
        public string VersionSpec { get; set; }
    }

    // TODO: Convert this to records.
    public class VersionModel
    {
        public NuGetVersion Version { get; set; }
        public long Downloads { get; set; }
        public bool Selected { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
