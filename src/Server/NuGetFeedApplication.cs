using System;
using Microsoft.Extensions.DependencyInjection;

namespace NuGetFeed;

public class NuGetFeedApplication
{
    public NuGetFeedApplication(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}
