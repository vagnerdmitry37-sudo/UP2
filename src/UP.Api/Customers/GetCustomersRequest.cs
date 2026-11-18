using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace UP.Api.Customers;

public sealed record GetCustomersRequest(
    [property: FromQuery(Name = "first")][Range(0, int.MaxValue)] int First = 0,
    [property: FromQuery(Name = "rows")][Range(1, GetCustomersRequest.MaxRows)] int Rows = GetCustomersRequest.DefaultRows,
    [property: FromQuery(Name = "globalFilter")][MaxLength(200)] string? GlobalFilter = null)
{
    public const int DefaultRows = 50;
    public const int MaxRows = 200;
}
