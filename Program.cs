using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using UnionOrders.Api;
using UnionOrders.Api.Contracts;
using UnionOrders.Api.Domain;
using UnionOrders.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// In .NET 11, AddValidation enables native asynchronous validation for Minimal APIs.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddValidation(options =>
{
    options.LocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
});

builder.Services.AddSingleton<ICustomerDirectory, DemoCustomerDirectory>();
builder.Services.AddSingleton<ICouponCatalog, DemoCouponCatalog>();
builder.Services.AddSingleton<IProductCatalog, DemoProductCatalog>();
builder.Services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US", "nl-NL")
    .AddSupportedUICultures("en-US", "nl-NL");

app.UseRequestLocalization(localizationOptions);

app.MapPost("/orders", async (
    CreateOrderRequest request,
    OrderService service,
    IStringLocalizer<SharedResource> messages,
    CancellationToken cancellationToken) =>
{
    // Invalid requests never reach this handler: .NET 11 validates them first.
    PlaceOrderResult result = await service.PlaceAsync(request, cancellationToken);

    // Deliberately no discard arm: a new union case must be handled explicitly.
    return result switch
    {
        OrderPlaced placed => Results.Created($"/orders/{placed.Id}", placed),
        ProductNotFound missing => Results.NotFound(new ErrorResponse(
            "product_not_found", messages["ProductNotFound", missing.ProductId].Value)),
        OutOfStock stock => Results.Conflict(new ErrorResponse(
            "out_of_stock", messages["OutOfStock", stock.ProductId, stock.AvailableStock].Value)),
        PaymentDeclined => Results.BadRequest(new ErrorResponse(
            "payment_declined", messages["PaymentDeclined"].Value))
    };
});

app.MapGet("/orders/{id:guid}", (Guid id, OrderStore orders) =>
    orders.Find(id) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.Run();
