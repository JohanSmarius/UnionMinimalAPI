# UnionOrders — .NET 11 Minimal API demo

A conference-friendly example of **C# 15 union types**, **native asynchronous Minimal API validation**, and **localized API error messages**.

## Requirements

- .NET 11 SDK (RC 2 or newer). The example pins `Microsoft.Extensions.Validation` to `11.0.0-rc.2.26504.105` for reproducibility with the October 2026 prerelease SDK.
- Any editor capable of running .NET projects. `Orders.http` works with the Visual Studio HTTP editor or REST Client extensions.

## Run

```sh
dotnet restore
dotnet run --urls http://localhost:5055
```

Open `Orders.http` and send requests individually, or use `curl`:

```sh
curl -i -X POST 'http://localhost:5055/orders?culture=nl-NL' \
  -H 'Content-Type: application/json' \
  -d '{"customerId":"unknown-customer","productId":"keyboard","quantity":2,"paymentToken":"approve"}'
```

## Design

1. ASP.NET Core validates `CreateOrderRequest` *before* the endpoint runs. `[Required]` and `[Range]` are synchronous rules. `IAsyncValidatableObject` checks a customer directory and coupon catalog asynchronously. These independent checks run concurrently via `Task.WhenAll`.
2. On invalid input, the framework returns HTTP 400 with validation details. The handler and domain service never run.
3. On valid input, `OrderService.PlaceAsync` returns exactly one of four C# 15 union cases: `OrderPlaced`, `ProductNotFound`, `OutOfStock`, or `PaymentDeclined`.
4. The Minimal API maps those cases to HTTP 201, 404, 409, and 400 respectively, with an exhaustive `switch` and no catch-all `_` branch.
5. The HTTP layer localizes user-facing messages; the domain models contain no HTTP or localization dependencies.

## Localization options

| How | Example | Priority |
| --- | --- | --- |
| Query string | `POST /orders?culture=nl-NL` | First |
| Cookie | `.AspNetCore.Culture=c=nl-NL\|uic=nl-NL` | Second |
| Accept-Language header | `Accept-Language: nl-NL` | Third |
| Default | `en-US` | Fallback |

Translation files are in `Resources/SharedResource.en-US.resx` and `Resources/SharedResource.nl-NL.resx`. The marker `SharedResource.cs` intentionally belongs to the root project namespace; moving it under `Resources` without changing its namespace or conventions can break lookup.

The validator uses two mechanisms:
- Attribute messages (`[Required]`, `[Range]`) use keys resolved by `AddValidation` and its `LocalizerProvider`.
- Messages returned by `IAsyncValidatableObject` use `IStringLocalizer<SharedResource>` explicitly.

## Live-demo exercise: add a new case

1. Add `public sealed record ShippingBlocked(string Reason);` to `Domain/PlaceOrderResult.cs`.
2. Add `ShippingBlocked` to the union declaration.
3. Rebuild and inspect the exhaustive switch in `Program.cs`. The compiler can now report an unhandled union case.
4. Add an explicit `ShippingBlocked` switch arm (for example HTTP 422).

## Limitations

This is demonstration code, **not a production order-processing system**:
- The directory, coupons, product catalog, and payment gateway are simulated with `Task.Delay`; they do not call real APIs.
- Inventory is not reserved or decremented. A production service needs a transaction/reservation strategy and concurrency control.
- `"approve"` and `"decline"` are fake payment tokens. No payment information should be sent to this sample.
- Orders are stored in memory and disappear when the process restarts.
- Add authentication/authorization, idempotency, persistence, logging, and tests before adapting it for real transactions.

> Note: The code was authored against the .NET 11 RC 2 documentation. It could not be compiled in the authoring environment because no .NET SDK was installed there. Rebuild with the installed SDK and adjust for any subsequent prerelease API changes.

## Official references

- [C# union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)
- [.NET 11: asynchronous validation for Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11)
- [ASP.NET Core validation and localization](https://learn.microsoft.com/en-us/aspnet/core/validation/overview)
- [Localization and resource lookup](https://learn.microsoft.com/en-us/dotnet/core/extensions/localization)
