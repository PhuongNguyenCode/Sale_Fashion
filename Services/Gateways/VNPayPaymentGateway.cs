using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FashionStore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FashionStore.Services.Gateways
{
    public class VNPayPaymentGateway : IPaymentGateway
    {
        private readonly VNPayConfiguration _config;
        private readonly ILogger<VNPayPaymentGateway> _logger;
        private readonly HttpClient _httpClient;

        public VNPayPaymentGateway(IOptions<VNPayConfiguration> config, ILogger<VNPayPaymentGateway> logger, HttpClient httpClient)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public string GatewayName => "VNPay";

        public Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                var vnp_Params = new SortedList<string, string>
                {
                    { "vnp_Version", _config.Version ?? "2.1.0" },
                    { "vnp_Command", "pay" },
                    { "vnp_TmnCode", _config.TmnCode ?? "" },
                    { "vnp_Amount", ((long)(request.Amount * 100)).ToString() },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", "VND" },
                    { "vnp_IpAddr", "127.0.0.1" },
                    { "vnp_Locale", "vn" },
                    { "vnp_OrderInfo", $"Thanh toan don hang {request.OrderCode}" },
                    { "vnp_OrderType", "other" },
                    { "vnp_ReturnUrl", request.ReturnUrl ?? "" },
                    { "vnp_TxnRef", request.OrderCode },
                    { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
                };

                var queryStrings = vnp_Params.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}");
                var queryString = string.Join("&", queryStrings);

                var vnp_SecureHash = ComputeHmacSha512(queryString, _config.HashSecret ?? "");

                var paymentUrl = $"{_config.PaymentUrl}?{queryString}&vnp_SecureHash={vnp_SecureHash}&vnp_SecureHashType=HMACSHA512";

                return Task.FromResult(new PaymentGatewayResult
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    TransactionId = request.OrderCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment");
                return Task.FromResult(new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var vnp_Params = new SortedList<string, string>
                {
                    { "vnp_RequestId", Guid.NewGuid().ToString() },
                    { "vnp_Version", _config.Version ?? "2.1.0" },
                    { "vnp_Command", "querydr" },
                    { "vnp_TmnCode", _config.TmnCode ?? "" },
                    { "vnp_TxnRef", transactionId },
                    { "vnp_OrderInfo", $"Query transaction {transactionId}" },
                    { "vnp_TransactionDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_IpAddr", "127.0.0.1" }
                };

                var queryStrings = vnp_Params.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}");
                var queryString = string.Join("&", queryStrings);
                var vnp_SecureHash = ComputeHmacSha512(queryString, _config.HashSecret ?? "");

                var content = new StringContent($"{queryString}&vnp_SecureHash={vnp_SecureHash}&vnp_SecureHashType=HMACSHA512", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await _httpClient.PostAsync(_config.QueryUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseParams = System.Web.HttpUtility.ParseQueryString(responseString);
                    var vnp_ResponseCode = responseParams["vnp_ResponseCode"];
                    var vnp_TransactionStatus = responseParams["vnp_TransactionStatus"];

                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        return new PaymentStatusResult
                        {
                            Success = true,
                            Status = "Paid",
                            TransactionId = responseParams["vnp_TransactionNo"] ?? transactionId
                        };
                    }
                    else if (vnp_ResponseCode == "00" && (vnp_TransactionStatus == "01" || vnp_TransactionStatus == "02"))
                    {
                        return new PaymentStatusResult
                        {
                            Success = true,
                            Status = "Processing",
                            TransactionId = responseParams["vnp_TransactionNo"] ?? transactionId
                        };
                    }
                    else if (vnp_ResponseCode != "00")
                    {
                        // If query failed (e.g., 91 transaction not found), return Success = false so we don't update anything
                        return new PaymentStatusResult
                        {
                            Success = false,
                            Status = "Failed",
                            ErrorMessage = $"VNPay Error: {vnp_ResponseCode} - {responseParams["vnp_Message"]}"
                        };
                    }

                    // Other statuses like cancelled etc.
                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = "Failed",
                        TransactionId = responseParams["vnp_TransactionNo"] ?? transactionId
                    };
                }

                return new PaymentStatusResult
                {
                    Success = false,
                    Status = "Failed",
                    ErrorMessage = responseString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VNPay payment status");
                return new PaymentStatusResult { Success = false, Status = "Failed", ErrorMessage = ex.Message };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var vnp_Params = new SortedList<string, string>
                {
                    { "vnp_RequestId", Guid.NewGuid().ToString() },
                    { "vnp_Version", _config.Version ?? "2.1.0" },
                    { "vnp_Command", "refund" },
                    { "vnp_TmnCode", _config.TmnCode ?? "" },
                    { "vnp_TransactionType", "02" },
                    { "vnp_TxnRef", transactionId },
                    { "vnp_Amount", ((long)(amount * 100)).ToString() },
                    { "vnp_OrderInfo", reason },
                    { "vnp_TransactionDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CreateBy", "admin" },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_IpAddr", "127.0.0.1" }
                };

                var queryStrings = vnp_Params.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}");
                var queryString = string.Join("&", queryStrings);
                var vnp_SecureHash = ComputeHmacSha512(queryString, _config.HashSecret ?? "");

                var content = new StringContent($"{queryString}&vnp_SecureHash={vnp_SecureHash}&vnp_SecureHashType=HMACSHA512", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await _httpClient.PostAsync(_config.RefundUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                return new RefundResult
                {
                    Success = response.IsSuccessStatusCode,
                    RefundId = transactionId,
                    RefundDate = DateTime.Now,
                    ErrorMessage = response.IsSuccessStatusCode ? null : responseString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding VNPay payment");
                return new RefundResult { Success = false, RefundId = transactionId, ErrorMessage = ex.Message };
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var queryParams = payload.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2 && p[0] != "vnp_SecureHash" && p[0] != "vnp_SecureHashType")
                    .ToDictionary(p => p[0], p => WebUtility.UrlDecode(p[1]));

                var sortedParams = new SortedList<string, string>(queryParams);
                var queryStrings = sortedParams.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}");
                var queryString = string.Join("&", queryStrings);

                var computedSignature = ComputeHmacSha512(queryString, _config.HashSecret ?? "");
                return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VNPay signature");
                return false;
            }
        }

        private string ComputeHmacSha512(string data, string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
