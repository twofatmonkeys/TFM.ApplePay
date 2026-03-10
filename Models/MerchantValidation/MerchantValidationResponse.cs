namespace TFM.ApplePay.Models.MerchantValidation;

/// <summary>
/// The opaque merchant session object returned by Apple.
/// The raw JSON blob is passed back to the JS SDK via completeMerchantValidation().
/// </summary>
public class MerchantValidationResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MerchantSessionJson { get; set; }
}
