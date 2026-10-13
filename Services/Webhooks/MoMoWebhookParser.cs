using System.Text.Json;

namespace FashionStore.Services.Webhooks
{
    public class MoMoWebhookParser : IWebhookParser
    {
        public string GatewayName => "MoMo";

        public WebhookPayload Parse(string jsonPayload)
        {
            try
            {
                var json = JsonDocument.Parse(jsonPayload);
                var root = json.RootElement;
                var orderId = root.GetProperty("orderId").GetString() ?? string.Empty;
                var requestId = root.GetProperty("requestId").GetString() ?? string.Empty;
                var resultCode = root.GetProperty("resultCode").GetInt32();
                var transId = root.GetProperty("transId").ToString();

                return new WebhookPayload
                {
                    TransactionId = transId != "0" ? transId : $"{orderId}:{requestId}:{resultCode}",
                    OrderCode = orderId,
                    Amount = root.GetProperty("amount").GetDecimal(),
                    Currency = "VND",
                    Status = MapMoMoStatus(resultCode),
                    ResponseCode = resultCode.ToString(),
                    ResponseMessage = root.TryGetProperty("message", out var msg) ? msg.GetString() : null,
                    RawResponse = jsonPayload
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to parse MoMo webhook payload: {ex.Message}", ex);
            }
        }

        public string PrettyPrint(WebhookPayload payload)
        {
            var formatted = new
            {
                gateway = GatewayName,
                transactionId = payload.TransactionId,
                orderCode = payload.OrderCode,
                amount = payload.Amount,
                currency = payload.Currency,
                status = payload.Status,
                responseCode = payload.ResponseCode,
                responseMessage = payload.ResponseMessage
            };

            return JsonSerializer.Serialize(formatted, new JsonSerializerOptions { WriteIndented = true });
        }

        private string MapMoMoStatus(int resultCode)
        {
            return resultCode switch
            {
                0 => "Paid",           // Success
                1000 => "Pending",      // Waiting for customer confirmation
                9000 => "Processing",  // Transaction in progress
                _ => "Failed"          // All other codes are failures
            };
        }
    }
}
