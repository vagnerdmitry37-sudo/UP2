using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class MeTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Me_returns_signed_in_user()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        Guid userId = await QueryDatabaseAsync(db => db.Users
            .Where(user => user.Email == Email)
            .Select(user => user.Id)
            .SingleAsync(CancellationToken));

        HttpResponseMessage response = await Client.GetMeAsync(token.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeResponse? me = await response.Content.ReadFromJsonAsync<MeResponse>(CancellationToken);
        me.ShouldNotBeNull();
        me.Id.ShouldBe(userId);
        me.Email.ShouldBe(Email);
    }

    [Fact]
    public async Task Me_response_is_never_cached()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage response = await Client.GetMeAsync(token.AccessToken);

        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Me_without_access_token_is_rejected()
    {
        HttpResponseMessage response = await Client.GetMeAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_other_authentication_scheme_is_rejected()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage response = await Client.GetMeAsync(new AuthenticationHeaderValue("Basic", token.AccessToken));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_malformed_token_is_rejected()
    {
        HttpResponseMessage response = await Client.GetMeAsync("not-a-jwt");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_tampered_token_is_rejected()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        string forged = TestJwt.Create(Guid.CreateVersion7(), "mallory@example.com", signingKey: TestJwt.OtherSigningKey);

        HttpResponseMessage response = await Client.GetMeAsync(TestJwt.WithPayloadOf(token.AccessToken, forged));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_expired_token_is_rejected()
    {
        string expired = TestJwt.Create(Guid.CreateVersion7(), Email, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        HttpResponseMessage response = await Client.GetMeAsync(expired);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_unsigned_token_is_rejected()
    {
        string unsigned = TestJwt.Create(Guid.CreateVersion7(), Email, signingKey: null);

        HttpResponseMessage response = await Client.GetMeAsync(unsigned);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_signed_by_other_key_is_rejected()
    {
        string foreign = TestJwt.Create(Guid.CreateVersion7(), Email, signingKey: TestJwt.OtherSigningKey);

        HttpResponseMessage response = await Client.GetMeAsync(foreign);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_from_other_issuer_is_rejected()
    {
        string foreign = TestJwt.Create(Guid.CreateVersion7(), Email, issuer: "other-issuer");

        HttpResponseMessage response = await Client.GetMeAsync(foreign);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_for_other_audience_is_rejected()
    {
        string foreign = TestJwt.Create(Guid.CreateVersion7(), Email, audience: "other-audience");

        HttpResponseMessage response = await Client.GetMeAsync(foreign);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_without_user_id_is_rejected()
    {
        string incomplete = TestJwt.Create(userId: null, Email);

        HttpResponseMessage response = await Client.GetMeAsync(incomplete);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_without_email_is_rejected()
    {
        string incomplete = TestJwt.Create(Guid.CreateVersion7(), email: null);

        HttpResponseMessage response = await Client.GetMeAsync(incomplete);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_refresh_token_instead_of_access_token_is_rejected()
    {
        await Client.RegisterAsync(Email, Password);
        string refreshToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));

        HttpResponseMessage response = await Client.GetMeAsync(refreshToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
