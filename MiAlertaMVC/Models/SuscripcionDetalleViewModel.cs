namespace MiAlertaMVC.Models
{
    public class SuscripcionDetalleViewModel
    {
        public string SubscriptionId { get; set; }
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime? subscription_start { get; set; }
        public DateTime? subscription_end { get; set; }
        public DateTime period_start { get; set; }
        public DateTime period_end { get; set; }
        public int trial_period_days { get; set; }
        public DateTime? trial_start { get; set; }
        public DateTime? trial_end { get; set; }
        public int cancel_at_period_end { get; set; }
        public DateTime? cancel_at { get; set; }
        public DateTime? next_invoice_date { get; set; }
        public int status { get; set; }
        public int planId { get; set; }
        public string PlanExternalId { get; set; }
        public int Morose { get; set; }
    }
}
