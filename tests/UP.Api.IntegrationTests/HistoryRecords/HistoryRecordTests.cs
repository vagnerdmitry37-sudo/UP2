using System.Net;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using UP.Api.Auth;
using UP.Api.HistoryRecords;
using UP.Api.IntegrationTests.Auth;
using UP.Api.IntegrationTests.Products;
using UP.Api.IntegrationTests.TestHost;
using UP.Api.Products;
using UP.Domain.HistoryRecords;

namespace UP.Api.IntegrationTests.HistoryRecords;

public sealed class HistoryRecordTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "seller@example.com";
    private const string Password = "correct-horse-battery";
    private const string ProductEntityType = "Product";

    [Fact]
    public async Task Creating_product_records_created_history_with_user()
    {
        string accessToken = await SignInAsync();
        ProductResponse product = await CreateProductAsync(accessToken);

        List<HistoryRecordResponse> history = await GetProductHistoryAsync(accessToken, product.Id);

        HistoryRecordResponse record = history.ShouldHaveSingleItem();
        record.Operation.ShouldBe(HistoryOperation.Created);
        record.UserId.ShouldBe(await UserIdAsync());
        record.Changes.GetProperty("Name").GetString().ShouldBe("Old Town Walk");
        record.Changes.GetProperty("Type").GetString().ShouldBe("Excursion");
        record.Changes.GetProperty("Attributes").GetProperty("durationMinutes").GetInt32().ShouldBe(180);
        record.Changes.TryGetProperty("Id", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Updating_product_records_only_changed_fields()
    {
        string accessToken = await SignInAsync();
        ProductResponse product = await CreateProductAsync(accessToken);

        await Client.UpdateProductsAsync(accessToken, ProductsApi.NewProduct(id: product.Id, name: "Night Walk"));

        List<HistoryRecordResponse> history = await GetProductHistoryAsync(accessToken, product.Id);
        history.Select(record => record.Operation).ShouldBe([HistoryOperation.Created, HistoryOperation.Updated]);
        JsonElement changes = history[1].Changes;
        changes.EnumerateObject().Select(property => property.Name).ShouldBe(["Name"]);
        changes.GetProperty("Name").GetProperty("old").GetString().ShouldBe("Old Town Walk");
        changes.GetProperty("Name").GetProperty("new").GetString().ShouldBe("Night Walk");
    }

    [Fact]
    public async Task Updating_product_without_changes_records_nothing()
    {
        string accessToken = await SignInAsync();
        ProductResponse product = await CreateProductAsync(accessToken);

        await Client.UpdateProductsAsync(accessToken, ProductsApi.NewProduct(id: product.Id));

        List<HistoryRecordResponse> history = await GetProductHistoryAsync(accessToken, product.Id);
        history.ShouldHaveSingleItem().Operation.ShouldBe(HistoryOperation.Created);
    }

    [Fact]
    public async Task Deleting_product_keeps_history_and_records_deleted()
    {
        string accessToken = await SignInAsync();
        ProductResponse product = await CreateProductAsync(accessToken);

        await Client.DeleteProductsAsync(accessToken, product.Id);

        List<HistoryRecordResponse> history = await GetProductHistoryAsync(accessToken, product.Id);
        history.Select(record => record.Operation).ShouldBe([HistoryOperation.Created, HistoryOperation.Deleted]);
    }

    [Fact]
    public async Task History_requires_sign_in()
    {
        HttpResponseMessage response = await Client.GetHistoryRecordsAsync(
            accessToken: null,
            ProductEntityType,
            Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> SignInAsync()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);
        return token.AccessToken;
    }

    private async Task<ProductResponse> CreateProductAsync(string accessToken) =>
        (await (await Client.CreateProductsAsync(accessToken, ProductsApi.NewProduct())).ReadProductsAsync())[0];

    private async Task<List<HistoryRecordResponse>> GetProductHistoryAsync(string accessToken, Guid productId)
    {
        HttpResponseMessage response = await Client.GetHistoryRecordsAsync(accessToken, ProductEntityType, productId);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadHistoryRecordsAsync();
    }

    private Task<Guid> UserIdAsync() =>
        QueryDatabaseAsync(db => db.Users
            .Where(user => user.Email == Email)
            .Select(user => user.Id)
            .SingleAsync(CancellationToken));
}
