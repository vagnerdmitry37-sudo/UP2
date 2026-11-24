using UP.Api.Auth;
using UP.Api.Customers;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Customers;

public sealed class GetCustomersTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "manager@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Global_filter_finds_customers_by_part_of_name_email_or_phone_ignoring_case()
    {
        string accessToken = await SignInAsync();
        await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer("Anna Novak", "anna@example.com", "+420 601 000 001"));
        await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer("Petr Svoboda", "petr@mail.cz", "+420 777 000 002"));
        await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer("Jana Dvorak", "jana@example.com", "+44 20 7946 0003"));

        CustomersPageResponse byName = await SearchAsync(accessToken, "NOVAK");
        CustomersPageResponse byEmail = await SearchAsync(accessToken, "mail.cz");
        CustomersPageResponse byPhone = await SearchAsync(accessToken, "7946");

        byName.Items.Select(customer => customer.Name).ShouldBe(["Anna Novak"]);
        byName.TotalRecords.ShouldBe(1);
        byEmail.Items.Select(customer => customer.Name).ShouldBe(["Petr Svoboda"]);
        byPhone.Items.Select(customer => customer.Name).ShouldBe(["Jana Dvorak"]);
    }

    [Fact]
    public async Task Global_filter_treats_like_wildcards_as_plain_text()
    {
        string accessToken = await SignInAsync();
        await Client.CreateCustomerAsync(accessToken, CustomersApi.NewCustomer("Anna Novak", "anna@example.com"));

        CustomersPageResponse page = await SearchAsync(accessToken, "%");

        page.Items.ShouldBeEmpty();
    }

    private async Task<CustomersPageResponse> SearchAsync(string accessToken, string globalFilter)
    {
        HttpResponseMessage response = await Client.GetCustomersAsync(
            accessToken,
            $"globalFilter={Uri.EscapeDataString(globalFilter)}");
        response.EnsureSuccessStatusCode();
        return await response.ReadCustomersPageAsync();
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }
}
