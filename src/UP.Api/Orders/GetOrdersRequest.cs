using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace UP.Api.Orders;

public sealed record GetOrdersRequest(
    [property: FromQuery(Name = "first")][Range(0, int.MaxValue)] int First = 0,
    [property: FromQuery(Name = "rows")][Range(1, GetOrdersRequest.MaxRows)] int Rows = GetOrdersRequest.DefaultRows)
{
    public const int DefaultRows = 50;
    public const int MaxRows = 200;
}
