using System;
using Microsoft.AspNetCore.Builder;
using NuGetFeed;

namespace NuGetFeed.Extensions;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOperationCancelledMiddleware(this IApplicationBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        return app.UseMiddleware<OperationCancelledMiddleware>();
    }
}
