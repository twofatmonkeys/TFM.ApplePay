# TFM.ApplePay

A full-stack ASP.NET Core library for integrating Apple Pay on any ASP.NET Core website. Provides models, certificate-based merchant validation, server-side token decryption, a Tag Helper for the Apple Pay button, and an abstract payment gateway interface.

## Features

- **Merchant Validation** — Certificate-based (.p12) validation with Apple's servers, with SSRF protection
- **Token Decryption** — Full EC_v1 server-side decryption (ECDH key agreement, NIST KDF, AES-256-GCM)
- **Tag Helper** — Drop-in `<apple-pay-button>` Razor Tag Helper with Apple's JS SDK
- **Gateway Agnostic** — Implement `IApplePayProcessor` to plug in any payment processor (Stripe, Adyen, Worldpay, etc.)
- **DI Integration** — Clean `IServiceCollection` extension methods for registration (lambda, config section, or environment variables)

## Installation

```bash
dotnet add package TFM.ApplePay
```

## Quick Start

### 1. Register services in `Program.cs`

```csharp
using TFM.ApplePay.Extensions;

builder.Services.AddApplePay(options =>
{
    options.MerchantId = "merchant.com.example";
    options.DisplayName = "My Store";
    options.InitiativeContext = "www.example.com"; // your registered domain

    // Merchant Identity Certificate — used for merchant validation with Apple
    options.MerchantIdentityCertificatePath = "/certs/merchant-identity.p12";
    options.MerchantIdentityCertificatePassword = "password";

    // Payment Processing Certificate — used for token decryption
    options.PaymentProcessingCertificatePath = "/certs/payment-processing.p12";
    options.PaymentProcessingCertificatePassword = "password";

    // Payment defaults
    options.CountryCode = "AU";
    options.CurrencyCode = "AUD";
    options.SupportedNetworks = ["visa", "masterCard"];
    options.MerchantCapabilities = ["supports3DS"];
});

// Register your payment gateway implementation
builder.Services.AddScoped<IApplePayProcessor, MyPaymentProcessor>();
```

Or bind from `appsettings.json`:

```csharp
builder.Services.AddApplePay(builder.Configuration);
```

```json
{
  "ApplePay": {
    "MerchantId": "merchant.com.example",
    "DisplayName": "My Store",
    "InitiativeContext": "www.example.com",
    "MerchantIdentityCertificatePath": "/certs/merchant-identity.p12",
    "MerchantIdentityCertificatePassword": "password",
    "PaymentProcessingCertificatePath": "/certs/payment-processing.p12",
    "PaymentProcessingCertificatePassword": "password",
    "CountryCode": "AU",
    "CurrencyCode": "AUD",
    "SupportedNetworks": ["visa", "masterCard"],
    "MerchantCapabilities": ["supports3DS"]
  }
}
```

Or read from environment variables (useful for containerized deployments):

```csharp
builder.Services.AddApplePay(); // reads from environment variables
```

| Environment Variable | Required | Default |
|---|---|---|
| `APPLEPAY_MERCHANT_ID` | Yes (skips registration if unset) | — |
| `APPLEPAY_DISPLAY_NAME` | Yes | — |
| `APPLEPAY_INITIATIVE_CONTEXT` | Yes | — |
| `APPLEPAY_MERCHANT_ID_CERTIFICATE_PATH` | No | `/app/config/apple-merchant-identity.p12` |
| `APPLEPAY_MERCHANT_ID_CERTIFICATE_PASSWORD` | Yes | — |
| `APPLEPAY_PAYMENT_CERTIFICATE_PATH` | No | `/app/config/apple-payment.p12` |
| `APPLEPAY_PAYMENT_CERTIFICATE_PASSWORD` | Yes | — |
| `APPLEPAY_COUNTRY_CODE` | No | `AU` |
| `APPLEPAY_CURRENCY_CODE` | No | `AUD` |
| `APPLEPAY_SUPPORTED_NETWORKS` | No | `visa,masterCard` |
| `APPLEPAY_MERCHANT_CAPABILITIES` | No | `supports3DS` |

### 2. Add the Tag Helper to your Razor view

In your `_ViewImports.cshtml`:

```razor
@addTagHelper *, TFM.ApplePay
```

On your checkout page, add the script tag helper to load the Apple Pay JS SDK and integration script:

```html
<script tfm-apple-pay></script>
```

This renders two script tags: the TFM integration JS (`tfm-apple-pay.js`) and Apple's official SDK (`apple-pay-sdk.js`). If Apple Pay is not configured (`MerchantId` is not set), the tag is suppressed automatically.

Then add the button:

```html
<apple-pay-button
    amount="29.99"
    label="My Store"
    button-style="black"
    button-type="buy"
    on-success="handleApplePaySuccess"
    on-error="handleApplePayError" />

<script>
    function handleApplePaySuccess(result) {
        // result is a PaymentAuthorizationResult with:
        //   result.status — 0 (success) or 1 (failure)
        //   result.orderDetails — optional order details
        console.log('Payment succeeded:', result);
    }

    function handleApplePayError(error) {
        console.error('Payment failed:', error);
    }
</script>
```

