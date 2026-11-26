using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.API.Dtos;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Registration, login and the access rules around user accounts.</summary>
    public class UserApiTests : IClassFixture<ApiFactory>
    {
        private static int _phoneSeed = 10_000_000;

        private readonly ApiFactory _factory;

        public UserApiTests(ApiFactory factory) => _factory = factory;

        private static RegisterRequest ValidRegistration(string name, UserRole role = UserRole.Groom) => new()
        {
            Username = name,
            Email = $"{name}@example.com",
            PhoneNumber = $"98{Interlocked.Increment(ref _phoneSeed)}", // unique per call; phone numbers must be unique
            Password = "Str0ng-Passw0rd!",
            Role = role
        };

        [Theory]
        [InlineData("/api/user/{0}")]
        [InlineData("/api/user/unverified")]
        [InlineData("/api/profile/user/{0}")]
        [InlineData("/api/payment/user/{0}")]
        [InlineData("/api/payment/receipt/{0}")]
        [InlineData("/api/matchmaking/search/{0}")]
        [InlineData("/api/matchmaking/user/{0}")]
        [InlineData("/api/meeting/user/{0}")]
        [InlineData("/api/message/history/{0}/{1}")]
        public async Task ProtectedEndpoints_WithoutToken_Return401(string pathFormat)
        {
            using var client = _factory.ClientFor(null);

            var response = await client.GetAsync(string.Format(pathFormat, Guid.NewGuid(), Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_CreatesUnverifiedAccount_StoresOnlyAHash_AndNeverReturnsIt()
        {
            using var client = _factory.ClientFor(null);
            var request = ValidRegistration("newbride", UserRole.Bride);

            var response = await client.PostAsJsonAsync("/api/user/register", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(request.Password, body);

            var stored = await _factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == request.Email));
            Assert.NotEqual(request.Password, stored.PasswordHash);
            Assert.DoesNotContain(request.Password, stored.PasswordHash);
            Assert.False(stored.IsVerified);
            Assert.Equal(UserRole.Bride, stored.Role);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.Staff)]
        public async Task Register_PrivilegedRole_IsRejected(UserRole role)
        {
            using var client = _factory.ClientFor(null);

            var response = await client.PostAsJsonAsync("/api/user/register", ValidRegistration($"sneaky{role}", role));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("email")]
        [InlineData("username")]
        [InlineData("phone number")]
        public async Task Register_DuplicateIdentifier_Returns409(string duplicated)
        {
            using var client = _factory.ClientFor(null);
            var first = ValidRegistration($"dup-{duplicated.Replace(' ', '-')}");
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/user/register", first)).StatusCode);

            var second = ValidRegistration($"other-{duplicated.Replace(' ', '-')}");
            if (duplicated == "email") second.Email = first.Email;
            if (duplicated == "username") second.Username = first.Username;
            if (duplicated == "phone number") second.PhoneNumber = first.PhoneNumber;

            var response = await client.PostAsJsonAsync("/api/user/register", second);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains(duplicated, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("short-password")]
        [InlineData("bad-email")]
        [InlineData("no-phone")]
        [InlineData("long-username")]
        public async Task Register_InvalidInput_Returns400(string problem)
        {
            using var client = _factory.ClientFor(null);
            var request = ValidRegistration($"invalid-{problem}");
            switch (problem)
            {
                case "short-password": request.Password = "short"; break;
                case "bad-email": request.Email = "not-an-email"; break;
                case "no-phone": request.PhoneNumber = ""; break;
                case "long-username": request.Username = new string('x', 51); break;
            }

            var response = await client.PostAsJsonAsync("/api/user/register", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsAWorkingToken()
        {
            var user = await _factory.CreateUserAsync(); // goes through the real login endpoint

            using var client = _factory.ClientFor(user);
            var response = await client.GetAsync($"/api/user/{user.UserId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("passwordHash", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_WrongPasswordOrUnknownEmail_GiveTheSame401()
        {
            var user = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(null);

            var wrongPassword = await client.PostAsJsonAsync("/api/user/login", new LoginRequest { Email = user.Email, Password = "wrong-password" });
            var unknownEmail = await client.PostAsJsonAsync("/api/user/login", new LoginRequest { Email = "nobody@example.com", Password = "whatever" });

            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
            // Identical bodies so the response can't be used to discover which emails have accounts.
            Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Login_AfterRegistering_Works()
        {
            using var client = _factory.ClientFor(null);
            var request = ValidRegistration("fullflow", UserRole.Family);
            await client.PostAsJsonAsync("/api/user/register", request);

            var response = await client.PostAsJsonAsync("/api/user/login", new LoginRequest { Email = request.Email, Password = request.Password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
            Assert.False(string.IsNullOrWhiteSpace(body.Token));
            Assert.Equal(request.Email, body.User.Email);
        }

        [Fact]
        public async Task GetUser_ForSomeoneElse_IsForbidden_ButAllowedForStaffAndSelf()
        {
            var alice = await _factory.CreateUserAsync();
            var bob = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);

            using var asBob = _factory.ClientFor(bob);
            using var asStaff = _factory.ClientFor(staff);
            using var asAlice = _factory.ClientFor(alice);

            Assert.Equal(HttpStatusCode.Forbidden, (await asBob.GetAsync($"/api/user/{alice.UserId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await asStaff.GetAsync($"/api/user/{alice.UserId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await asAlice.GetAsync($"/api/user/{alice.UserId}")).StatusCode);
        }

        [Fact]
        public async Task VerifyUser_RequiresStaff_AndMarksTheUserVerified()
        {
            var member = await _factory.CreateUserAsync(verified: false);
            var other = await _factory.CreateUserAsync();
            var admin = await _factory.CreateUserAsync(UserRole.Admin);

            using var asMember = _factory.ClientFor(member);
            using var asOther = _factory.ClientFor(other);
            using var asAdmin = _factory.ClientFor(admin);

            // Users must not be able to verify themselves or each other.
            Assert.Equal(HttpStatusCode.Forbidden, (await asMember.PutAsync($"/api/user/verify/{member.UserId}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await asOther.PutAsync($"/api/user/verify/{member.UserId}", null)).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await asAdmin.PutAsync($"/api/user/verify/{member.UserId}", null)).StatusCode);
            Assert.True((await _factory.WithDbAsync(db => db.Users.SingleAsync(u => u.UserId == member.UserId))).IsVerified);

            Assert.Equal(HttpStatusCode.NotFound, (await asAdmin.PutAsync($"/api/user/verify/{Guid.NewGuid()}", null)).StatusCode);
        }

        [Fact]
        public async Task Unverified_RequiresStaff_AndListsOnlyUnverifiedUsersWithoutHashes()
        {
            var pending = await _factory.CreateUserAsync(verified: false);
            var verified = await _factory.CreateUserAsync(verified: true);
            var staff = await _factory.CreateUserAsync(UserRole.Staff);

            using var asMember = _factory.ClientFor(verified);
            using var asStaff = _factory.ClientFor(staff);

            Assert.Equal(HttpStatusCode.Forbidden, (await asMember.GetAsync("/api/user/unverified")).StatusCode);

            var response = await asStaff.GetAsync("/api/user/unverified");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = (await response.Content.ReadFromJsonAsync<List<UserResponse>>())!;
            Assert.Contains(users, u => u.UserId == pending.UserId);
            Assert.DoesNotContain(users, u => u.UserId == verified.UserId);
            Assert.DoesNotContain("passwordHash", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
