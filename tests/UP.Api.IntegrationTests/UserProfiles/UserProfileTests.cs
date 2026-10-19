using System.Net;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.UserProfiles;

namespace UP.Api.IntegrationTests.UserProfiles;

public sealed class UserProfileTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task New_user_gets_default_currency()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.GetUserProfileAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserProfileResponse profile = await response.ReadUserProfileAsync();
        profile.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task Changed_currency_is_returned_afterwards()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage updateResponse = await Client.UpdateUserProfileAsync(accessToken, "EUR");
        HttpResponseMessage getResponse = await Client.GetUserProfileAsync(accessToken);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await updateResponse.ReadUserProfileAsync()).Currency.ShouldBe("EUR");
        (await getResponse.ReadUserProfileAsync()).Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task Currency_can_be_changed_again()
    {
        string accessToken = await SignInAsync();

        await Client.UpdateUserProfileAsync(accessToken, "EUR");
        await Client.UpdateUserProfileAsync(accessToken, "GBP");
        HttpResponseMessage response = await Client.GetUserProfileAsync(accessToken);

        (await response.ReadUserProfileAsync()).Currency.ShouldBe("GBP");
    }

    [Fact]
    public async Task User_profile_without_sign_in_is_rejected()
    {
        HttpResponseMessage response = await Client.GetUserProfileAsync(accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
