using TFM.ApplePay.Models.DecryptedToken;
using TFM.ApplePay.Models.PaymentToken;

namespace TFM.ApplePay.Interfaces;

public interface IApplePayTokenDecryptionService
{
    DecryptedPaymentData DecryptToken(ApplePayPaymentToken token);
}
