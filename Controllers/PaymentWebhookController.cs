using FashionStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FashionStore.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IWebhookHandler _webhookHandler;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IWebhookHandler webhookHandler,
            ILogger<PaymentWebhookController> logger)
        {
            _webhookHandler = webhookHandler;
            _logger = logger;
        }

        /// <summary>
        /// Verify webhook endpoint (for Casso dashboard validation)
        /// </summary>
        [HttpGet("webhook/{gateway}")]
        public IActionResult VerifyWebhookEndpoint(string gateway)
        {
            if (!gateway.Equals("casso", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            _logger.LogInformation($"Webhook endpoint verification request from {gateway}");
            return Ok(new { status = "ready", gateway = gateway, message = "Webhook endpoint is active" });
        }

        /// <summary>
        /// Receive webhooks from payment gateways
        /// </summary>
        [HttpPost("webhook/{gateway}")]
        [IgnoreAntiforgeryToken] // Webhooks come from external sources
        public async Task<IActionResult> ReceiveWebhook(string gateway)
        {
            _logger.LogInformation($"Webhook received from {gateway}");

            try
            {
                var result = await _webhookHandler.HandleWebhookAsync(gateway, Request);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling webhook from {gateway}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Short endpoint for Casso webhook (alias for webhook/casso)
        /// </summary>
        [HttpPost("casso")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ReceiveCassoWebhook()
        {
            _logger.LogInformation("Webhook received from casso (short URL)");

            try
            {
                var result = await _webhookHandler.HandleWebhookAsync("casso", Request);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling webhook from casso");
                return StatusCode(500);
            }
        }
        /// <summary>
        /// VNPay IPN endpoint - VNPay sends payment notification via GET with query params
        /// </summary>
        [HttpGet("ipn/vnpay")]
        public async Task<IActionResult> ReceiveVNPayIPN()
        {
            _logger.LogInformation("VNPay IPN received");

            try
            {
                // VNPay sends IPN as query string
                var queryString = Request.QueryString.Value?.TrimStart('?') ?? "";
                
                _logger.LogInformation($"VNPay IPN query: {queryString}");

                // Create a fake HttpRequest-like body from the query string for the webhook handler
                var memoryStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(queryString));
                Request.Body = memoryStream;
                Request.ContentType = "text/plain";

                var result = await _webhookHandler.HandleWebhookAsync("vnpay", Request);
                
                if (result is OkObjectResult okResult)
                {
                    var msg = okResult.Value?.ToString() ?? "";
                    if (msg.Contains("already processed", StringComparison.OrdinalIgnoreCase))
                        return Ok(new { RspCode = "02", Message = "Order already confirmed" });
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }
                else if (result is BadRequestObjectResult badRequest)
                {
                    var error = badRequest.Value?.ToString() ?? "";
                    if (error.Contains("signature", StringComparison.OrdinalIgnoreCase)) 
                        return Ok(new { RspCode = "97", Message = "Invalid signature" });
                    if (error.Contains("not found", StringComparison.OrdinalIgnoreCase)) 
                        return Ok(new { RspCode = "01", Message = "Order not found" });
                    if (error.Contains("Amount mismatch", StringComparison.OrdinalIgnoreCase)) 
                        return Ok(new { RspCode = "04", Message = "Invalid amount" });
                }
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay IPN");
                // VNPay expects specific response format
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
    }
}
