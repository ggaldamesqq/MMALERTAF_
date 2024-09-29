namespace MiAlertaMVC.Models
{
    public class UsuarioComunidadViewModel
    {
        public int IDUsuarioComunidad { get; set; }
        public int IDUsuario { get; set; }
        public int IDComunidad { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int  EsAdmin { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Token { get; set; }
        public string Correo { get; set; }
        public string Contraseña { get; set; }
        public int Admin { get; set; }
        public int Validado { get; set; }
        public string NumeroTelefonico { get; set; }
        public string LogoComunidad { get; set; }
    }
}
