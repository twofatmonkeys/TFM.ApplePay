using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentToken;

public class ApplePayPaymentTokenHeader
{
    [JsonPropertyName("ephemeralPublicKey")]
    public string? EphemeralPublicKey { get; set; }

    [JsonPropertyName("publicKeyHash")]
    public string PublicKeyHash { get; set; } = string.Empty;

    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("applicationData")]
    public string? ApplicationData { get; set; }

    [JsonPropertyName("wrappedKey")]
    public string? WrappedKey { get; set; }
}
