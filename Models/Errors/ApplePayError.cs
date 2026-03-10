

namespace TFM.ApplePay.Models.Errors
{
    public class ApplePayError
    {
        [JsonPropertyName("code")]
        public ApplePayErrorCode Code { get; set; } = ApplePayErrorCode.unknown;

        [JsonPropertyName("contactField")]
        public ApplePayErrorContactField? ContactField { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        public ApplePayError(string message)
        {
            Code = ApplePayErrorCode.unknown;
            Message = message;
        }

        public ApplePayError(ApplePayErrorCode code, ApplePayErrorContactField? contactField, string message)
        {
            Code = code;
            ContactField = contactField;
            Message = message;
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplePayErrorCode
    {
        shippingContactInvalid,
        billingContactInvalid,
        addressUnserviceable,
        couponCodeInvalid,
        couponCodeExpired,
        unknown
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplePayErrorContactField
    { 
        phoneNumber,
        emailAddress,
        name,
        phoneticName,
        postalAddress,
        addressLines,
        locality,
        subLocality,
        postalCode,
        administrativeArea,
        subAdministrativeArea,
        country,
        countryCode

    }

}
