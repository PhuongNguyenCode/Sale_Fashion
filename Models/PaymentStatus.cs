namespace FashionStore.Models
{
    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded";
        public const string Expired = "Expired";

        public static readonly string[] AllStatuses = 
        { 
            Pending, 
            Processing, 
            Paid, 
            Failed, 
            Cancelled, 
            Refunded, 
            Expired 
        };

        public static bool IsValidStatus(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}
