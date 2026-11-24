using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.ReferenceData;

namespace UP.Api.IntegrationTests.ReferenceData;

internal static class ReferenceDataApi
{
    private const string ReferenceDataRoute = "api/reference-data";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> GetReferenceDataAsync(this HttpClient client) =>
        client.GetAsync(ReferenceDataRoute, CancellationToken);

    public static async Task<ReferenceDataResponse> ReadReferenceDataAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ReferenceDataResponse>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no reference data.");
}
