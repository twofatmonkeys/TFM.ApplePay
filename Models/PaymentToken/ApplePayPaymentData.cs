using System.Text.Json;
using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentToken;

/// <summary>
/// The encrypted payment data from Apple Pay.
/// </summary>
public class ApplePayPaymentData
{
    [JsonPropertyName("header")]
    public ApplePayPaymentTokenHeader Header { get; set; } = new();

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}
