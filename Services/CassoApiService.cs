using System.Net.Http.Headers;
using System.Text.Json;

namespace FashionStore.Services
{
    /// <summary>
    /// Service to interact with Casso API
    /// Docs: https://docs.casso.vn/
    /// </summary>
    public class CassoApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CassoApiService> _logger;

        public CassoApiService(HttpClient httpClient, IConfiguration configuration, ILogger<CassoApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get recent transactions from Casso or payOS
        /// </summary>
        public async Task<List<CassoTransaction>> GetRecentTransactionsAsync(int pageSize = 20)
        {
            try
            {
                var apiKey = _configuration["PaymentGateways:Casso:ApiKey"];
                var clientId = _configuration["PaymentGateways:Casso:ClientId"];
                var apiUrl = _configuration["PaymentGateways:Casso:ApiUrl"] ?? "https://api-merchant.payos.vn";

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Payment gateway ApiKey not configured");
                    return new List<CassoTransaction>();
                }

                _httpClient.DefaultRequestHeaders.Clear();

                // Check if using payOS API
                if (apiUrl.Contains("payos.vn", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                    }
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                    // payOS pushes transaction data in real-time via Webhooks.
                    // Logs confirmation that payOS API credentials are configured properly.
                    _logger.LogDebug($"payOS API configured with ClientId {clientId?.Substring(0, 8)}... - Listening for payOS Webhooks");
                    return new List<CassoTransaction>();
                }

                // Casso API endpoint
                var url = $"{apiUrl}/v2/transactions?pageSize={pageSize}&sort=DESC";
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Apikey {apiKey}");

                _logger.LogInformation($"Calling Casso API: {url}...");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Casso API returned 401 Unauthorized. Please check ApiKey in appsettings.json.");
                    }
                    else
                    {
                        _logger.LogError($"Casso API error: {response.StatusCode} - {content}");
                    }
                    return new List<CassoTransaction>();
                }

                var result = JsonSerializer.Deserialize<CassoApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Data?.Records != null)
                {
                    _logger.LogInformation($"Found {result.Data.Records.Count} transactions from Casso");
                    return result.Data.Records;
                }

                return new List<CassoTransaction>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling payment gateway API");
                return new List<CassoTransaction>();
            }
        }
    }

    #region Casso API Models

    public class CassoApiResponse
    {
        public CassoData? Data { get; set; }
    }

    public class CassoData
    {
        public List<CassoTransaction>? Records { get; set; }
    }

    public class CassoTransaction
    {
        public int Id { get; set; }
        public string? Tid { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? When { get; set; }
        public int BankSubAccId { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? Cusum { get; set; }
    }

    #endregion
}
