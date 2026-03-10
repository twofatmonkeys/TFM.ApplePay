using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentRequest;

public class ApplePayPaymentRequest
{
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("supportedNetworks")]
    public string[] SupportedNetworks { get; set; } = [];

    [JsonPropertyName("merchantCapabilities")]
    public string[] MerchantCapabilities { get; set; } = [];

    [JsonPropertyName("total")]
    public ApplePayLineItem Total { get; set; } = new();

    [JsonPropertyName("lineItems")]
    public ApplePayLineItem[]? LineItems { get; set; }

    [JsonPropertyName("requiredBillingContactFields")]
    public string[]? RequiredBillingContactFields { get; set; }

    [JsonPropertyName("requiredShippingContactFields")]
    public string[]? RequiredShippingContactFields { get; set; }

    [JsonPropertyName("shippingMethods")]
    public ApplePayShippingMethod[]? ShippingMethods { get; set; }

    [JsonPropertyName("applicationData")]
    public string? ApplicationData { get; set; }

}
