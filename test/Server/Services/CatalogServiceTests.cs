using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NuGetFeed.Catalog;
using NuGetFeed.Entities;
using NuGetFeed.Extensions;
using NuGetFeed.Storage;
using Xunit;

namespace NuGetFeed.Server.Tests.Services
{
    public class CatalogServiceTests : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "NuGetFeedTests", Guid.NewGuid().ToString("N") + ".db");
        private readonly NuGetFeedContext _context;
        private readonly CatalogService _target;

        public CatalogServiceTests()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            var connectionString = new SqliteConnectionStringBuilder { DataSource = _path, Pooling = false };
            var options = new DbContextOptionsBuilder<NuGetFeedContext>()
                .UseSqlite(connectionString.ToString())
                .Options;

            _context = new NuGetFeedContext(options);
            _context.RunMigrationsAsync(default).GetAwaiter().GetResult();

            _target = new CatalogService(
                _context,
                Mock.Of<IUrlGenerator>(),
                Mock.Of<IPackageStorageService>(),
                new SystemTime(),
                NullLogger<CatalogService>.Instance);
        }

        public void Dispose()
        {
            _context.Dispose();
            File.Delete(_path);
        }

        [Fact]
        public async Task PagesHoldMaxPageSizeItems()
        {
            for (var i = 0; i <= CatalogService.MaxPageSize; i++)
            {
                _context.CatalogEntries.Add(new CatalogEntry
                {
                    CommitId = Guid.NewGuid().ToString(),
                    CommitTimestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i),
                    Type = CatalogEntry.PackageDeleteType,
                    PackageId = "Package",
                    PackageVersion = $"1.0.{i}",
                });
            }

            await _context.SaveChangesAsync();

            var index = await _target.GetIndexAsync(default);
            var first = await _target.GetPageOrNullAsync(0, default);
            var second = await _target.GetPageOrNullAsync(1, default);

            Assert.Equal(2, index.Count);
            Assert.Equal(CatalogService.MaxPageSize, first!.Items.Count);
            Assert.Equal("1.0.0", first.Items.First().PackageVersion);
            Assert.Equal($"1.0.{CatalogService.MaxPageSize}", Assert.Single(second!.Items).PackageVersion);
            Assert.Null(await _target.GetPageOrNullAsync(2, default));
        }
    }
}
