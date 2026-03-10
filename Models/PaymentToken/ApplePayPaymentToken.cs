using System.Text.Json.Serialization;

namespace TFM.ApplePay.Models.PaymentToken;

/// <summary>
/// Represents the encrypted payment token received from Apple Pay JS.
/// Corresponds to ApplePayPayment.token from the JS API.
/// </summary>
public class ApplePayPaymentToken
{
    [JsonPropertyName("paymentData")]
    public ApplePayPaymentData PaymentData { get; set; } = new();

    [JsonPropertyName("paymentMethod")]
    public ApplePayPaymentMethod PaymentMethod { get; set; } = new();

    [JsonPropertyName("transactionIdentifier")]
    public string TransactionIdentifier { get; set; } = string.Empty;
}

public class ApplePayPaymentMethod
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("paymentPass")]
    public ApplePayPaymentPass? PaymentPass { get; set; }

    [JsonPropertyName("billingContact")]
    public ApplePayPaymentContact? BillingContact { get; set; }
}

public class ApplePayPaymentContact
{
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
    
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
    
    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }
    
    [JsonPropertyName("familyName")]
    public string? FamilyName { get; set; }
    
    [JsonPropertyName("phoneticGivenName")]
    public string? PhoneticGivenName { get; set; }
    
    [JsonPropertyName("phoneticFamilyName")]
    public string? PhoneticFamilyName { get; set; }
    
    [JsonPropertyName("addressLines")]
    public string[]? AddressLines { get; set; }
    
    [JsonPropertyName("subLocality")]
    public string? SubLocality { get; set; }
    
    [JsonPropertyName("locality")]
    public string? Locality { get; set; }
    
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
    
    [JsonPropertyName("administrativeArea")]
    public string? AdministrativeArea { get; set; }
    
    [JsonPropertyName("subAdministrativeArea")]
    public string? SubAdministrativeArea { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }
}


public class ApplePayPaymentPass
{
    [JsonPropertyName("primaryAccountIdentifier")]
    public string PrimaryAccountIdentifier { get; set; } = string.Empty;
    [JsonPropertyName("primaryAccountNumberSuffix")]
    public string PrimaryAccountNumberSuffix { get; set; } = string.Empty;
    [JsonPropertyName("deviceAccountIdentifier")]
    public string DeviceAccountIdentifier { get; set; } = string.Empty;
    [JsonPropertyName("deviceAccountNumberSuffix")]
    public string DeviceAccountNumberSuffix { get; set; } = string.Empty;
    [JsonPropertyName("activationState")]
    public ApplePayPassActivationState ActivationState { get; set; }

}

public enum ApplePayPassActivationState
{
    activated,
    requiresActivation,
    activating,
    suspended,
    deactivated
}
