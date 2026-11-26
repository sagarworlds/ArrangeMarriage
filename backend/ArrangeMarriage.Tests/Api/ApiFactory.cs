using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArrangeMarriage.API.Dtos;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>A signed-in test user: their id plus a bearer token issued by the real login endpoint.</summary>
    public sealed record TestUser(Guid UserId, string Email, string Password, string Token);

    /// <summary>
    /// Hosts the real API in-process with the same pipeline as production (auth, controllers, SignalR),
    /// replacing only PostgreSQL with an isolated in-memory database and supplying test secrets.
    /// </summary>
    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public const string WebhookSecret = "integration-test-webhook-secret";
        public const string DefaultPassword = "Passw0rd!-for-tests";

        private readonly string _databaseName = Guid.NewGuid().ToString();

        /// <summary>User ids (as strings) whose SignalR connect work should be artificially slowed, and by how much.</summary>
        public ConcurrentDictionary<string, TimeSpan> SlowConnectUsers { get; } = new();
        private int _userCounter;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // "Testing" keeps Development-only behaviour (Swagger, dev secrets) out of the picture.
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789-abcdef");
            builder.UseSetting("Payments:WebhookSecret", WebhookSecret);

            builder.ConfigureServices(services =>
            {
                services.Configure<HubOptions>(options => options.AddFilter(new SlowConnectHubFilter(SlowConnectUsers)));

                // EF Core 9+ keeps every AddDbContext configuration callback, so removing only the options
                // object would leave UseNpgsql in place next to UseInMemoryDatabase. Matching the internal
                // IDbContextOptionsConfiguration<> registration by name avoids depending on its namespace.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                var leftovers = services
                    .Where(d => d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")
                        && d.ServiceType.GenericTypeArguments[0] == typeof(ApplicationDbContext))
                    .ToList();
                foreach (var descriptor in leftovers) services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }

        /// <summary>Runs <paramref name="action"/> against the app's database in a fresh DI scope.</summary>
        public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            using var scope = Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }

        /// <summary>
        /// Creates a user straight in the database (so any role can be used) and signs in through the
        /// real login endpoint to obtain a genuine token.
        /// </summary>
        public async Task<TestUser> CreateUserAsync(UserRole role = UserRole.Bride, bool verified = true)
        {
            var n = Interlocked.Increment(ref _userCounter);
            var email = $"user{n}@example.com";

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                db.Users.Add(new User
                {
                    Username = $"user{n}",
                    Email = email,
                    PhoneNumber = $"90000{n:D5}",
                    Role = role,
                    IsVerified = verified,
                    PasswordHash = hasher.Hash(DefaultPassword)
                });
                await db.SaveChangesAsync();
            }

            return await LoginAsync(email, DefaultPassword);
        }

        /// <summary>Signs in via the API and returns the user with their bearer token.</summary>
        public async Task<TestUser> LoginAsync(string email, string password)
        {
            using var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/user/login", new LoginRequest { Email = email, Password = password });
            response.EnsureSuccessStatusCode();
            var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
            return new TestUser(body.User.UserId, email, password, body.Token);
        }

        /// <summary>A client that sends <paramref name="user"/>'s bearer token (or none when null).</summary>
        public HttpClient ClientFor(TestUser? user)
        {
            var client = CreateClient();
            if (user != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
            }
            return client;
        }
    }
}
