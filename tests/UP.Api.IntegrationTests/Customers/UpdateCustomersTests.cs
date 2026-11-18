using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.Customers;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Customers;

namespace UP.Api.IntegrationTests.Customers;

public sealed class UpdateCustomersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "manager@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Updated_customers_are_returned_in_request_order_and_saved()
    {
        string accessToken = await SignInAsync();
        Guid annaId = await CreateCustomerAsync(accessToken, "Anna Smith", "anna@example.com");
        Guid petrId = await CreateCustomerAsync(accessToken, "Petr Novak", "petr@example.com");

        HttpResponseMessage response = await Client.UpdateCustomersAsync(
            accessToken,
            new { id = petrId, name = "Petr Svoboda", email = "petr@mail.cz", phone = "+420 777 000 002" },
            new { id = annaId, name = "Anna Dvorak", email = "anna@example.com", phone = "+44 20 7946 0001" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IReadOnlyList<CustomerResponse> updated = await response.ReadCustomersAsync();
        updated.Select(customer => customer.Id).ShouldBe([petrId, annaId]);
        updated[0].Email.ShouldBe("petr@mail.cz");
        updated[1].Name.ShouldBe("Anna Dvorak");
        Customer saved = await QueryDatabaseAsync(db =>
            db.Customers.SingleAsync(customer => customer.Id == petrId, CancellationToken));
        saved.Name.ShouldBe("Petr Svoboda");
        saved.Email.ShouldBe("petr@mail.cz");
        saved.Phone.ShouldBe("+420 777 000 002");
    }

    [Fact]
    public async Task Email_of_another_customer_is_refused_with_conflict_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid annaId = await CreateCustomerAsync(accessToken, "Anna Smith", "anna@example.com");
        await CreateCustomerAsync(accessToken, "Petr Novak", "petr@example.com");

        HttpResponseMessage response = await Client.UpdateCustomersAsync(
            accessToken,
            new { id = annaId, name = "Anna Dvorak", email = "petr@example.com", phone = "+44 20 7946 0000" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await ShouldBeUnchangedAsync(annaId, "Anna Smith", "anna@example.com");
    }

    [Fact]
    public async Task Unknown_customer_is_refused_with_not_found_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid annaId = await CreateCustomerAsync(accessToken, "Anna Smith", "anna@example.com");

        HttpResponseMessage response = await Client.UpdateCustomersAsync(
            accessToken,
            new { id = annaId, name = "Anna Dvorak", email = "anna@example.com", phone = "+44 20 7946 0000" },
            new { id = Guid.CreateVersion7(), name = "Ghost", email = "ghost@example.com", phone = "+1" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await ShouldBeUnchangedAsync(annaId, "Anna Smith", "anna@example.com");
    }

    [Fact]
    public async Task Same_customer_twice_is_refused_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        Guid annaId = await CreateCustomerAsync(accessToken, "Anna Smith", "anna@example.com");

        HttpResponseMessage response = await Client.UpdateCustomersAsync(
            accessToken,
            new { id = annaId, name = "Anna Dvorak", email = "anna@example.com", phone = "+44 20 7946 0000" },
            new { id = annaId, name = "Anna Novak", email = "anna.novak@example.com", phone = "+44 20 7946 0000" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedAsync(annaId, "Anna Smith", "anna@example.com");
    }

    [Fact]
    public async Task Updating_customers_without_sign_in_is_rejected_and_saves_nothing()
    {
        Guid annaId = await CreateCustomerAsync(await SignInAsync(), "Anna Smith", "anna@example.com");

        HttpResponseMessage response = await Client.UpdateCustomersAsync(
            accessToken: null,
            new { id = annaId, name = "Anna Dvorak", email = "anna@example.com", phone = "+44 20 7946 0000" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await ShouldBeUnchangedAsync(annaId, "Anna Smith", "anna@example.com");
    }

    private async Task<Guid> CreateCustomerAsync(string accessToken, string name, string email)
    {
        HttpResponseMessage response = await Client.CreateCustomerAsync(
            accessToken,
            CustomersApi.NewCustomer(name, email));
        response.EnsureSuccessStatusCode();
        return (await response.ReadCustomerAsync()).Id;
    }

    private async Task ShouldBeUnchangedAsync(Guid id, string name, string email)
    {
        Customer saved = await QueryDatabaseAsync(db =>
            db.Customers.SingleAsync(customer => customer.Id == id, CancellationToken));
        saved.Name.ShouldBe(name);
        saved.Email.ShouldBe(email);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
