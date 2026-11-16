namespace UP.Api.Orders;

internal static class OrderRoutes
{
    public const string Base = "api/orders";

    public const string ById = "{id:guid}";

    public const string ByIds = "by-ids";

    public const string Items = "{id:guid}/items";

    public const string Customer = "{id:guid}/customer";
}
