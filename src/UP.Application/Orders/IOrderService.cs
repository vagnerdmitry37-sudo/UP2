using UP.Domain.Orders;

namespace UP.Application.Orders;

public interface IOrderService
{
    Task<OrderResult> CreateAsync(Order order, CancellationToken cancellationToken);

    Task<OrderResult> CreateItemAsync(Guid orderId, OrderItem item, CancellationToken cancellationToken);

    Task<OrderResult> UpdateItemAsync(
        Guid orderId,
        Guid itemId,
        OrderItem item,
        CancellationToken cancellationToken);

    Task<OrderResult> DeleteItemsAsync(
        Guid orderId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task<Order?> UpdateCustomerAsync(
        Guid id,
        string customerName,
        string customerEmail,
        string customerPhone,
        CancellationToken cancellationToken);

    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);

    Task<OrdersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
