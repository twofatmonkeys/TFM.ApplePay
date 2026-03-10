using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentRequest;

public class ApplePayShippingMethod
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
}
