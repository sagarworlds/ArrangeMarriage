using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class AstroService : IAstroService
    {
        private const int MaxScore = 36;
        private const int MinSimulatedScore = 14;
        private const int NeutralScore = 18;

        /// <inheritdoc />
        public int CalculateGunaMatching(Profile profileA, Profile profileB)
        {
            if (string.IsNullOrEmpty(profileA.FullName) || string.IsNullOrEmpty(profileB.FullName))
            {
                return NeutralScore;
            }

            // string.GetHashCode() is randomized per process in .NET, which made scores change on
            // every restart. SHA-256 is stable. Names are ordered so A-B and B-A score identically.
            var ordered = string.CompareOrdinal(profileA.FullName, profileB.FullName) <= 0
                ? profileA.FullName + "|" + profileB.FullName
                : profileB.FullName + "|" + profileA.FullName;
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(ordered));
            var value = BinaryPrimitives.ReadUInt32LittleEndian(digest);

            // Range is inclusive of the 36-point maximum.
            return MinSimulatedScore + (int)(value % (MaxScore - MinSimulatedScore + 1));
        }
    }
}
