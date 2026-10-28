using System.Collections.Concurrent;
using UnionOrders.Api.Domain;

namespace UnionOrders.Api.Infrastructure;

public sealed record Product(string Id, int Stock, decimal UnitPrice);

public interface ICustomerDirectory
{
    Task<bool> IsActiveAsync(string customerId, CancellationToken cancellationToken);
}

public interface ICouponCatalog
{
    Task<bool> IsValidAsync(string couponCode, CancellationToken cancellationToken);
}

public interface IProductCatalog
{
    Task<Product?> FindAsync(string productId, CancellationToken cancellationToken);
}

public interface IPaymentGateway
{
    Task<bool> AuthorizeAsync(string paymentToken, decimal amount, CancellationToken cancellationToken);
}

public sealed class DemoCustomerDirectory : ICustomerDirectory
{
    public async Task<bool> IsActiveAsync(string customerId, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate a remote customer lookup.
        return customerId is "customer-1" or "customer-2";
    }
}

public sealed class DemoCouponCatalog : ICouponCatalog
{
    public async Task<bool> IsValidAsync(string couponCode, CancellationToken cancellationToken)
    {
        await Task.Delay(120, cancellationToken); // Simulate a remote coupon lookup.
        return string.Equals(couponCode, "SAVE10", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DemoProductCatalog : IProductCatalog
{
    private static readonly Dictionary<string, Product> Products =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["keyboard"] = new("keyboard", Stock: 3, UnitPrice: 79.95m),
            ["mouse"] = new("mouse", Stock: 10, UnitPrice: 29.95m),
            ["monitor"] = new("monitor", Stock: 0, UnitPrice: 199.95m)
        };

    public async Task<Product?> FindAsync(string productId, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        return Products.GetValueOrDefault(productId);
    }
}

public sealed class DemoPaymentGateway : IPaymentGateway
{
    public async Task<bool> AuthorizeAsync(
        string paymentToken, decimal amount, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate a remote payment gateway.
        return !string.Equals(paymentToken, "decline", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, OrderPlaced> _orders = new();

    public void Save(OrderPlaced order) => _orders[order.Id] = order;

    public OrderPlaced? Find(Guid id) => _orders.GetValueOrDefault(id);
}
