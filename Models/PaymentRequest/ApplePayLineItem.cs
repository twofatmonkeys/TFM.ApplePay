using System.Text.Json;
using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentRequest;

public class ApplePayLineItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "final";
}
