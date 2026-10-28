namespace UnionOrders.Api.Domain;

public sealed record OrderPlaced(Guid Id, string ProductId, int Quantity, decimal Total);
public sealed record ProductNotFound(string ProductId);
public sealed record OutOfStock(string ProductId, int AvailableStock);
public sealed record PaymentDeclined;

// C# 15: this is a closed set of results, with exhaustive pattern matching.
public union PlaceOrderResult(OrderPlaced, ProductNotFound, OutOfStock, PaymentDeclined);
