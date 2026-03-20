global using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TFM.ApplePay.Configuration;


public class ApplePayOptions
{
    public const string SectionName = "ApplePay";

    // Merchant identity
    public string MerchantId { get => _MerchantId; set { _MerchantId = value; IsConfigured = !string.IsNullOrEmpty(_MerchantId); } }
    private string _MerchantId = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Initiative { get; set; } = "web";
    public string InitiativeContext { get; set; } = string.Empty;

    // Select where to handle token decryption
    public bool DecryptTokensLocally { get; set; } = true;

    // Merchant Identity Certificate (.p12) — used for merchant validation with Apple
    public string MerchantIdentityCertificatePath { get; set; } = string.Empty;
    public string MerchantIdentityCertificatePassword { get; set; } = string.Empty;
    public string? MerchantIdentityCertificateThumbprint { get; set; }

    // Payment Processing Certificate (.p12) — used for token decryption
    public string PaymentProcessingCertificatePath { get; set; } = string.Empty;
    public string? PaymentProcessingCertificatePassword { get; set; }
    public string? PaymentProcessingCertificateThumbprint { get; set; }

    // Payment request defaults, set to system locale for convenience, but can be overridden as needed
    public string CountryCode { get; set; } = GetSystemLocaleCountryCode();
    public string CurrencyCode { get; set; } = GetSystemLocaleCurrencyCode();
    public string[] SupportedNetworks { get; set; } = ["visa", "masterCard"];
    public string[] MerchantCapabilities { get; set; } = ["supports3DS"];
    public string[] RequiredBillingContactFields { get; set; } = [];
    public string[] RequiredShippingContactFields { get; set; } = [];

    // Endpoint configuration
    public string RoutePrefix { get; set; } = "api/applepay";

    // Apple's merchant validation URL
    public string ApplePayMerchantValidationUrl { get; set; }
        = "https://apple-pay-gateway.apple.com/paymentservices/paymentSession";

    // Apple Pay js sdk url
    public string ApplePayJsSdkUrl { get; set; } = "https://applepay.cdn-apple.com/jsapi/v1/apple-pay-sdk.js";

    public bool IsConfigured { get; set; } = false;

    public void Validate()
    {
        if (IsConfigured)
        {
            if (string.IsNullOrEmpty(DisplayName))
                throw new InvalidOperationException("Apple Pay DisplayName is not configured.");
            if (string.IsNullOrEmpty(InitiativeContext))
                throw new InvalidOperationException("Apple Pay InitiativeContext is not configured.");
            if (string.IsNullOrEmpty(MerchantIdentityCertificatePath) || string.IsNullOrEmpty(MerchantIdentityCertificatePassword))
                throw new InvalidOperationException("Apple Pay Merchant Identity Certificate path and password must be configured.");
            if (DecryptTokensLocally && (string.IsNullOrEmpty(PaymentProcessingCertificatePath) || string.IsNullOrEmpty(PaymentProcessingCertificatePassword)))
                throw new InvalidOperationException("Apple Pay Payment Processing Certificate path and password must be configured when DecryptTokensLocally is true.");
        }
    }

    private static string GetSystemLocaleCountryCode()
    {
        try
        {
            var cc = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
            Console.WriteLine("ApplePayOptions: Detected system locale country code: " + cc);
            return cc;
        }
        catch
        {
            Console.WriteLine("ApplePayOptions: Failed to detect system locale country code. Falling back to US.");
            return "US"; // Fallback to US if we can't determine the system locale
        }
    }
     private static string GetSystemLocaleCurrencyCode()
    {
        try
        {
            var cc =  new System.Globalization.RegionInfo(System.Globalization.CultureInfo.CurrentCulture.LCID).ISOCurrencySymbol;
            Console.WriteLine("ApplePayOptions: Detected system locale currency code: " + cc);
            return cc;
        }
        catch
        {
            Console.WriteLine("ApplePayOptions: Failed to detect system locale currency code. Falling back to USD.");
            return "USD"; // Fallback to USD if we can't determine the system locale
        }
    }   
}

public class ApplePayOptionsValidator : IValidateOptions<ApplePayOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplePayOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
