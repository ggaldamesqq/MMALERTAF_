namespace MiAlertaMVC.Models
{
    public class LogPageViewModel
    {
        public IEnumerable<LogViewModel> Logs { get; set; }
        public Dictionary<string, int> LogCounts { get; set; }
    }
}
