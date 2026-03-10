using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Models.PaymentRequest;
using TFM.ApplePay.Models.PaymentToken;

namespace TFM.ApplePay.Controllers;

[ApiController]
[Route("api/applepay")]
public class ApplePayController : ControllerBase
{
    private readonly IApplePayService _applePayService;
    private readonly ILogger<ApplePayController> _logger;

    public ApplePayController(
        IApplePayService applePayService,
        ILogger<ApplePayController> logger)
    {
        _applePayService = applePayService;
        _logger = logger;
    }

    [HttpPost("validate-merchant")]
    public async Task<IActionResult> ValidateMerchant(
        [FromBody] ValidateMerchantRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ValidationUrl))
            return BadRequest(new { error = "Validation URL is required." });

        var result = await _applePayService.ValidateMerchantAsync(
            request.ValidationUrl, cancellationToken);

        if (result.Success)
            return Content(result.MerchantSessionJson!, "application/json");

        _logger.LogWarning("Merchant validation failed: {Error}", result.ErrorMessage);
        return StatusCode(500, new { error = result.ErrorMessage });
    }

    [HttpPost("process-payment")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Token == null)
            return BadRequest(new { error = "Payment token is required." });
        if (request.PaymentRequest == null)
            return BadRequest(new { error = "Payment request is required." });

        var result = await _applePayService.ProcessPaymentAsync(
            request.Token, request.BillingContact, request.ShippingContact, request.PaymentRequest, cancellationToken);
        return Ok(result);
    }

    [HttpPost("log-message")]
    public IActionResult LogMessage([FromBody] LogMessageRequestDto request)
    {
        _logger.LogInformation("Apple Pay message: {Message}", request.Message);
        return Ok();
    }
}

public class LogMessageRequestDto
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ValidateMerchantRequestDto
{
    [JsonPropertyName("validationUrl")]
    public string ValidationUrl { get; set; } = string.Empty;
}

public class ProcessPaymentRequestDto
{
    [JsonPropertyName("token")]
    public ApplePayPaymentToken? Token { get; set; }

    [JsonPropertyName("billingContact")]
    public ApplePayPaymentContact? BillingContact { get; set; }

    [JsonPropertyName("shippingContact")]
    public ApplePayPaymentContact? ShippingContact { get; set; }

    [JsonPropertyName("paymentRequest")]
    public ApplePayPaymentRequest? PaymentRequest { get; set; }
}
