using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.ReferenceData;

internal static class ReferenceDataLabels
{
    public static string Label(this ProductType type) => type switch
    {
        ProductType.GroupExcursion => "Group excursion",
        ProductType.IndividualExcursion => "Individual excursion",
        ProductType.IndividualTransfer => "Individual transfer",
        ProductType.GroupTransfer => "Group transfer",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static string Label(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
