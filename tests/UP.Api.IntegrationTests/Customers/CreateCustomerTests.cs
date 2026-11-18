using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.Customers;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Domain.Customers;

namespace UP.Api.IntegrationTests.Customers;

public sealed class CreateCustomerTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "manager@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Created_customer_is_returned_and_saved()
    {
        string accessToken = await SignInAsync();

        HttpResponseMessage response = await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        CustomerResponse created = await response.ReadCustomerAsync();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe("Anna Smith");
        created.Email.ShouldBe("anna@example.com");
        created.Phone.ShouldBe("+44 20 7946 0000");
        created.CreatedAt.ShouldNotBe(default);
        Customer? saved = await QueryDatabaseAsync(db =>
            db.Customers.SingleOrDefaultAsync(customer => customer.Id == created.Id, CancellationToken));
        saved.ShouldNotBeNull().Email.ShouldBe("anna@example.com");
    }

    [Fact]
    public async Task Customer_with_taken_email_is_refused_and_saves_nothing()
    {
        string accessToken = await SignInAsync();
        await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer(name: "Anna Smith"));

        HttpResponseMessage response = await Client.CreateCustomerAsync(
            accessToken,
            CustomersApi.NewCustomer(name: "Another Anna"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        int savedCount = await QueryDatabaseAsync(db => db.Customers.CountAsync(CancellationToken));
        savedCount.ShouldBe(1);
    }

    [Fact]
    public async Task Creating_customer_without_sign_in_is_rejected()
    {
        HttpResponseMessage response = await Client.CreateCustomerAsync(accessToken: null, CustomersApi.NewCustomer());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        int savedCount = await QueryDatabaseAsync(db => db.Customers.CountAsync(CancellationToken));
        savedCount.ShouldBe(0);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
