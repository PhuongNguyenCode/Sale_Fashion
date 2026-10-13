namespace FashionStore.Configuration
{
    public class VNPayConfiguration
    {
        public bool Enabled { get; set; }
        public string? TmnCode { get; set; }
        public string? HashSecret { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QueryUrl { get; set; }
        public string? RefundUrl { get; set; }
        public string? Version { get; set; } = "2.1.0";
    }
}
