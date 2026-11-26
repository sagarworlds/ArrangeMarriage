using ArrangeMarriage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Support
{
    /// <summary>Creates isolated in-memory <see cref="ApplicationDbContext"/> instances for tests.</summary>
    public static class TestDb
    {
        /// <summary>
        /// Returns a context backed by a uniquely named in-memory database, so tests never share state.
        /// Pass the same <paramref name="databaseName"/> twice to get a second context over the same data.
        /// </summary>
        public static ApplicationDbContext Create(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
