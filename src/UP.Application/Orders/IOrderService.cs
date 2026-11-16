using UP.Domain.Orders;

namespace UP.Application.Orders;

public interface IOrderService
{
    Task<CreateOrderResult> CreateAsync(Order order, CancellationToken cancellationToken);

    Task<OrdersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken);
}
