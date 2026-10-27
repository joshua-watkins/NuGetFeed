using System;
using Microsoft.AspNetCore.Mvc;

namespace NuGetFeed.Controllers;

/// <summary>
/// NuGetFeed only serves the NuGet V3 protocol. This answers NuGet V2 (OData) feed
/// requests with a message that points clients to the V3 service index.
/// </summary>
public class V2FeedController : Controller
{
    private readonly IUrlGenerator _url;

    public V2FeedController(IUrlGenerator url)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
    }

    // GET api/v2/{**path}
    [HttpGet]
    [HttpHead]
    public IActionResult NotSupported()
    {
        return NotFound(
            "The NuGet V2 (OData) feed is not supported. " +
            $"Use the NuGet V3 service index at {_url.GetServiceIndexUrl()} instead.");
    }
}
