using TFM.ApplePay.Models.MerchantValidation;
using TFM.ApplePay.Models.PaymentRequest;
using TFM.ApplePay.Models.PaymentToken;
using TFM.ApplePay.Models.Results;

namespace TFM.ApplePay.Interfaces;

public interface IApplePayService
{
    Task<MerchantValidationResponse> ValidateMerchantAsync(
        string validationUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentAuthorizationResult> ProcessPaymentAsync(
        ApplePayPaymentToken token,
        ApplePayPaymentContact? billingContact,
        ApplePayPaymentContact? shippingContact,
        ApplePayPaymentRequest paymentRequest,
        CancellationToken cancellationToken = default);


}
