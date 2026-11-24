using System.Globalization;

using Microsoft.EntityFrameworkCore;

using UP.Application.Orders;
using UP.Domain.Customers;
using UP.Domain.Orders;
using UP.Domain.Products;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Orders;

internal sealed class OrderService(AppDbContext dbContext, TimeProvider timeProvider) : IOrderService
{
    private const string NextNumberSql =
        "SELECT nextval('\"" + OrderConfiguration.NumberSequenceName + "\"') AS \"Value\"";

    public async Task<OrderResult> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        Customer? customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Id == order.CustomerId, cancellationToken);
        if (customer is null)
        {
            return new OrderResult(null, OrderError.CustomerNotFound, $"Customer {order.CustomerId} not found.");
        }

        order.Customer = customer;
        DateTimeOffset now = timeProvider.GetUtcNow();

        OrderResult? itemsError = await PrepareItemsAsync(order.Items, now, cancellationToken);
        if (itemsError is not null)
        {
            return itemsError;
        }

        long sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>(NextNumberSql)
            .SingleAsync(cancellationToken);

        order.Id = Guid.CreateVersion7();
        order.Number = string.Create(CultureInfo.InvariantCulture, $"ORD-{now.Year}-{sequenceValue:D6}");
        order.Status = OrderStatus.Pending;
        order.CreatedAt = now;
        order.TotalPrice = order.Items.Sum(item => item.TotalPrice);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderResult(order);
    }

    public async Task<OrderItemResult> CreateItemAsync(
        Guid orderId,
        OrderItem item,
        CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return new OrderItemResult(null, OrderError.OrderNotFound, $"Order {orderId} not found.");
        }

        OrderResult? itemError = await PrepareItemsAsync([item], timeProvider.GetUtcNow(), cancellationToken);
        if (itemError is not null)
        {
            return new OrderItemResult(null, itemError.Error, itemError.ErrorDetail);
        }

        order.Items.Add(item);
        order.TotalPrice = order.Items.Sum(orderItem => orderItem.TotalPrice);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderItemResult(item);
    }

    public async Task<OrderResult> UpdateItemsAsync(
        Guid orderId,
        IReadOnlyList<OrderItem> items,
        CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return new OrderResult(null, OrderError.OrderNotFound, $"Order {orderId} not found.");
        }

        List<Guid> missingIds = [.. items
            .Select(item => item.Id)
            .Where(id => !order.Items.Exists(orderItem => orderItem.Id == id))];
        if (missingIds.Count > 0)
        {
            return new OrderResult(
                null,
                OrderError.ItemNotFound,
                $"Order items not found: {string.Join(", ", missingIds)}");
        }

        List<OrderItem> existingItems = [.. items.Select(item => order.Items.First(orderItem => orderItem.Id == item.Id))];

        OrderResult? itemError = await PrepareItemsAsync([.. items], timeProvider.GetUtcNow(), cancellationToken);
        if (itemError is not null)
        {
            return itemError;
        }

        for (int index = 0; index < items.Count; index++)
        {
            OrderItem item = items[index];
            OrderItem existingItem = existingItems[index];
            existingItem.ProductId = item.ProductId;
            existingItem.ProductType = item.ProductType;
            existingItem.ProductName = item.ProductName;
            existingItem.UnitPrice = item.UnitPrice;
            existingItem.StartsAt = item.StartsAt;
            existingItem.Passengers = item.Passengers;
            existingItem.PickupAddress = item.PickupAddress;
            existingItem.Notes = item.Notes;
            existingItem.TotalPrice = item.TotalPrice;
        }

        order.TotalPrice = order.Items.Sum(orderItem => orderItem.TotalPrice);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderResult(order);
    }

    public async Task<OrderResult> DeleteItemsAsync(
        Guid orderId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return new OrderResult(null, OrderError.OrderNotFound, $"Order {orderId} not found.");
        }

        List<Guid> missingIds = [.. itemIds.Where(id => !order.Items.Exists(item => item.Id == id))];
        if (missingIds.Count > 0)
        {
            return new OrderResult(
                null,
                OrderError.ItemNotFound,
                $"Order items not found: {string.Join(", ", missingIds)}");
        }

        order.Items.RemoveAll(item => itemIds.Contains(item.Id));
        order.TotalPrice = order.Items.Sum(orderItem => orderItem.TotalPrice);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderResult(order);
    }

    public async Task<UpdateOrdersResult> UpdateAsync(
        IReadOnlyList<Order> orders,
        CancellationToken cancellationToken)
    {
        List<Guid> ids = [.. orders.Select(order => order.Id)];
        List<Order> existingOrders = await dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .Where(order => ids.Contains(order.Id))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        List<Guid> missingOrderIds = [.. ids.Except(existingOrders.Select(order => order.Id))];

        if (missingOrderIds.Count > 0)
        {
            return new UpdateOrdersResult([], missingOrderIds, []);
        }

        List<Guid> customerIds = [.. orders.Select(order => order.CustomerId).OfType<Guid>().Distinct()];
        List<Customer> customers = await dbContext.Customers
            .Where(customer => customerIds.Contains(customer.Id))
            .ToListAsync(cancellationToken);
        List<Guid> missingCustomerIds = [.. customerIds.Except(customers.Select(customer => customer.Id))];

        if (missingCustomerIds.Count > 0)
        {
            return new UpdateOrdersResult([], [], missingCustomerIds);
        }

        foreach (Order existingOrder in existingOrders)
        {
            Order order = orders.First(order => order.Id == existingOrder.Id);
            existingOrder.Customer = customers.First(customer => customer.Id == order.CustomerId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateOrdersResult(
            [.. ids.Select(id => existingOrders.First(order => order.Id == id))],
            [],
            []);
    }

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Order> orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .Where(order => ids.Contains(order.Id))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return [.. ids.Select(id => orders.Find(order => order.Id == id)).OfType<Order>()];
    }

    public async Task<OrdersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken)
    {
        int totalRecords = await dbContext.Orders.CountAsync(cancellationToken);
        List<Order> items = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.Customer)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(first)
            .Take(rows)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new OrdersPage(items, totalRecords);
    }

    public async Task<IReadOnlyList<Guid>> DeleteAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Order> existingOrders = await dbContext.Orders
            .Where(order => ids.Contains(order.Id))
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = [.. ids.Except(existingOrders.Select(order => order.Id))];

        if (missingIds.Count > 0)
        {
            return missingIds;
        }

        dbContext.Orders.RemoveRange(existingOrders);
        await dbContext.SaveChangesAsync(cancellationToken);

        return [];
    }

    private async Task<OrderResult?> PrepareItemsAsync(
        List<OrderItem> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        OrderItem? pastItem = items.Find(item => item.StartsAt <= now);
        if (pastItem is not null)
        {
            return new OrderResult(
                null,
                OrderError.StartsInPast,
                $"Start time {pastItem.StartsAt:O} is in the past.");
        }

        List<Guid> productIds = [.. items.Select(item => item.ProductId).Distinct()];
        Dictionary<Guid, Product> products = await dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        List<Guid> missingIds = [.. productIds.Where(id => !products.ContainsKey(id))];
        if (missingIds.Count > 0)
        {
            return new OrderResult(
                null,
                OrderError.ProductNotFound,
                $"Products not found: {string.Join(", ", missingIds)}");
        }

        foreach (OrderItem item in items)
        {
            Product product = products[item.ProductId];
            item.Id = Guid.CreateVersion7();
            item.ProductType = product.Type;
            item.ProductName = product.Name;
            item.UnitPrice = product.Price;
            item.StartsAt = item.StartsAt.ToUniversalTime();
            item.TotalPrice = OrderItemPricing.TotalPrice(product.Type, product.Price, item.Passengers);
        }

        return null;
    }
}
