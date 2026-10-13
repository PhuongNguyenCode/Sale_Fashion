namespace FashionStore.Configuration
{
    public class CassoConfiguration
    {
        public bool Enabled { get; set; }
        public string? ClientId { get; set; }
        public string? ApiKey { get; set; }
        public string? SecureToken { get; set; }
        public string? ChecksumKey { get; set; }   // <-- MỚI: lấy từ my.payos.vn
        public string? ApiUrl { get; set; } = "https://api-merchant.payos.vn";
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? Branch { get; set; }
    }
}