global using System.Text.Json.Serialization;

namespace TFM.ApplePay.Configuration;


public class ApplePayOptions
{
    public const string SectionName = "ApplePay";

    // Merchant identity
    public string MerchantId { get => _MerchantId; set {_MerchantId = value; IsConfigured = !string.IsNullOrEmpty(_MerchantId); } }
    private string _MerchantId = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Initiative { get; set; } = "web";
    public string InitiativeContext { get; set; } = string.Empty;

    // Merchant Identity Certificate (.p12) — used for merchant validation with Apple
    public string MerchantIdentityCertificatePath { get; set; } = string.Empty;
    public string MerchantIdentityCertificatePassword { get; set; } = string.Empty;
    public string? MerchantIdentityCertificateThumbprint { get; set; }

    // Payment Processing Certificate (.p12) — used for token decryption
    public string PaymentProcessingCertificatePath { get; set; } = string.Empty;
    public string PaymentProcessingCertificatePassword { get; set; } = string.Empty;
    public string? PaymentProcessingCertificateThumbprint { get; set; }

    // Payment request defaults
    public string CountryCode { get; set; } = "AU";
    public string CurrencyCode { get; set; } = "AUD";
    public string[] SupportedNetworks { get; set; } = ["visa", "masterCard"];
    public string[] MerchantCapabilities { get; set; } = ["supports3DS"];
    public string[] RequiredBillingContactFields { get; set; } = ["postalAddress"];
    public string[] RequiredShippingContactFields { get; set; } = ["name", "email", "phone"];

    // Endpoint configuration
    public string RoutePrefix { get; set; } = "api/applepay";

    // Apple's merchant validation URL
    public string ApplePayMerchantValidationUrl { get; set; }
        = "https://apple-pay-gateway.apple.com/paymentservices/paymentSession";

    // Apple Pay js sdk url
    public string ApplePayJsSdkUrl { get; set; } = "https://applepay.cdn-apple.com/jsapi/v1/apple-pay-sdk.js";

    public bool IsConfigured { get; set; } = false;
}
