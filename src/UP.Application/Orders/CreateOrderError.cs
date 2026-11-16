namespace UP.Application.Orders;

public enum CreateOrderError
{
    ProductNotFound = 1,
    StartsInPast = 2,
    TooManyPassengers = 3,
}
