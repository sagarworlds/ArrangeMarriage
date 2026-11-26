using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Services;

namespace ArrangeMarriage.Tests.Services
{
    public class AstroServiceTests
    {
        private readonly AstroService _service = new();

        private static Profile Named(string name) => new() { FullName = name };

        [Fact]
        public void CalculateGunaMatching_IsDeterministic()
        {
            var first = new AstroService().CalculateGunaMatching(Named("Asha Rao"), Named("Vikram Shah"));
            var second = new AstroService().CalculateGunaMatching(Named("Asha Rao"), Named("Vikram Shah"));

            Assert.Equal(first, second);
        }

        [Fact]
        public void CalculateGunaMatching_IsSymmetric()
        {
            Assert.Equal(
                _service.CalculateGunaMatching(Named("Asha Rao"), Named("Vikram Shah")),
                _service.CalculateGunaMatching(Named("Vikram Shah"), Named("Asha Rao")));
        }

        [Fact]
        public void CalculateGunaMatching_StaysWithinRange_AndIncludesBothBounds()
        {
            var scores = Enumerable.Range(0, 2000)
                .Select(i => _service.CalculateGunaMatching(Named($"Bride {i}"), Named($"Groom {i}")))
                .ToList();

            Assert.All(scores, s => Assert.InRange(s, 14, 36));
            Assert.Contains(14, scores);
            Assert.Contains(36, scores); // the documented maximum must be reachable
        }

        [Theory]
        [InlineData("", "Vikram")]
        [InlineData("Asha", "")]
        public void CalculateGunaMatching_WithMissingName_ReturnsNeutralScore(string a, string b)
        {
            Assert.Equal(18, _service.CalculateGunaMatching(Named(a), Named(b)));
        }
    }
}
