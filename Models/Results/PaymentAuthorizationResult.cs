using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFM.ApplePay.Models.Errors;

namespace TFM.ApplePay.Models.Results
{
    public class PaymentAuthorizationResult
    {
        [JsonPropertyName("status")]
        public StatusCode Status { get; set; } = StatusCode.STATUS_SUCCESS;

        [JsonPropertyName("errors")]
        public List<ApplePayError>? Errors { get; set; }

        [JsonPropertyName("orderDetails")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ApplePayPaymentOrderDetails? OrderDetails { get; set; }


        public enum StatusCode
        { 
            STATUS_SUCCESS = 0,
            STATUS_FAILURE = 1,
        }

        public static PaymentAuthorizationResult Success()
            => new() { Status = StatusCode.STATUS_SUCCESS };
        
        public static PaymentAuthorizationResult Success(ApplePayPaymentOrderDetails orderDetails)
            => new() { Status = StatusCode.STATUS_SUCCESS, OrderDetails = orderDetails };

        public static PaymentAuthorizationResult Failure(string errorMessage)
            => new() { Status = StatusCode.STATUS_FAILURE, Errors = [new ApplePayError(errorMessage)] };

        public static PaymentAuthorizationResult Failure(ApplePayError error)
            => new() { Status = StatusCode.STATUS_FAILURE, Errors = [error] };

        public static PaymentAuthorizationResult Failure(List<ApplePayError> errors)
            => new() { Status = StatusCode.STATUS_FAILURE, Errors = errors };

        

    }

}
