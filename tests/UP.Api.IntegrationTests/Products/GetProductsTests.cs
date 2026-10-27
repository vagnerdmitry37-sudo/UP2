using System.Net;

using UP.Api.Auth;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.Products;

public sealed class GetProductsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";

    [Fact]
    public async Task Products_page_is_empty_when_nothing_was_created()
    {
        HttpResponseMessage response = await Client.GetProductsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProductsPageResponse page = await response.ReadProductsPageAsync();
        page.Items.ShouldBeEmpty();
        page.TotalRecords.ShouldBe(0);
    }

    [Fact]
    public async Task Products_can_be_listed_without_sign_in()
    {
        HttpResponseMessage response = await Client.GetProductsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Products_are_listed_in_creation_order_by_default()
    {
        string token = await SignInAsync();
        await Client.CreateProductsAsync(token, ProductsApi.NewProduct(name: "Old Town Walk"));
        await Client.CreateProductsAsync(token, ProductsApi.NewProduct(type: "Transfer", name: "Airport to hotel"));

        ProductsPageResponse page = await GetPageAsync();

        page.Items.Select(product => product.Name).ShouldBe(["Old Town Walk", "Airport to hotel"]);
        page.Items[1].Type.ShouldBe(ProductType.Transfer);
        page.Items[0].Attributes.GetProperty("durationMinutes").GetInt32().ShouldBe(180);
    }

    [Fact]
    public async Task Page_returns_only_requested_range_with_total_of_all_products()
    {
        string token = await SignInAsync();
        await CreateProductsAsync(token, "Item 1", "Item 2", "Item 3", "Item 4", "Item 5");

        ProductsPageResponse page = await GetPageAsync("sortField=name&first=1&rows=2");

        page.Items.Select(product => product.Name).ShouldBe(["Item 2", "Item 3"]);
        page.TotalRecords.ShouldBe(5);
    }

    [Fact]
    public async Task Page_past_the_end_is_empty_but_keeps_total()
    {
        string token = await SignInAsync();
        await CreateProductsAsync(token, "One", "Two");

        ProductsPageResponse page = await GetPageAsync("first=10&rows=5");

        page.Items.ShouldBeEmpty();
        page.TotalRecords.ShouldBe(2);
    }

    [Fact]
    public async Task Page_has_fifty_products_when_rows_are_not_given()
    {
        string token = await SignInAsync();
        await CreateProductsAsync(token, [.. Enumerable.Range(1, 51).Select(number => $"Product {number}")]);

        ProductsPageResponse page = await GetPageAsync();

        page.Items.Count.ShouldBe(50);
        page.TotalRecords.ShouldBe(51);
    }

    [Fact]
    public async Task Products_are_sorted_by_requested_field_and_order()
    {
        string token = await SignInAsync();
        await Client.CreateProductsAsync(
            token,
            ProductsApi.NewProduct(name: "Cheap", price: 10m),
            ProductsApi.NewProduct(name: "Expensive", price: 90m),
            ProductsApi.NewProduct(name: "Middle", price: 50m));

        ProductsPageResponse byPriceDescending = await GetPageAsync("sortField=price&sortOrder=-1");
        ProductsPageResponse byName = await GetPageAsync("sortField=name&sortOrder=1");

        byPriceDescending.Items.Select(product => product.Name).ShouldBe(["Expensive", "Middle", "Cheap"]);
        byName.Items.Select(product => product.Name).ShouldBe(["Cheap", "Expensive", "Middle"]);
    }

    [Fact]
    public async Task Products_with_equal_sort_values_keep_one_order_across_pages()
    {
        string token = await SignInAsync();
        await CreateProductsAsync(token, "Same", "Same", "Same", "Same", "Same", "Same");
        ProductsPageResponse all = await GetPageAsync("sortField=name");

        ProductsPageResponse firstPage = await GetPageAsync("sortField=name&first=0&rows=3");
        ProductsPageResponse secondPage = await GetPageAsync("sortField=name&first=3&rows=3");

        firstPage.Items.Concat(secondPage.Items).Select(product => product.Id)
            .ShouldBe(all.Items.Select(product => product.Id));
    }

    [Fact]
    public async Task Global_filter_matches_name_or_description_ignoring_case_and_counts_only_matches()
    {
        string token = await SignInAsync();
        await Client.CreateProductsAsync(
            token,
            ProductsApi.NewProduct(name: "Old Town Walk", description: "Historic centre"),
            ProductsApi.NewProduct(name: "Night tour", description: "A walk after dark"),
            ProductsApi.NewProduct(name: "Airport to hotel", description: "Door to door"));

        ProductsPageResponse page = await GetPageAsync("globalFilter=WALK&sortField=name&rows=1");

        page.Items.ShouldHaveSingleItem().Name.ShouldBe("Night tour");
        page.TotalRecords.ShouldBe(2);
    }

    [Fact]
    public async Task Global_filter_treats_wildcard_characters_as_plain_text()
    {
        string token = await SignInAsync();
        await CreateProductsAsync(token, "50% off tour", "Old Town Walk");

        ProductsPageResponse page = await GetPageAsync("globalFilter=%25");

        page.Items.ShouldHaveSingleItem().Name.ShouldBe("50% off tour");
    }

    [Fact]
    public async Task Page_of_two_hundred_rows_is_accepted()
    {
        HttpResponseMessage response = await Client.GetProductsAsync("rows=200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("first=-1")]
    [InlineData("rows=0")]
    [InlineData("rows=201")]
    [InlineData("sortOrder=2")]
    [InlineData("sortField=colour")]
    public async Task Invalid_page_request_is_rejected(string query)
    {
        HttpResponseMessage response = await Client.GetProductsAsync(query);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Requested_products_are_returned_in_requested_order()
    {
        string token = await SignInAsync();
        ProductResponse walk = await CreateProductAsync(token, "Old Town Walk");
        await CreateProductAsync(token, "Food Tour");
        ProductResponse transfer = await CreateProductAsync(token, "Airport to hotel");

        HttpResponseMessage response = await Client.GetProductsByIdsAsync(transfer.Id, walk.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadProductsAsync()).Select(product => product.Name).ShouldBe(["Airport to hotel", "Old Town Walk"]);
    }

    [Fact]
    public async Task Same_id_twice_returns_the_product_once()
    {
        string token = await SignInAsync();
        ProductResponse walk = await CreateProductAsync(token, "Old Town Walk");

        HttpResponseMessage response = await Client.GetProductsByIdsAsync(walk.Id, walk.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadProductsAsync()).ShouldHaveSingleItem().Id.ShouldBe(walk.Id);
    }

    [Fact]
    public async Task Unknown_id_returns_404_naming_it()
    {
        string token = await SignInAsync();
        ProductResponse walk = await CreateProductAsync(token, "Old Town Walk");
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await Client.GetProductsByIdsAsync(walk.Id, unknownId);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain(unknownId.ToString());
    }

    [Fact]
    public async Task Lookup_without_ids_is_rejected()
    {
        HttpResponseMessage response = await Client.GetProductsByIdsAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task More_than_one_hundred_ids_are_rejected()
    {
        HttpResponseMessage response = await Client.GetProductsByIdsAsync(
            [.. Enumerable.Range(0, 101).Select(_ => Guid.CreateVersion7())]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<string> SignInAsync() =>
        (await Client.RegisterAndLoginAsync(Email, Password)).AccessToken;

    private async Task<ProductsPageResponse> GetPageAsync(string query = "")
    {
        HttpResponseMessage response = await Client.GetProductsAsync(query);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadProductsPageAsync();
    }

    private async Task CreateProductsAsync(string accessToken, params string[] names) =>
        (await Client.CreateProductsAsync(
            accessToken,
            [.. names.Select(name => ProductsApi.NewProduct(name: name))])).StatusCode.ShouldBe(HttpStatusCode.Created);

    private async Task<ProductResponse> CreateProductAsync(string accessToken, string name) =>
        (await (await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct(name: name)))
            .ReadProductsAsync()).ShouldHaveSingleItem();
}
