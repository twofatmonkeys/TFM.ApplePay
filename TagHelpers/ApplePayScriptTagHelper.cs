using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFM.ApplePay.Configuration;

namespace TFM.ApplePay.TagHelpers
{
    [HtmlTargetElement("script" , Attributes = "tfm-apple-pay")]
    public class ApplePayScriptTagHelper(IOptions<ApplePayOptions> options) : TagHelper
    {
        private readonly ApplePayOptions _options = options.Value;
        private readonly string scriptVersion = "26031008";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!_options.IsConfigured)
            {
                output.SuppressOutput();
                return;
            }
            output.Attributes.RemoveAll("tfm-apple-pay");
            output.Attributes.SetAttribute("src", $"/_content/TFM.ApplePay/js/tfm-apple-pay.js?v={scriptVersion}");
            output.PostElement.AppendHtml("<script src=\"https://applepay.cdn-apple.com/jsapi/1.latest/apple-pay-sdk.js\"></script>");
        }

    }
}
