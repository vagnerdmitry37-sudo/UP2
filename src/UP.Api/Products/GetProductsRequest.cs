using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using UP.Application.Products;

namespace UP.Api.Products;

public sealed record GetProductsRequest(
    [property: FromQuery(Name = "first")][Range(0, int.MaxValue)] int First = 0,
    [property: FromQuery(Name = "rows")][Range(1, GetProductsRequest.MaxRows)] int Rows = GetProductsRequest.DefaultRows,
    [property: FromQuery(Name = "sortField")][EnumDataType(typeof(ProductSortField))] ProductSortField? SortField = null,
    [property: FromQuery(Name = "sortOrder")][AllowedValues(1, -1)] int SortOrder = 1,
    [property: FromQuery(Name = "globalFilter")][MaxLength(200)] string? GlobalFilter = null)
{
    public const int DefaultRows = 50;
    public const int MaxRows = 200;
}
