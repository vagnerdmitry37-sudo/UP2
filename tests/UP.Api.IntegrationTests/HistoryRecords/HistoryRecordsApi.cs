using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using UP.Api.HistoryRecords;

namespace UP.Api.IntegrationTests.HistoryRecords;

internal static class HistoryRecordsApi
{
    public const string HistoryRecordsRoute = "api/history-records";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static async Task<HttpResponseMessage> GetHistoryRecordsAsync(
        this HttpClient client,
        string? accessToken,
        string entityType,
        Guid entityId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{HistoryRecordsRoute}?entityType={entityType}&entityId={entityId}");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<List<HistoryRecordResponse>> ReadHistoryRecordsAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<HistoryRecordResponse>>(JsonOptions, CancellationToken)
        ?? throw new InvalidOperationException("Response has no history records.");
}
