namespace MiAlertaMVC.Models
{
    public class LogWithCountsViewModel
    {
        public IEnumerable<LogViewModel> Logs { get; set; }
        public Dictionary<string, int> LogCounts { get; set; }
    }
}
