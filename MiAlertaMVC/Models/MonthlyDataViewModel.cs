namespace MiAlertaMVC.Models
{
    public class MonthlyDataViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Comunidades { get; set; }
        public int Usuarios { get; set; }
        public int Subscripciones { get; set; }

        public string MonthYear => $"{Year}-{Month:00}";
    }
}
