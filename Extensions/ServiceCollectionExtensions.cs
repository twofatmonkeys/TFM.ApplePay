using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata.Ecma335;
using TFM.ApplePay.Configuration;
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplePay(this IServiceCollection services)
    {
        var merchantID = Environment.GetEnvironmentVariable("APPLEPAY_MERCHANT_ID");
        if (string.IsNullOrEmpty(merchantID))
            return services; // Don't add Apple Pay services if Merchant ID is not set, as it's required for all operations. This allows the app to run without Apple Pay if desired.
        var displayName = Environment.GetEnvironmentVariable("APPLEPAY_DISPLAY_NAME") ?? throw new Exception($"Environment variable APPLEPAY_DISPLAY_NAME is not set");
        var initiativeContext = Environment.GetEnvironmentVariable("APPLEPAY_INITIATIVE_CONTEXT") ?? throw new Exception($"Environment variable APPLEPAY_INITIATIVE_CONTEXT is not set");
        var merchantIdCertificatePath = Environment.GetEnvironmentVariable("APPLEPAY_MERCHANT_ID_CERTIFICATE_PATH") ?? "/app/config/apple-merchant-identity.p12";
        var merchantIdCertificatePassword = Environment.GetEnvironmentVariable("APPLEPAY_MERCHANT_ID_CERTIFICATE_PASSWORD") ?? throw new Exception($"Environment variable APPLEPAY_MERCHANT_ID_CERTIFICATE_PASSWORD is not set");
        var paymentCertificatePath = Environment.GetEnvironmentVariable("APPLEPAY_PAYMENT_CERTIFICATE_PATH") ?? "/app/config/apple-payment.p12";
        var paymentCertificatePassword = Environment.GetEnvironmentVariable("APPLEPAY_PAYMENT_CERTIFICATE_PASSWORD");
        var countryCode = Environment.GetEnvironmentVariable("APPLEPAY_COUNTRY_CODE") ?? "AU";
        var currencyCode = Environment.GetEnvironmentVariable("APPLEPAY_CURRENCY_CODE") ?? "AUD";
        var supportedNetworks = Environment.GetEnvironmentVariable("APPLEPAY_SUPPORTED_NETWORKS")?.Split(',').Select(s => s.Trim()).ToArray();
        var merchantCapabilities = Environment.GetEnvironmentVariable("APPLEPAY_MERCHANT_CAPABILITIES")?.Split(',').Select(s => s.Trim()).ToArray();
        var merchantValidationUrl = Environment.GetEnvironmentVariable("APPLEPAY_MERCHANT_VALIDATION_URL");
        var decryptTokensLocally = Environment.GetEnvironmentVariable("APPLEPAY_DECRYPT_TOKENS_LOCALLY") ?? (String.IsNullOrEmpty(paymentCertificatePassword) ? "false" : "true");
        var requiredBillingContactFields = Environment.GetEnvironmentVariable("APPLEPAY_REQUIRED_BILLING_CONTACT_FIELDS")?.Split(',').Select(s => s.Trim()).ToArray();
        var requiredShippingContactFields = Environment.GetEnvironmentVariable("APPLEPAY_REQUIRED_SHIPPING_CONTACT_FIELDS")?.Split(',').Select(s => s.Trim()).ToArray();
        return services.AddApplePay(options =>
        {
            options.MerchantId = merchantID;
            options.DisplayName = displayName;
            options.InitiativeContext = initiativeContext;
            options.MerchantIdentityCertificatePath = merchantIdCertificatePath;
            options.MerchantIdentityCertificatePassword = merchantIdCertificatePassword;
            options.PaymentProcessingCertificatePath = paymentCertificatePath;
            options.PaymentProcessingCertificatePassword = paymentCertificatePassword;
            options.CountryCode = countryCode;
            options.CurrencyCode = currencyCode;
            options.DecryptTokensLocally = decryptTokensLocally.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (supportedNetworks != null)
                options.SupportedNetworks = supportedNetworks;
            if(merchantCapabilities != null)
                options.MerchantCapabilities = merchantCapabilities;
            if(merchantValidationUrl != null)
                options.ApplePayMerchantValidationUrl = merchantValidationUrl;
            if(requiredBillingContactFields != null)
                options.RequiredBillingContactFields = requiredBillingContactFields;
            if(requiredShippingContactFields != null)   
                options.RequiredShippingContactFields = requiredShippingContactFields;
        });
    }

    public static IServiceCollection AddApplePay(
        this IServiceCollection services,
        Action<ApplePayOptions> configureOptions)
    {
        services.Configure(configureOptions);
        RegisterServices(services);
        return services;
    }

    public static IServiceCollection AddApplePay(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApplePayOptions>(
            configuration.GetSection(ApplePayOptions.SectionName));
        RegisterServices(services);
        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<ApplePayOptions>, ApplePayOptionsValidator>();
        services.AddOptions<ApplePayOptions>().ValidateOnStart();
        services.AddScoped<IApplePayMerchantValidationService, ApplePayMerchantValidationService>();
        services.AddScoped<IApplePayTokenDecryptionService, ApplePayTokenDecryptionService>();
        services.AddScoped<IApplePayService, ApplePayService>();
    }
}
