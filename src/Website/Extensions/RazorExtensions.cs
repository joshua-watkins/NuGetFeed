using Humanizer;
using NuGetFeed.Extensions;

namespace NuGetFeed.Extensions;

public static class RazorExtensions
{
    public static string ToMetric(this long value)
    {
        return ((double) value).ToMetric();
    }
}
