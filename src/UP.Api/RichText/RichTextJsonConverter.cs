using System.Text.Json;
using System.Text.Json.Serialization;

using Ganss.Xss;

namespace UP.Api.RichText;

internal sealed class RichTextJsonConverter : JsonConverter<string>
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public override bool HandleNull => true;

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string html = reader.GetString() ?? string.Empty;
        return Sanitizer.Sanitize(html).Replace("&nbsp;", " ", StringComparison.Ordinal).Trim();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "h2", "h3", "strong", "em", "u", "s", "ul", "ol", "li", "blockquote", "a",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
        };

        return new HtmlSanitizer(options);
    }
}
