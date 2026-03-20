using TFM.ApplePay.Models.DecryptedToken;
using TFM.ApplePay.Models.PaymentRequest;
using TFM.ApplePay.Models.PaymentToken;
using TFM.ApplePay.Models.Results;

namespace TFM.ApplePay.Interfaces;

/// <summary>
/// Abstract payment gateway interface that consumers implement for their chosen payment processor.
/// </summary>
public interface IApplePayProcessor
{
    Task<PaymentAuthorizationResult> ProcessPaymentAsync(
        DecryptedPaymentData decryptedToken,
        ApplePayPaymentRequest originalRequest,
        ApplePayPaymentContact? billingContact,
        ApplePayPaymentContact? shippingContact,
        CancellationToken cancellationToken = default);

    Task<PaymentAuthorizationResult> ProcessEncryptedPaymentAsync(
        ApplePayPaymentToken encryptedToken,
        ApplePayPaymentRequest originalRequest,
        ApplePayPaymentContact? billingContact,
        ApplePayPaymentContact? shippingContact,
        CancellationToken cancellationToken = default);
}
