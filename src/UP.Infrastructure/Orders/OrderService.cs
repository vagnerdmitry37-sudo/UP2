using System.Globalization;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using UP.Application.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Orders;

internal sealed class OrderService(AppDbContext dbContext, TimeProvider timeProvider) : IOrderService
{
    private const string MaxPassengersAttribute = "maxPassengers";

    private const string NextNumberSql =
        "SELECT nextval('\"" + OrderConfiguration.NumberSequenceName + "\"') AS \"Value\"";

    public async Task<OrderResult> CreateAsync(Order order, CancellationToken cancellationToken)
    {
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

    public async Task<OrderResult> CreateItemAsync(Guid orderId, OrderItem item, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return new OrderResult(null, OrderError.OrderNotFound, $"Order {orderId} not found.");
        }

        OrderResult? itemError = await PrepareItemsAsync([item], timeProvider.GetUtcNow(), cancellationToken);
        if (itemError is not null)
        {
            return itemError;
        }

        order.Items.Add(item);
        order.TotalPrice = order.Items.Sum(orderItem => orderItem.TotalPrice);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderResult(order);
    }

    public async Task<Order?> UpdateCustomerAsync(
        Guid id,
        string customerName,
        string customerEmail,
        string customerPhone,
        CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.CustomerName = customerName;
        order.CustomerEmail = customerEmail;
        order.CustomerPhone = customerPhone;

        await dbContext.SaveChangesAsync(cancellationToken);

        return order;
    }

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<OrdersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken)
    {
        int totalRecords = await dbContext.Orders.CountAsync(cancellationToken);
        List<Order> items = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(first)
            .Take(rows)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new OrdersPage(items, totalRecords);
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
            int? maxPassengers = MaxPassengersOf(product);

            if (item.Passengers > maxPassengers)
            {
                return new OrderResult(
                    null,
                    OrderError.TooManyPassengers,
                    $"Product {product.Id} allows at most {maxPassengers} passengers.");
            }

            item.Id = Guid.CreateVersion7();
            item.ProductType = product.Type;
            item.ProductName = product.Name;
            item.ProductAttributes = product.Attributes;
            item.UnitPrice = product.Price;
            item.StartsAt = item.StartsAt.ToUniversalTime();
            item.TotalPrice = OrderItemPricing.TotalPrice(product.Type, product.Price, item.Passengers);
        }

        return null;
    }

    private static int? MaxPassengersOf(Product product)
    {
        using JsonDocument attributes = JsonDocument.Parse(product.Attributes);

        return attributes.RootElement.TryGetProperty(MaxPassengersAttribute, out JsonElement value)
            && value.TryGetInt32(out int maxPassengers)
                ? maxPassengers
                : null;
    }
}
