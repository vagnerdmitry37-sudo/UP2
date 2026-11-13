using System.Text.Json.Serialization;

namespace UP.Api.RichText;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RichTextAttribute : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return new RichTextJsonConverter();
    }
}
