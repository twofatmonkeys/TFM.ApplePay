using System.Text.Json;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using TFM.ApplePay.Configuration;

namespace TFM.ApplePay.TagHelpers;

[HtmlTargetElement("apple-pay-button")]
public class ApplePayButtonTagHelper(IOptions<ApplePayOptions> options) : TagHelper
{
    private readonly ApplePayOptions _options = options.Value;

    [HtmlAttributeName("amount")]
    public string Amount { get; set; } = "0.00";

    [HtmlAttributeName("label")]
    public string Label { get; set; } = string.Empty;

    [HtmlAttributeName("line-items")]
    public string? LineItems { get; set; }

    [HtmlAttributeName("button-style")]
    public string ButtonStyle { get; set; } = "black";

    [HtmlAttributeName("button-type")]
    public string ButtonType { get; set; } = "pay";

    [HtmlAttributeName("css-class")]
    public string? CssClass { get; set; }

    [HtmlAttributeName("on-success")]
    public string? OnSuccess { get; set; }

    [HtmlAttributeName("on-error")]
    public string? OnError { get; set; }

    [HtmlAttributeName("api-version")]
    public int ApiVersion { get; set; } = 14;

    [HtmlAttributeName("appData")]
    public string? AppData { get; set; }

    [HtmlAttributeName("supported-networks")]
    public string[]? SupportedNetworks { get; set; }

    [HtmlAttributeName("required-billing-contact-fields")]
    public string[]? RequiredBillingContactFields { get; set; }

    [HtmlAttributeName("required-shipping-contact-fields")]
    public string[]? RequiredShippingContactFields { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if(!_options.IsConfigured)
        {
            output.SuppressOutput();
            return;
        }

        var elementId = $"apple-pay-button-{Guid.NewGuid():N}";

        output.TagName = "div";
        output.Attributes.SetAttribute("id", elementId);
        if (!string.IsNullOrEmpty(CssClass))
            output.Attributes.SetAttribute("class", CssClass);

        var config = new
        {
            elementId,
            apiVersion = ApiVersion,
            merchantId = _options.MerchantId,
            countryCode = _options.CountryCode,
            currencyCode = _options.CurrencyCode,
            supportedNetworks = SupportedNetworks ?? _options.SupportedNetworks,
            merchantCapabilities = _options.MerchantCapabilities,
            requiredBillingContactFields = RequiredBillingContactFields ?? _options.RequiredBillingContactFields,
            requiredShippingContactFields = RequiredShippingContactFields ?? _options.RequiredShippingContactFields,
            total = new
            {
                label = string.IsNullOrEmpty(Label) ? _options.DisplayName : Label,
                type = "final",
                amount = Amount
            },
            lineItems = LineItems,
            buttonStyle = ButtonStyle,
            buttonType = ButtonType,
            applicationData = AppData?.ToBase64(),
            validateMerchantUrl = $"/{_options.RoutePrefix}/validate-merchant",
            processPaymentUrl = $"/{_options.RoutePrefix}/process-payment",
            logMessageUrl = $"/{_options.RoutePrefix}/log-message",
            onSuccess = OnSuccess,
            onError = OnError
        };

        string configJson = JsonSerializer.Serialize(config,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });


        output.Content.SetHtmlContent(
            $"<apple-pay-button buttonstyle=\"{ButtonStyle}\" type=\"{ButtonType}\" locale=\"en\" data-apple-pay='{configJson}'></apple-pay-button>");
    }
}
