using System.Collections.Generic;
using NuGetFeed.Authentication;

namespace NuGetFeed.Configuration;

/// <summary>
/// Who may do what on the feed. Clients present an API key in the X-NuGet-ApiKey header, or as the
/// password of HTTP Basic authentication, which is how NuGet sends a package source's credentials
/// from a nuget.config file. The application must add the feed's authentication for these settings,
/// and its authorization policies for the roles (see <see cref="FeedAccessMode"/>).
/// </summary>
public class FeedAuthorizationOptions
{
    /// <summary>
    /// What requests without a valid API key may do. If unset, <see cref="FeedRole.ReadOnly"/>.
    /// </summary>
    public FeedRole? AnonymousRole { get; set; }

    /// <summary>
    /// The API keys that grant access to the feed. <see cref="NuGetFeedOptions.ApiKey"/>, if set,
    /// is also an <see cref="FeedRole.Admin"/> key.
    /// </summary>
    public List<ApiKeyOptions> ApiKeys { get; set; } = new();
}

public class ApiKeyOptions
{
    /// <summary>
    /// Who or what uses the key, like "Build server". It appears in the logs.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The secret clients present.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// What the key may do. Required with the feed's authorization policies. Without them,
    /// any key may do everything, so only <see cref="FeedRole.Admin"/> is allowed.
    /// </summary>
    public FeedRole? Role { get; set; }
}
