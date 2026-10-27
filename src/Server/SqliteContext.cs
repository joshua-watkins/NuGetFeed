using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NuGetFeed.Entities;

namespace NuGetFeed;

public class SqliteContext : AbstractContext<SqliteContext>
{
    /// <summary>
    /// The Sqlite error code for when a unique constraint is violated.
    /// </summary>
    private const int SqliteUniqueConstraintViolationErrorCode = 19;

    public SqliteContext(DbContextOptions<SqliteContext> options)
        : base(options)
    { }

    public override async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        // SQLite creates the database file, but not the folder it lives in.
        var dataSource = new SqliteConnectionStringBuilder(Database.GetConnectionString()).DataSource;
        if (!string.IsNullOrEmpty(dataSource) && dataSource != ":memory:")
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await base.RunMigrationsAsync(cancellationToken);
    }

    public override bool IsUniqueConstraintViolationException(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException &&
            sqliteException.SqliteErrorCode == SqliteUniqueConstraintViolationErrorCode;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Package>()
            .Property(p => p.Id)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<Package>()
            .Property(p => p.NormalizedVersionString)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<PackageDependency>()
            .Property(d => d.Id)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<PackageType>()
            .Property(t => t.Name)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<TargetFramework>()
            .Property(f => f.Moniker)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<VulnerabilityAdvisory>()
            .Property(a => a.PackageId)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<CatalogEntry>()
            .Property(e => e.PackageId)
            .HasColumnType("TEXT COLLATE NOCASE");

        builder.Entity<CatalogEntry>()
            .Property(e => e.PackageVersion)
            .HasColumnType("TEXT COLLATE NOCASE");
    }
}
