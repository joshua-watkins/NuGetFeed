using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NuGetFeed.Entities;

/// <summary>
/// Creates the context for Entity Framework's tools, like "dotnet ef migrations add --project src/Server".
/// The server is a library without a host, so the tools can't get the context from its services.
/// The migrations are SQLite's, so the context uses SQLite. The tools don't open the database.
/// </summary>
public class NuGetFeedContextDesignTimeFactory : IDesignTimeDbContextFactory<NuGetFeedContext>
{
    public NuGetFeedContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NuGetFeedContext>()
            .UseSqlite("Data Source=nuget.db")
            .Options;

        return new NuGetFeedContext(options);
    }
}
