using System;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Infrastructure.Services
{
    public interface IAstroService
    {
        int CalculateGunaMatching(Profile profileA, Profile profileB);
    }

    public class AstroService : IAstroService
    {
        public int CalculateGunaMatching(Profile profileA, Profile profileB)
        {
            // Astrological matching algorithm simulating Guna Milap (maximum 36 points).
            // A score of 18 or above is considered compatible.
            // Uses a deterministic hash of user names so the score remains consistent for a pair.
            if (string.IsNullOrEmpty(profileA.FullName) || string.IsNullOrEmpty(profileB.FullName))
            {
                return 18; // Default neutral match score
            }

            int hash = (profileA.FullName + profileB.FullName).GetHashCode();
            var random = new Random(hash);
            return random.Next(14, 36);
        }
    }
}
