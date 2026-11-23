using System;
using System.Security.Claims;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Security
{
    public static class CurrentUserExtensions
    {
        /// <summary>Returns the authenticated user's id from the token, or null if absent/malformed.</summary>
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }

        /// <summary>True when the caller is an Admin or Staff member.</summary>
        public static bool IsStaff(this ClaimsPrincipal principal) =>
            principal.IsInRole(nameof(UserRole.Admin)) || principal.IsInRole(nameof(UserRole.Staff));

        /// <summary>True when the caller is <paramref name="userId"/> or is staff.</summary>
        public static bool CanAccessUser(this ClaimsPrincipal principal, Guid userId) =>
            principal.IsStaff() || principal.GetUserId() == userId;
    }
}
