using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFM.ApplePay
{
    public static class StringExtensions
    {
        public static string ToBase64(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string base64)
        {
            if (string.IsNullOrEmpty(base64)) return string.Empty;
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
