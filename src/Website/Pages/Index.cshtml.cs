using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGetFeed.Models;
using NuGetFeed.Search;

namespace NuGetFeed.Pages;

public class IndexModel : PageModel
{
    private readonly ISearchClient _search;
    private readonly FeedConnection _feed;

    public IndexModel(ISearchClient search, FeedConnection feed)
    {
        _search = search ?? throw new ArgumentNullException(nameof(search));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
    }

    public const int ResultsPerPage = 20;

    [BindProperty(Name = "q", SupportsGet = true)]
    public string Query { get; set; }

    [BindProperty(Name = "p", SupportsGet = true)]
    [Range(1, int.MaxValue)]
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string PackageType { get; set; } = "any";

    [BindProperty(SupportsGet = true)]
    public string Framework { get; set; } = "any";

    [BindProperty(SupportsGet = true)]
    public bool Prerelease { get; set; } = true;

    public IReadOnlyList<SearchResult> Packages { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();

        var packageType = PackageType == "any" ? null : PackageType;
        var framework = Framework == "any" ? null : Framework;

        var search = await _search.SearchAsync(
            Query,
            skip: (PageIndex - 1) * ResultsPerPage,
            take: ResultsPerPage,
            includePrerelease: Prerelease,
            includeSemVer2: true,
            packageType,
            framework,
            cancellationToken);

        Packages = search.Data ?? Array.Empty<SearchResult>();

        foreach (var package in Packages)
        {
            package.IconUrl = _feed.ToPublicUrl(package.IconUrl);
        }

        return Page();
    }
}
