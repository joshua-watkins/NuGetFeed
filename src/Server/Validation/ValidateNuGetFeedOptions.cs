using System.Collections.Generic;
using Microsoft.Extensions.Options;
using NuGetFeed.Configuration;

namespace NuGetFeed.Validation;

/// <summary>
/// Validates the options NuGetFeed reads from its configuration.
/// </summary>
public class ValidateNuGetFeedOptions
    : IValidateOptions<DatabaseOptions>
    , IValidateOptions<MirrorOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add(Invalid(nameof(NuGetFeedOptions.Database), $"The {nameof(DatabaseOptions.ConnectionString)} field is required."));
        }

        return Result(failures);
    }

    public ValidateOptionsResult Validate(string? name, MirrorOptions options)
    {
        var failures = new List<string>();

        if (options.PackageDownloadTimeoutSeconds < 0)
        {
            failures.Add(Invalid(
                nameof(NuGetFeedOptions.Mirror),
                $"The field {nameof(MirrorOptions.PackageDownloadTimeoutSeconds)} must be between 0 and {int.MaxValue}."));
        }

        if (options.Enabled && options.PackageSource == null)
        {
            failures.Add(Invalid(
                nameof(NuGetFeedOptions.Mirror),
                $"The {nameof(MirrorOptions.PackageSource)} configuration is required if mirroring is enabled"));
        }

        return Result(failures);
    }

    private static string Invalid(string section, string message) => $"Invalid '{section}' configs: {message}";

    private static ValidateOptionsResult Result(List<string> failures)
        => failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
}
