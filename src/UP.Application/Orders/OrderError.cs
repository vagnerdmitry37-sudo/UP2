namespace UP.Application.Orders;

public enum OrderError
{
    ProductNotFound = 1,
    StartsInPast = 2,
    OrderNotFound = 4,
    ItemNotFound = 5,
    CustomerNotFound = 6,
}
