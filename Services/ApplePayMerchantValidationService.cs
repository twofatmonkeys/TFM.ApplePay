using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using TFM.ApplePay.Configuration;
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Models.MerchantValidation;

namespace TFM.ApplePay.Services;

public class ApplePayMerchantValidationService(IOptions<ApplePayOptions> options, ILogger<ApplePayMerchantValidationService> logger) : IApplePayMerchantValidationService
{
    private readonly ApplePayOptions _options = options.Value;
    private readonly ILogger<ApplePayMerchantValidationService> _logger = logger;

    public async Task<MerchantValidationResponse> ValidateMerchantAsync(string validationUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateAppleDomain(validationUrl))
            {
                _logger.LogWarning("Merchant validation URL {Url} is not an Apple domain", validationUrl);
                return new MerchantValidationResponse
                {
                    Success = false,
                    ErrorMessage = "Validation URL is not an Apple domain."
                };
            }

            using var cert = LoadMerchantIdentityCertificate();
            using var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);

            using var client = new HttpClient(handler);

            var requestBody = new MerchantValidationRequest
            {
                MerchantIdentifier = _options.MerchantId,
                DisplayName = _options.DisplayName,
                Initiative = _options.Initiative,
                InitiativeContext = _options.InitiativeContext
            };

            var response = await client.PostAsJsonAsync(validationUrl, requestBody, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Apple merchant validation failed with status {Status}: {Body}",
                    response.StatusCode, responseContent);
                return new MerchantValidationResponse
                {
                    Success = false,
                    ErrorMessage = $"Apple returned {response.StatusCode}: {responseContent}"
                };
            }

            return new MerchantValidationResponse
            {
                Success = true,
                MerchantSessionJson = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merchant validation failed");
            return new MerchantValidationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates the merchant validation URL is an Apple-owned HTTPS endpoint.
    /// Prevents SSRF by ensuring the URL host is exactly apple.com or a subdomain of it.
    /// </summary>
    private static bool ValidateAppleDomain(string validationUrl)
    {
        if (!Uri.TryCreate(validationUrl, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var host = uri.Host;
        return host.Equals("apple.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".apple.com", StringComparison.OrdinalIgnoreCase);
    }

    private X509Certificate2 LoadMerchantIdentityCertificate()
    {
        if (!string.IsNullOrEmpty(_options.MerchantIdentityCertificateThumbprint))
            return LoadCertificateFromStore(_options.MerchantIdentityCertificateThumbprint);

        var certPath = _options.MerchantIdentityCertificatePath;

        if (string.IsNullOrEmpty(certPath) || !File.Exists(certPath))
            throw new FileNotFoundException("Merchant identity certificate file not found.", certPath);

        var flags = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet
            : X509KeyStorageFlags.EphemeralKeySet;

        return new X509Certificate2(certPath, _options.MerchantIdentityCertificatePassword, flags);
    }

    private static X509Certificate2 LoadCertificateFromStore(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (certs.Count == 0)
            throw new InvalidOperationException(
                $"Certificate with thumbprint '{thumbprint}' not found in LocalMachine/My store.");

        return certs[0];
    }

}
