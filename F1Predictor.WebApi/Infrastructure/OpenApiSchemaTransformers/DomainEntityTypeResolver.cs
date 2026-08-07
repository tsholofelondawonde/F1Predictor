using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace F1Predictor.WebApi.Infrastructure.OpenApiSchemaTransformers;

/// <summary>
/// Custom JSON type info resolver that handles domain entities with required properties but private setters.
/// This resolver prevents serialization errors during OpenAPI schema generation.
/// </summary>
internal sealed class DomainEntityTypeResolver : DefaultJsonTypeInfoResolver
{
    private const string DomainNamespace = "F1Predictor.Domain";

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        // If this is a domain entity type, modify its property requirements
        if (type.Namespace?.StartsWith(DomainNamespace, StringComparison.Ordinal) == true)
        {
            // Remove required constraint from properties with private setters
            foreach (var property in typeInfo.Properties)
            {
                var propertyInfo = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.SetMethod?.IsPrivate == true)
                {
                    // Mark as not required to allow schema generation
                    property.IsRequired = false;
                }
            }
        }

        return typeInfo;
    }
}
