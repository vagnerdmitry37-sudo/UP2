using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Http;

using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class RegisterTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";
    private const string OtherPassword = "another-strong-password";

    [Fact]
    public async Task New_user_can_register_and_log_in()
    {
        HttpResponseMessage registration = await Client.RegisterAsync(Email, Password);
        HttpResponseMessage login = await Client.LoginAsync(Email, Password);

        registration.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Registering_taken_email_looks_same_as_new_registration()
    {
        HttpResponseMessage first = await Client.RegisterAsync(Email, Password);
        HttpResponseMessage second = await Client.RegisterAsync(Email, OtherPassword);

        string firstBody = await first.Content.ReadAsStringAsync(CancellationToken);
        string secondBody = await second.Content.ReadAsStringAsync(CancellationToken);

        second.StatusCode.ShouldBe(first.StatusCode);
        secondBody.ShouldBe(firstBody);
    }

    [Fact]
    public async Task Registering_taken_email_does_not_change_existing_account()
    {
        await Client.RegisterAsync(Email, Password);
        await Client.RegisterAsync(Email, OtherPassword);

        (await Client.LoginAsync(Email, Password)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.LoginAsync(Email, OtherPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Email_comparison_ignores_letter_case()
    {
        await Client.RegisterAsync(Email, Password);
        HttpResponseMessage upperCaseRegistration = await Client.RegisterAsync(Email.ToUpperInvariant(), OtherPassword);

        upperCaseRegistration.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await Client.LoginAsync(Email.ToUpperInvariant(), OtherPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.LoginAsync(Email.ToUpperInvariant(), Password)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Weak_password_is_rejected_with_reason()
    {
        HttpResponseMessage response = await Client.RegisterAsync(Email, "short");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Values.SelectMany(messages => messages)
            .ShouldContain(message => message.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Rejected_registration_does_not_create_account()
    {
        await Client.RegisterAsync(Email, "short");

        (await Client.LoginAsync(Email, "short")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local-part.com")]
    public async Task Invalid_email_is_rejected(string email)
    {
        HttpResponseMessage response = await Client.RegisterAsync(email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_fields_are_rejected()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(AuthApi.RegisterRoute, new { }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Too_long_email_is_rejected()
    {
        string email = new string('a', 250) + "@example.com";

        HttpResponseMessage response = await Client.RegisterAsync(email, Password);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
