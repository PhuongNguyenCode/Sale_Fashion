using Microsoft.AspNetCore.Mvc;

namespace FashionStore.Services
{
    public interface IWebhookHandler
    {
        /// <summary>
        /// Handle webhook from payment gateway
        /// </summary>
        Task<IActionResult> HandleWebhookAsync(string gateway, HttpRequest request);
    }
}
