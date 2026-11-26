using System;
using System.Collections;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Serialization
{
    /// <summary>
    /// Stops request bodies from filling in navigation properties of domain entities.
    /// <para>
    /// Several endpoints bind a domain entity straight from the body. EF Core treats a nested object as part of
    /// the graph and saves it too, so a body such as <c>{ "userId": "...", "user": { "role": "Admin", ... } }</c>
    /// would create an admin account (or re-parent another user's rows) as a side effect of an innocent request.
    /// Clients refer to related records by id, so navigation properties are only ever written in responses.
    /// </para>
    /// </summary>
    public static class IgnoreEntityNavigationsOnInput
    {
        private static readonly string EntityNamespace = typeof(User).Namespace!;

        /// <summary>
        /// A <see cref="System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver"/> modifier that makes
        /// navigation properties of domain entities write-protected when deserializing. Serialization is unchanged.
        /// </summary>
        public static void Modify(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object || !IsEntity(typeInfo.Type)) return;

            foreach (var property in typeInfo.Properties.Where(p => IsNavigation(p.PropertyType)))
            {
                // Without a setter the serializer skips the JSON value instead of assigning it.
                property.Set = null;
            }
        }

        private static bool IsEntity(Type type) => type.IsClass && type.Namespace == EntityNamespace;

        private static bool IsNavigation(Type type) =>
            IsEntity(type)
            || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Any(IsEntity));
    }
}
