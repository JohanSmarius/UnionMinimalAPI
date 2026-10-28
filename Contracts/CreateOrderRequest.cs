using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using UnionOrders.Api.Infrastructure;

namespace UnionOrders.Api.Contracts;

public sealed class CreateOrderRequest : IAsyncValidatableObject
{
    [Required(ErrorMessage = "CustomerIdRequired")]
    public string CustomerId { get; init; } = "";

    [Required(ErrorMessage = "ProductIdRequired")]
    public string ProductId { get; init; } = "";

    [Range(1, 10, ErrorMessage = "QuantityOutOfRange")]
    public int Quantity { get; init; }

    [Required(ErrorMessage = "PaymentTokenRequired")]
    public string PaymentToken { get; init; } = "";

    public string? CouponCode { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        throw new InvalidOperationException("Use asynchronous validation.");

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var customers = context.GetRequiredService<ICustomerDirectory>();
        var coupons = context.GetRequiredService<ICouponCatalog>();
        var messages = context.GetRequiredService<IStringLocalizer<SharedResource>>();

        // Independent remote checks execute concurrently.
        Task<bool> customerTask = customers.IsActiveAsync(CustomerId, cancellationToken);
        Task<bool> couponTask = string.IsNullOrWhiteSpace(CouponCode)
            ? Task.FromResult(true)
            : coupons.IsValidAsync(CouponCode, cancellationToken);

        await Task.WhenAll(customerTask, couponTask);

        if (!await customerTask)
        {
            yield return new ValidationResult(
                messages["CustomerNotActive"].Value, [nameof(CustomerId)]);
        }

        if (!await couponTask)
        {
            yield return new ValidationResult(
                messages["CouponInvalid"].Value, [nameof(CouponCode)]);
        }
    }
}
