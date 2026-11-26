using System.ComponentModel.DataAnnotations;
using ArrangeMarriage.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace ArrangeMarriage.API.Validation
{
    /// <summary>
    /// With nullable reference types enabled, MVC treats every non-nullable reference property as [Required].
    /// Domain entities declare their navigation properties as <c>User User { get; set; } = null!</c>, so any
    /// endpoint that binds an entity from the request body (Profile, Payment, Meeting, ...) would return
    /// "The User field is required" unless the client embedded the whole related object.
    /// Clients identify related records by foreign-key id, so navigation properties to other domain
    /// entities are never required in request bodies. Validation of every other property is unchanged.
    /// </summary>
    public class EntityNavigationValidationMetadataProvider : IValidationMetadataProvider
    {
        private static readonly string EntityNamespace = typeof(User).Namespace!;

        /// <inheritdoc />
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context.Key.MetadataKind != ModelMetadataKind.Property) return;

            var type = context.Key.ModelType;
            if (!type.IsClass || type.Namespace != EntityNamespace) return;

            context.ValidationMetadata.IsRequired = false;

            // ValidatorMetadata is an IList<object>, so remove in reverse to keep indexes valid.
            var validators = context.ValidationMetadata.ValidatorMetadata;
            for (var i = validators.Count - 1; i >= 0; i--)
            {
                if (validators[i] is RequiredAttribute) validators.RemoveAt(i);
            }
        }
    }
}
