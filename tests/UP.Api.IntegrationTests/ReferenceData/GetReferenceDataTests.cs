using System.Net;

using UP.Api.IntegrationTests.TestHost;
using UP.Api.ReferenceData;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.IntegrationTests.ReferenceData;

public sealed class GetReferenceDataTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Every_product_type_and_order_status_has_a_label_without_sign_in()
    {
        HttpResponseMessage response = await Client.GetReferenceDataAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ReferenceDataResponse referenceData = await response.ReadReferenceDataAsync();
        referenceData.ProductTypes.Select(option => option.Value).ShouldBe(Enum.GetValues<ProductType>());
        referenceData.OrderStatuses.Select(option => option.Value).ShouldBe(Enum.GetValues<OrderStatus>());
        referenceData.ProductTypes.ShouldAllBe(option => option.Label.Length > 0);
        referenceData.OrderStatuses.ShouldAllBe(option => option.Label.Length > 0);
    }

    [Fact]
    public async Task Product_type_label_is_readable_text_not_the_enum_name()
    {
        ReferenceDataResponse referenceData = await (await Client.GetReferenceDataAsync()).ReadReferenceDataAsync();

        referenceData.ProductTypes.ShouldContain(new ProductTypeOption(ProductType.GroupExcursion, "Group excursion"));
    }
}
