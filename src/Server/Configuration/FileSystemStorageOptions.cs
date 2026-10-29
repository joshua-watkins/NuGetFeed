namespace NuGetFeed.Configuration;

public class FileSystemStorageOptions
{
    /// <summary>
    /// The path at which content will be stored. Defaults to the same path
    /// as the main NuGetFeed executable. This path will be created if it does not
    /// exist at startup. Packages will be stored in a subfolder named "packages".
    /// </summary>
    public string? Path { get; set; }
}
