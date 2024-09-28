namespace MiAlertaMVC.Models
{
    public class UserViewModel
    {
        public int IDUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Token { get; set; }
        public string? Correo { get; set; }
        public string? Contrasena { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int  Admin { get; set; }
        public int Validado { get; set; }
        public string? NumeroTelefonico { get; set; }
    }
}
