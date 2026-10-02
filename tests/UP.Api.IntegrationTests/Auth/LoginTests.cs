using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

using UP.Api.Auth;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class LoginTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";
    private const string WrongPassword = "wrong-horse-battery";
    private const int FailedAttemptsBeforeLockout = 5;

    [Fact]
    public async Task Registered_user_receives_access_token()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokenResponse token = await response.ReadAccessTokenAsync();
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
        token.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Refresh_cookie_is_hidden_from_scripts_and_cross_site_requests()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        SetCookieHeaderValue cookie = RefreshCookie.Find(response).ShouldNotBeNull();
        cookie.HttpOnly.ShouldBeTrue();
        cookie.Secure.ShouldBeTrue();
        cookie.SameSite.ShouldBe(SameSiteMode.Strict);
        cookie.Path.Value.ShouldBe("/api/auth");
        cookie.Expires.ShouldNotBeNull().ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Access_token_grants_access_to_current_user()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage me = await Client.GetMeAsync(token.AccessToken);

        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_without_session()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage response = await Client.LoginAsync(Email, WrongPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        RefreshCookie.Find(response).ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_email_is_rejected_without_session()
    {
        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        RefreshCookie.Find(response).ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_look_same()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage wrongPassword = await Client.LoginAsync(Email, WrongPassword);
        HttpResponseMessage unknownEmail = await Client.LoginAsync("nobody@example.com", WrongPassword);

        await ShouldLookSameAsync(unknownEmail, wrongPassword);
    }

    [Fact]
    public async Task Email_is_matched_ignoring_letter_case()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage response = await Client.LoginAsync(Email.ToUpperInvariant(), Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Password_is_case_sensitive()
    {
        await Client.RegisterAsync(Email, Password);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password.ToUpperInvariant());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Account_locks_after_five_wrong_passwords()
    {
        await Client.RegisterAsync(Email, Password);
        await FailLoginAsync(FailedAttemptsBeforeLockout);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Locked_account_looks_same_as_wrong_password()
    {
        await Client.RegisterAsync(Email, Password);
        HttpResponseMessage wrongPassword = await Client.LoginAsync(Email, WrongPassword);
        await FailLoginAsync(FailedAttemptsBeforeLockout - 1);

        HttpResponseMessage locked = await Client.LoginAsync(Email, Password);

        await ShouldLookSameAsync(locked, wrongPassword);
    }

    [Fact]
    public async Task Fewer_than_five_wrong_passwords_do_not_lock_account()
    {
        await Client.RegisterAsync(Email, Password);
        await FailLoginAsync(FailedAttemptsBeforeLockout - 1);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Successful_login_resets_failed_attempts()
    {
        await Client.RegisterAsync(Email, Password);
        await FailLoginAsync(FailedAttemptsBeforeLockout - 1);
        await Client.LoginAsync(Email, Password);
        await FailLoginAsync(FailedAttemptsBeforeLockout - 1);

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lockout_does_not_end_existing_session()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        HttpClient attacker = CreateSessionClient();
        for (int attempt = 0; attempt < FailedAttemptsBeforeLockout; attempt++)
        {
            await attacker.LoginAsync(Email, WrongPassword);
        }

        HttpResponseMessage refresh = await Client.RefreshAsync();

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Account_unlocks_when_lockout_expires()
    {
        await Client.RegisterAsync(Email, Password);
        await FailLoginAsync(FailedAttemptsBeforeLockout);
        await QueryDatabaseAsync(db => db.Users.ExecuteUpdateAsync(
            setters => setters.SetProperty(user => user.LockoutEnd, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken));

        HttpResponseMessage response = await Client.LoginAsync(Email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Each_login_starts_independent_session()
    {
        HttpClient laptop = CreateSessionClient();
        HttpClient phone = CreateSessionClient();
        await laptop.RegisterAndLoginAsync(Email, Password);
        await phone.LoginAsync(Email, Password);

        await laptop.LogoutAsync();

        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("not-an-email", Password)]
    [InlineData("", Password)]
    [InlineData(Email, "")]
    public async Task Invalid_input_is_rejected(string email, string password)
    {
        HttpResponseMessage response = await Client.LoginAsync(email, password);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task FailLoginAsync(int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            (await Client.LoginAsync(Email, WrongPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    private static async Task ShouldLookSameAsync(HttpResponseMessage actual, HttpResponseMessage expected)
    {
        actual.StatusCode.ShouldBe(expected.StatusCode);
        ProblemDetails? actualProblem = await actual.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        ProblemDetails? expectedProblem = await expected.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        actualProblem.ShouldNotBeNull();
        expectedProblem.ShouldNotBeNull();
        actualProblem.Title.ShouldBe(expectedProblem.Title);
        actualProblem.Type.ShouldBe(expectedProblem.Type);
        actualProblem.Detail.ShouldBe(expectedProblem.Detail);
    }
}
