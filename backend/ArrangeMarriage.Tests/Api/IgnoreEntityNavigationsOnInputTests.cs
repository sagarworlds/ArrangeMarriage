using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ArrangeMarriage.API.Serialization;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>The JSON rule that keeps request bodies from smuggling related entities into the object graph.</summary>
    public class IgnoreEntityNavigationsOnInputTests
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { IgnoreEntityNavigationsOnInput.Modify } }
        };

        [Fact]
        public void Deserializing_ReadsScalarValues_ButIgnoresANestedEntity()
        {
            const string json = """
                { "fullName": "Asha Rao", "city": "Pune",
                  "user": { "userId": "11111111-1111-1111-1111-111111111111", "role": 0, "isVerified": true },
                  "familyDetail": { "fatherName": "smuggled" } }
                """;

            var profile = JsonSerializer.Deserialize<Profile>(json, Options)!;

            Assert.Equal("Asha Rao", profile.FullName);
            Assert.Equal("Pune", profile.City);
            Assert.Null(profile.User);          // declared non-nullable, but never populated from input
            Assert.Null(profile.FamilyDetail);
        }

        [Fact]
        public void Deserializing_IgnoresCollectionsOfEntities()
        {
            const string json = """
                { "username": "asha", "documents": [ { "documentType": "smuggled" } ] }
                """;

            var user = JsonSerializer.Deserialize<User>(json, Options)!;

            Assert.Equal("asha", user.Username);
            Assert.Empty(user.Documents);
        }

        [Fact]
        public void Deserializing_ForeignKeyIdsStillWork()
        {
            var proposalId = Guid.NewGuid();

            var meeting = JsonSerializer.Deserialize<Meeting>($$"""{ "proposalId": "{{proposalId}}", "venue": "Hall" }""", Options)!;

            Assert.Equal(proposalId, meeting.ProposalId);
            Assert.Equal("Hall", meeting.Venue);
        }

        [Fact]
        public void Serializing_StillIncludesNavigationProperties()
        {
            var profile = new Profile { FullName = "Asha Rao", FamilyDetail = new FamilyDetail { FatherName = "R. Rao" } };

            var json = JsonSerializer.Serialize(profile, Options);

            Assert.Contains("\"fatherName\":\"R. Rao\"", json);
        }

        [Fact]
        public void TypesOutsideTheDomain_AreLeftAlone()
        {
            // Navigation-like properties on other types (request DTOs, framework types) must keep working normally.
            var result = JsonSerializer.Deserialize<Wrapper>("""{ "profile": { "fullName": "Asha" }, "items": [ { "fullName": "B" } ] }""", Options)!;

            Assert.Equal("Asha", result.Profile!.FullName);
            Assert.Single(result.Items);
        }

        private sealed class Wrapper
        {
            public Profile? Profile { get; set; }
            public List<Profile> Items { get; set; } = new();
        }
    }
}
