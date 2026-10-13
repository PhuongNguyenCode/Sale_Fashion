namespace FashionStore.Configuration
{
    public class MoMoConfiguration
    {
        public string? PartnerCode { get; set; }
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? ApiUrl { get; set; }
        public string? QueryApiUrl { get; set; }
        public string? RefundApiUrl { get; set; }
        public bool Enabled { get; set; }
    }


    public class BankTransferConfiguration
    {
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? Branch { get; set; }
        public bool Enabled { get; set; }
    }
}
