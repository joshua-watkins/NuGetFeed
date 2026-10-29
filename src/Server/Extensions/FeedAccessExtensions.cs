using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NuGetFeed.Authentication;
using NuGetFeed.Configuration;

namespace NuGetFeed.Extensions;

/// <summary>
/// Turns on the feed's API keys. Without these, the feed is open.
/// </summary>
public static class FeedAccessExtensions
{
    /// <summary>
    /// Requires a valid API key for what requests without one may not do. Any key may do everything,
    /// unless the application also adds <see cref="AddNuGetFeedPolicies"/>.
    /// </summary>
    public static AuthenticationBuilder AddNuGetFeedApiKey(this AuthenticationBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        // Authenticate requests by their API key, unless the application authenticates them another way.
        builder.Services.Configure<AuthenticationOptions>(options => options.DefaultScheme ??= ApiKeyAuthenticationHandler.SchemeName);

        return builder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName,
            configureOptions: null);
    }

    /// <summary>
    /// Gives each API key its role. Requires <see cref="AddNuGetFeedApiKey"/>.
    /// </summary>
    public static AuthorizationBuilder AddNuGetFeedPolicies(this AuthorizationBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, FeedPermissionHandler>());

        return builder.AddFeedPolicies();
    }

    /// <summary>
    /// Whether the configuration has API keys, in <see cref="NuGetFeedOptions.ApiKey"/>
    /// or <see cref="FeedAuthorizationOptions.ApiKeys"/>.
    /// </summary>
    public static bool HasNuGetFeedApiKeys(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        return !string.IsNullOrEmpty(configuration[nameof(NuGetFeedOptions.ApiKey)]) ||
            configuration
                .GetSection($"{nameof(NuGetFeedOptions.Authorization)}:{nameof(FeedAuthorizationOptions.ApiKeys)}")
                .GetChildren()
                .Any(apiKey => !string.IsNullOrEmpty(apiKey[nameof(ApiKeyOptions.Key)]));
    }
}
