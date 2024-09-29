namespace MiAlertaMVC.Models
{
    public class LogPageViewModel
    {
        public IEnumerable<LogViewModel> Logs { get; set; }
        public Dictionary<string, int> LogCounts { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int  PageNumber {get;set;}
        public int TotalLogs { get; set; }
        public string Filter {  get; set; }
    }
}
