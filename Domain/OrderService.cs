using UnionOrders.Api.Contracts;
using UnionOrders.Api.Infrastructure;

namespace UnionOrders.Api.Domain;

public sealed class OrderService(
    IProductCatalog products,
    IPaymentGateway payments,
    OrderStore orders)
{
    public async Task<PlaceOrderResult> PlaceAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        Product? product = await products.FindAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return new ProductNotFound(request.ProductId);
        }

        if (product.Stock < request.Quantity)
        {
            return new OutOfStock(product.Id, product.Stock);
        }

        decimal total = product.UnitPrice * request.Quantity;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            total *= 0.90m; // The request validator already checked the coupon.
        }

        bool approved = await payments.AuthorizeAsync(
            request.PaymentToken, total, cancellationToken);
        if (!approved)
        {
            return new PaymentDeclined();
        }

        var placed = new OrderPlaced(Guid.NewGuid(), product.Id, request.Quantity, total);
        orders.Save(placed);
        return placed;
    }
}