### 3. Implement `IApplePayProcessor`

This is the only interface you need to implement. It receives the decrypted card details along with billing/shipping contacts and sends them to your chosen payment processor:

```csharp
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Models.DecryptedToken;
using TFM.ApplePay.Models.PaymentRequest;
using TFM.ApplePay.Models.PaymentToken;
using TFM.ApplePay.Models.Results;

public class MyPaymentProcessor : IApplePayProcessor
{
    public async Task<PaymentAuthorizationResult> ProcessPaymentAsync(
        DecryptedPaymentData decryptedToken,
        ApplePayPaymentRequest originalRequest,
        ApplePayPaymentContact? billingContact,
        ApplePayPaymentContact? shippingContact,
        CancellationToken cancellationToken = default)
    {
        // decryptedToken contains:
        //   .ApplicationPrimaryAccountNumber  — card number (PAN)
        //   .ApplicationExpirationDate        — expiry in YYMMDD format
        //   .PaymentCredential.OnlinePaymentCryptogram — 3DS cryptogram
        //   .PaymentCredential.EciIndicator   — ECI value

        // billingContact / shippingContact contain the customer's
        // name, email, phone, and address if requested via
        // RequiredBillingContactFields / RequiredShippingContactFields

        // Send these to your payment processor...

        return PaymentAuthorizationResult.Success();

        // Or with order details:
        // return PaymentAuthorizationResult.Success(new ApplePayPaymentOrderDetails
        // {
        //     OrderTypeIdentifier = "com.example.order",
        //     OrderIdentifier = "order-123",
        //     WebServiceURL = "https://example.com/orders",
        //     AuthenticationToken = "token-abc"
        // });

        // Or on failure:
        // return PaymentAuthorizationResult.Failure("Payment declined.");
    }
}
```

## Tag Helper Attributes

| Attribute | Default | Description |
|-----------|---------|-------------|
| `amount` | `"0.00"` | Total amount to charge |
| `label` | Options.DisplayName | Label shown on the Apple Pay sheet |
| `line-items` | `null` | JSON array of line items |
| `button-style` | `"black"` | `"black"`, `"white"`, or `"white-outline"` |
| `button-type` | `"pay"` | `"buy"`, `"pay"`, `"plain"`, `"order"`, `"donate"`, etc. |
| `css-class` | `null` | CSS class(es) for the container div |
| `on-success` | `null` | JS callback function name for payment success |
| `on-error` | `null` | JS callback function name for payment failure |
| `api-version` | `14` | Apple Pay JS API version |
| `appData` | `null` | Application-specific data (base64-encoded automatically) |
| `supported-networks` | Options.SupportedNetworks | Override supported networks per button |
| `required-billing-contact-fields` | Options.RequiredBillingContactFields | Override required billing fields per button |
| `required-shipping-contact-fields` | Options.RequiredShippingContactFields | Override required shipping fields per button |

## API Endpoints

The library registers three API endpoints (default prefix: `api/applepay`):

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/applepay/validate-merchant` | Called by the JS SDK during merchant validation |
| POST | `/api/applepay/process-payment` | Receives the encrypted token, decrypts, and processes payment |
| POST | `/api/applepay/log-message` | Logs client-side messages from the JS SDK for server-side diagnostics |

## Certificate Configuration

You can load certificates from file or from the Windows Certificate Store:

**From file (.p12):**
```csharp
options.MerchantIdentityCertificatePath = "/path/to/cert.p12";
options.MerchantIdentityCertificatePassword = "password";
```

**From Certificate Store (by thumbprint):**
```csharp
options.MerchantIdentityCertificateThumbprint = "ABC123...";
```

The same pattern applies for both the Merchant Identity Certificate and the Payment Processing Certificate.

## Architecture

```
Consumer App
    │
    ├── AddApplePay(options)          ← DI registration
    ├── <apple-pay-button />          ← Razor Tag Helper
    │       │
    │       ├── apple-pay-sdk.js      ← Apple's official JS SDK
    │       └── tfm-apple-pay.js      ← Integration glue
    │               │
    │               ▼
    ├── ApplePayController
    │       │
    │       ├── validate-merchant ──► ApplePayMerchantValidationService ──► Apple servers
    │       │
    │       ├── process-payment  ──► ApplePayTokenDecryptionService (ECDH + KDF + AES-GCM)
    │       │                               │
    │       │                               ▼
    │       │                       IApplePayProcessor.ProcessPaymentAsync()
    │       │                               │
    │       │                               ▼
    │       │                       Consumer's gateway implementation
    │       │
    │       └── log-message      ──► ILogger (server-side diagnostics)
    │
    └── PaymentAuthorizationResult ──► Returned to Apple Pay sheet
```

## Requirements

- .NET 8.0 or later
- Apple Developer account with Apple Pay configured
- Merchant Identity Certificate (.p12) — for merchant validation
- Payment Processing Certificate (.p12) — for token decryption
- Domain registered and verified in the Apple Developer portal

## License

MIT License — see [LICENSE.txt](LICENSE.txt)
