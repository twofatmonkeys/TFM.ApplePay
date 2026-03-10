using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.DecryptedToken;

public class DecryptedPaymentCredential
{
    [JsonPropertyName("onlinePaymentCryptogram")]
    public string OnlinePaymentCryptogram { get; set; } = string.Empty;

    [JsonPropertyName("eciIndicator")]
    public string? EciIndicator { get; set; }
}
