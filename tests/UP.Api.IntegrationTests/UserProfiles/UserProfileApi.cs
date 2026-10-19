using System.Net.Http.Headers;
using System.Net.Http.Json;

using UP.Api.UserProfiles;

namespace UP.Api.IntegrationTests.UserProfiles;

internal static class UserProfileApi
{
    public const string UserProfileRoute = "api/user-profile";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static async Task<HttpResponseMessage> GetUserProfileAsync(this HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserProfileRoute);
        Authorize(request, accessToken);
        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateUserProfileAsync(this HttpClient client, string accessToken, string currency)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, UserProfileRoute)
        {
            Content = JsonContent.Create(new { currency }),
        };
        Authorize(request, accessToken);
        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<UserProfileResponse> ReadUserProfileAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<UserProfileResponse>(CancellationToken)
        ?? throw new InvalidOperationException("Response has no user profile.");

    private static void Authorize(HttpRequestMessage request, string? accessToken)
    {
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
