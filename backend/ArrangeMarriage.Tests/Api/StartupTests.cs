using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Configuration that must be present for the API to start at all.</summary>
    public class StartupTests
    {
        [Fact]
        public void Startup_WithoutASigningKey_FailsFastWithAClearMessage()
        {
            // The "Testing" environment loads no dev key, and appsettings.json deliberately ships an empty one.
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("Jwt:Key", error.Message);
        }

        [Fact]
        public void Startup_WithAShortSigningKey_IsRejected()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Jwt:Key", "too-short");
            });

            Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        }
    }
}
