using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TFM.ApplePay.Configuration;
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Models.MerchantValidation;
using TFM.ApplePay.Models.PaymentRequest;
using TFM.ApplePay.Models.PaymentToken;
using TFM.ApplePay.Models.Results;

namespace TFM.ApplePay.Services;

public class ApplePayService(
        IApplePayMerchantValidationService merchantValidation,
        IApplePayTokenDecryptionService tokenDecryption,
        IApplePayProcessor paymentGateway,
        IOptions<ApplePayOptions> options,
        ILogger<ApplePayService> logger) : IApplePayService
{
    private readonly IApplePayMerchantValidationService _merchantValidation = merchantValidation;
    private readonly IApplePayTokenDecryptionService _tokenDecryption = tokenDecryption;
    private readonly IApplePayProcessor _paymentGateway = paymentGateway;
    private readonly ApplePayOptions _options = options.Value;
    private readonly ILogger<ApplePayService> _logger = logger;

    public async Task<MerchantValidationResponse> ValidateMerchantAsync(string validationUrl, CancellationToken cancellationToken = default)
    {
        return await _merchantValidation.ValidateMerchantAsync(validationUrl, cancellationToken);
    }

    public async Task<PaymentAuthorizationResult> ProcessPaymentAsync(
        ApplePayPaymentToken token,
        ApplePayPaymentContact? billingContact,
        ApplePayPaymentContact? shippingContact,
        ApplePayPaymentRequest paymentRequest,
        CancellationToken cancellationToken = default)
    {
        if (_options.DecryptTokensLocally)
        {
            Models.DecryptedToken.DecryptedPaymentData decryptedData;
            try
            {
                decryptedData = _tokenDecryption.DecryptToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt Apple Pay token");
                return PaymentAuthorizationResult.Failure("Token decryption failed.");
            }

            try
            {
                return await _paymentGateway.ProcessPaymentAsync(
                    decryptedData, paymentRequest, billingContact, shippingContact, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment gateway processing failed");
                return PaymentAuthorizationResult.Failure("Payment processing error.");
            }
        }
        else
        {
            try
            {
                return await _paymentGateway.ProcessEncryptedPaymentAsync(
                    token, paymentRequest, billingContact, shippingContact, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment gateway processing failed");
                return PaymentAuthorizationResult.Failure("Payment processing error.");
            }
        }
        }


    }
