using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace NuGetFeed;

/// <summary>
/// The website's connection to the NuGet feed it displays. The website only talks
/// to the feed through the NuGet V3 protocol.
/// </summary>
public class FeedConnection
{
    private const string DefaultServiceIndexPath = "v3/index.json";

    private readonly IServer? _server;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly bool _isCoHosted;
    private readonly Lazy<string> _serviceIndexUrl;
    private readonly Lazy<NuGetClientFactory> _clientFactory;

    public FeedConnection(
        IOptions<NuGetWebsiteOptions> options,
        IServer? server,
        IHttpContextAccessor? httpContextAccessor)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        _server = server;
        _httpContextAccessor = httpContextAccessor;

        var configuredUrl = options.Value.ServiceIndexUrl;

        // The server's listening addresses are only known once the application has started,
        // so resolve them on first use.
        _isCoHosted = string.IsNullOrEmpty(configuredUrl);
        _serviceIndexUrl = new Lazy<string>(() => _isCoHosted ? GetLocalServiceIndexUrl() : configuredUrl!);
        _clientFactory = new Lazy<NuGetClientFactory>(() => new NuGetClientFactory(CreateHttpClient(), ServiceIndexUrl));
    }

    /// <summary>
    /// The service index URL the website uses to call the feed.
    /// </summary>
    public string ServiceIndexUrl => _serviceIndexUrl.Value;

    /// <summary>
    /// Creates clients for the feed's resources.
    /// </summary>
    public NuGetClientFactory ClientFactory => _clientFactory.Value;

    /// <summary>
    /// Convert a URL returned by the feed into a URL the browser can use.
    /// </summary>
    /// <param name="url">A URL returned by the feed.</param>
    /// <returns>The URL to show in the page, or null if <paramref name="url"/> is empty.</returns>
    public string? ToPublicUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // A feed configured explicitly is reached by browsers the same way the website reaches it.
        if (!_isCoHosted) return url;

        var request = _httpContextAccessor?.HttpContext?.Request;
        if (request == null) return url;

        // The co-hosted server generates links using the local address the website called it on.
        // Swap that address for the one the browser used to reach this application.
        var feedRoot = new Uri(ServiceIndexUrl).GetLeftPart(UriPartial.Authority);
        if (!url.StartsWith(feedRoot + "/", StringComparison.OrdinalIgnoreCase)) return url;

        var publicRoot = string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent());

        return publicRoot + url.Substring(feedRoot.Length);
    }

    private string GetLocalServiceIndexUrl()
    {
        var addresses = _server?.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses == null || addresses.Count == 0)
        {
            throw new InvalidOperationException(
                "Could not determine the address this application listens on. " +
                $"Set '{NuGetWebsiteOptions.DefaultSection}:{nameof(NuGetWebsiteOptions.ServiceIndexUrl)}' " +
                "to the feed's service index URL.");
        }

        // Prefer HTTP so the call isn't rejected by a certificate issued for the public host name.
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? addresses.First();

        // Kestrel reports wildcard bindings such as http://*:5000, http://+:5000 or http://[::]:5000.
        address = address
            .Replace("://*", "://localhost")
            .Replace("://+", "://localhost");

        var builder = new UriBuilder(address);
        if (builder.Host is "0.0.0.0" or "[::]" or "::")
        {
            builder.Host = "localhost";
        }

        builder.Path = DefaultServiceIndexPath;

        return builder.Uri.AbsoluteUri;
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });
    }
}
