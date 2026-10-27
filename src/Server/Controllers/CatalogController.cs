using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NuGetFeed.Catalog;
using NuGetFeed.Models;

namespace NuGetFeed.Controllers;

/// <summary>
/// The Catalog resource, a record of every change to the feed's packages.
/// See: https://learn.microsoft.com/en-us/nuget/api/catalog-resource
/// </summary>
public class CatalogController : Controller
{
    private readonly ICatalogService _catalog;

    public CatalogController(ICatalogService catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    // GET v3/catalog/index.json
    [HttpGet]
    public async Task<CatalogIndex> IndexAsync(CancellationToken cancellationToken)
    {
        return await _catalog.GetIndexAsync(cancellationToken);
    }

    // GET v3/catalog/page{page}.json
    [HttpGet]
    public async Task<ActionResult<CatalogPage>> PageAsync(int page, CancellationToken cancellationToken)
    {
        var result = await _catalog.GetPageOrNullAsync(page, cancellationToken);
        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    // GET v3/catalog/data/{key}/{id}.{version}.json
    [HttpGet]
    public async Task<IActionResult> LeafAsync(int key, CancellationToken cancellationToken)
    {
        var leaf = await _catalog.GetLeafOrNullAsync(key, cancellationToken);
        if (leaf == null)
        {
            return NotFound();
        }

        // Serialize the leaf as its runtime type: package details or package delete.
        return Ok((object)leaf);
    }
}
