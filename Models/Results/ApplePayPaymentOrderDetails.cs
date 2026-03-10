using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFM.ApplePay.Models.Results
{
    public class ApplePayPaymentOrderDetails
    {
        [JsonPropertyName("orderTypeIdentifier")]
        public string OrderTypeIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("orderIdentifier")]
        public string OrderIdentifier { get; set; }  = string.Empty;

        [JsonPropertyName("webServiceURL")]
        public string WebServiceURL { get; set; } = string.Empty;

        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; } = string.Empty;

    }
}
