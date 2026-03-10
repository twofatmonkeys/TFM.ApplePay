using TFM.ApplePay.Models.MerchantValidation;

namespace TFM.ApplePay.Interfaces;

public interface IApplePayMerchantValidationService
{
    Task<MerchantValidationResponse> ValidateMerchantAsync(
        string validationUrl,
        CancellationToken cancellationToken = default);
}
