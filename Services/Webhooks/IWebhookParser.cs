namespace FashionStore.Services.Webhooks
{
    public interface IWebhookParser
    {
        /// <summary>
        /// Gateway name this parser handles
        /// </summary>
        string GatewayName { get; }

        /// <summary>
        /// Parse webhook payload from JSON
        /// </summary>
        /// <param name="jsonPayload">Raw JSON payload from gateway</param>
        /// <returns>Parsed webhook payload</returns>
        WebhookPayload Parse(string jsonPayload);

        /// <summary>
        /// Pretty print webhook payload as formatted JSON
        /// </summary>
        /// <param name="payload">Webhook payload to format</param>
        /// <returns>Formatted JSON string</returns>
        string PrettyPrint(WebhookPayload payload);
    }
}
