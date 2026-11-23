using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    /// <summary>Computes horoscope compatibility between two profiles.</summary>
    public interface IAstroService
    {
        /// <summary>Returns a Guna Milap style score from 0 to 36 (18+ is compatible).</summary>
        int CalculateGunaMatching(Profile profileA, Profile profileB);
    }
}
