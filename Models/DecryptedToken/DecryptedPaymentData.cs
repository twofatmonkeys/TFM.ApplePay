using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.DecryptedToken;

/// <summary>
/// The decrypted contents of an Apple Pay payment token.
/// </summary>
public class DecryptedPaymentData
{
    [JsonPropertyName("applicationPrimaryAccountNumber")]
    public string ApplicationPrimaryAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("applicationExpirationDate")]
    public string ApplicationExpirationDate { get; set; } = string.Empty;

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("transactionAmount")]
    public long TransactionAmount { get; set; }

    [JsonPropertyName("deviceManufacturerIdentifier")]
    public string DeviceManufacturerIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("paymentDataType")]
    public string PaymentDataType { get; set; } = string.Empty;

    [JsonPropertyName("paymentData")]
    public DecryptedPaymentCredential PaymentCredential { get; set; } = new();
}
