using UP.Domain.Orders;

namespace UP.Application.Orders;

public interface IOrderService
{
    Task<OrderResult> CreateAsync(Order order, CancellationToken cancellationToken);

    Task<UpdateOrdersResult> UpdateAsync(IReadOnlyList<Order> orders, CancellationToken cancellationToken);

    Task<OrderItemResult> CreateItemAsync(Guid orderId, OrderItem item, CancellationToken cancellationToken);

    Task<OrderResult> UpdateItemsAsync(
        Guid orderId,
        IReadOnlyList<OrderItem> items,
        CancellationToken cancellationToken);

    Task<OrderResult> DeleteItemsAsync(
        Guid orderId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task<OrderItemsPage?> GetItemsAsync(
        Guid orderId,
        int first,
        int rows,
        CancellationToken cancellationToken);

    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);

    Task<OrdersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
