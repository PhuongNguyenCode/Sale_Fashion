using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace FashionStore.Services.Webhooks
{
    public class VNPayWebhookParser : IWebhookParser
    {
        public string GatewayName => "VNPay";

        public WebhookPayload Parse(string jsonPayload)
        {
            var queryParams = jsonPayload.Split('&')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => WebUtility.UrlDecode(p[1]));

            var vnp_ResponseCode = queryParams.GetValueOrDefault("vnp_ResponseCode");
            var status = vnp_ResponseCode switch
            {
                "00" => "Paid",
                "24" => "Cancelled",
                _ => "Failed"
            };

            decimal.TryParse(queryParams.GetValueOrDefault("vnp_Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount);
            amount = amount / 100m;

            return new WebhookPayload
            {
                TransactionId = queryParams.GetValueOrDefault("vnp_TransactionNo") ?? string.Empty,
                OrderCode = queryParams.GetValueOrDefault("vnp_TxnRef") ?? string.Empty,
                Amount = amount,
                Currency = "VND",
                Status = status,
                ResponseCode = vnp_ResponseCode,
                ResponseMessage = queryParams.GetValueOrDefault("vnp_OrderInfo"),
                RawResponse = jsonPayload
            };
        }

        public string PrettyPrint(WebhookPayload payload)
        {
            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
