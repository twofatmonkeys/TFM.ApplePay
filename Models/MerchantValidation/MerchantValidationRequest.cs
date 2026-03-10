using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.MerchantValidation;

public class MerchantValidationRequest
{
    [JsonPropertyName("merchantIdentifier")]
    public string MerchantIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("initiative")]
    public string Initiative { get; set; } = "web";

    [JsonPropertyName("initiativeContext")]
    public string InitiativeContext { get; set; } = string.Empty;
}
