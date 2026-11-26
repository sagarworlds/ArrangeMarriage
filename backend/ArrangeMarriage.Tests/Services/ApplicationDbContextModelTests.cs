using ArrangeMarriage.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Services
{
    /// <summary>Guards the EF model itself: an invalid model makes every database call throw at runtime.</summary>
    public class ApplicationDbContextModelTests
    {
        [Fact]
        public void Model_Builds_AndEveryEntityHasAPrimaryKey()
        {
            using var context = TestDb.Create();

            var entityTypes = context.Model.GetEntityTypes().ToList();

            Assert.NotEmpty(entityTypes);
            Assert.All(entityTypes, e => Assert.True(e.FindPrimaryKey() != null, $"{e.DisplayName()} has no primary key"));
        }
    }
}
