using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGetFeed.Extensions;
using NuGetFeed.ServiceIndex;

namespace NuGetFeed.Pages;

public class UploadModel : PageModel
{
    private readonly IServiceIndexClient _serviceIndex;
    private readonly FeedConnection _feed;

    public UploadModel(IServiceIndexClient serviceIndex, FeedConnection feed)
    {
        _serviceIndex = serviceIndex ?? throw new ArgumentNullException(nameof(serviceIndex));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
    }

    public string ServiceIndexUrl { get; private set; }

    public string PublishUrl { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var serviceIndex = await _serviceIndex.GetAsync(cancellationToken);

        ServiceIndexUrl = _feed.ToPublicUrl(_feed.ServiceIndexUrl);
        PublishUrl = _feed.ToPublicUrl(serviceIndex.GetPackagePublishResourceUrl());
    }
}
