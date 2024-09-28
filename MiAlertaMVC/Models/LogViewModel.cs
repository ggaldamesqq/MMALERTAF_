namespace MiAlertaMVC.Models
{
    public class LogViewModel
    {
        public int IDLog { get; set; }
        public string? Funcion { get; set; }
        public string? Texto { get; set; }
        public string? Categoria { get; set; }
        public string? Respuesta { get; set; }
        public string? Campo1 { get; set; }
        public int IDUsuario{ get; set; }
        public DateTime FechaCreacion{ get; set; }


    }
}
