using System.Text.Json;

namespace FashionStore.Services.Webhooks
{
    /// <summary>
    /// Parser cho webhook payOS (payment-requests).
    /// Docs: https://payos.vn/docs/du-lieu-tra-ve/webhook/
    /// </summary>
    public class CassoWebhookParser : IWebhookParser
    {
        public string GatewayName => "Casso";

        private readonly ILogger<CassoWebhookParser> _logger;

        public CassoWebhookParser(ILogger<CassoWebhookParser> logger)
        {
            _logger = logger;
        }

        public WebhookPayload Parse(string payload)
        {
            try
            {
                var json = JsonDocument.Parse(payload);
                var root = json.RootElement;

                // Cấu trúc thật (payOS payment-requests webhook):
                // {
                //   "code": "00", "desc": "success", "success": true,
                //   "data": {
                //     "orderCode": 123,          // số nguyên - mã đơn hàng đã truyền khi tạo Payment Link
                //     "amount": 3000,
                //     "description": "VQRIO123",
                //     "reference": "TF230204212323",   // mã giao dịch ngân hàng
                //     "paymentLinkId": "...",
                //     ...
                //   },
                //   "signature": "..."
                // }

                if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Payload không có object 'data' hợp lệ");
                }

                var transactionId = dataElement.TryGetProperty("reference", out var refEl)
                    ? refEl.GetString() ?? ""
                    : "";

                var paymentLinkId = dataElement.TryGetProperty("paymentLinkId", out var linkEl)
                    ? linkEl.GetString() ?? ""
                    : "";

                // Nhận diện payload test cố định mà payOS gửi khi bạn bấm "Lưu" webhook URL trên dashboard
                // (đây là bộ giá trị mẫu công khai trong tài liệu payOS, không trùng với giao dịch thật nào)
                if (transactionId == "TF230204212323" && paymentLinkId == "124c33293c43417ab7879e14c8d9eb18")
                {
                    _logger.LogInformation("Nhận diện đây là request test/validate webhook từ payOS — trả về OK");
                    return new WebhookPayload
                    {
                        TransactionId = transactionId,
                        OrderCode = "",
                        Amount = 0,
                        Status = "Ping",
                        ResponseCode = "0",
                        ResponseMessage = "Validation ping - payload mẫu từ payOS",
                        RawResponse = payload
                    };
                }

                var description = dataElement.TryGetProperty("description", out var descEl)
                    ? descEl.GetString() ?? ""
                    : "";

                var amount = dataElement.TryGetProperty("amount", out var amtEl)
                    ? amtEl.GetDecimal()
                    : 0;

                // Ưu tiên tìm mã đơn hàng dạng "ORD-..." trong description trước
                // (vì payOS's "orderCode" là số nguyên, có thể không trùng OrderCode dạng chuỗi của hệ thống bạn)
                var orderCode = ExtractOrderCode(description);

                if (string.IsNullOrWhiteSpace(orderCode) && dataElement.TryGetProperty("orderCode", out var ocEl))
                {
                    orderCode = ocEl.ValueKind == JsonValueKind.Number
                        ? ocEl.GetRawText()
                        : ocEl.GetString() ?? "";
                }

                if (string.IsNullOrWhiteSpace(orderCode))
                {
                    _logger.LogWarning($"Không tìm thấy order code trong payload payOS: {description}");
                }

                _logger.LogInformation($"Parsed payOS webhook: TransactionId={transactionId}, OrderCode={orderCode}, Amount={amount}");

                return new WebhookPayload
                {
                    TransactionId = transactionId,
                    OrderCode = orderCode,
                    Amount = amount,
                    Status = "Paid",
                    ResponseCode = "0",
                    ResponseMessage = "Payment successful",
                    RawResponse = payload
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing payOS webhook");
                throw new InvalidOperationException($"Failed to parse payOS webhook: {ex.Message}");
            }
        }

        public string PrettyPrint(WebhookPayload payload)
        {
            return $"""
                payOS Webhook:
                - Transaction ID: {payload.TransactionId}
                - Order Code: {payload.OrderCode}
                - Amount: {payload.Amount:N0} VND
                - Status: {payload.Status}
                - Response: {payload.ResponseMessage}
                """;
        }

        private string ExtractOrderCode(string description)
        {
            try
            {
                // Match ORD-260913020211-123 or ORD260913020211123
                var pattern = @"ORD-?(\d{12})-?(\d{3})";
                var match = System.Text.RegularExpressions.Regex.Match(description, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // Reconstruct standard ORD-yyMMddHHmmss-rrr format
                    return $"ORD-{match.Groups[1].Value}-{match.Groups[2].Value}";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}