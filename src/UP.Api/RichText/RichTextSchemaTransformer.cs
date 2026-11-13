using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace UP.Api.RichText;

internal sealed class RichTextSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        bool isRichText = context.JsonPropertyInfo?.AttributeProvider?.IsDefined(typeof(RichTextAttribute), inherit: false) ?? false;
        if (isRichText)
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "html";
        }

        return Task.CompletedTask;
    }
}
