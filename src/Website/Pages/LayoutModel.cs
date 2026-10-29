namespace NuGetFeed.Pages;

/// <summary>
/// The pages' shared layout: the header with the navigation and search box.
/// </summary>
/// <param name="Title">The page's title.</param>
/// <param name="PathBase">The application's path base, which links start with.</param>
/// <param name="ActivePage">The navigation link to highlight.</param>
/// <param name="SearchQuery">The search box's value.</param>
public record LayoutModel(string Title, string PathBase, WebsitePage ActivePage = WebsitePage.None, string? SearchQuery = null)
{
    /// <summary>
    /// The URL of one of the website's files, like "images/github-32x32.png".
    /// </summary>
    public string Content(string path) => $"{PathBase}{WebsiteFiles.RequestPath}/{path}";
}

/// <summary>
/// The pages in the website's navigation.
/// </summary>
public enum WebsitePage
{
    None,
    Packages,
    Upload,
}

/// <summary>
/// Shown when the website couldn't get the page's content from the feed.
/// </summary>
/// <param name="PathBase">The application's path base, which links start with.</param>
/// <param name="ServiceIndexUrl">The feed the website reads from.</param>
public record FeedErrorModel(string PathBase, string ServiceIndexUrl);
