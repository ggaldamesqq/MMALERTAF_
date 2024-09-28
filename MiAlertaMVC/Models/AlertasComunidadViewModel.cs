namespace MiAlertaMVC.Models
{
    public class AlertasComunidadViewModel
    {
        public int IDComunidad { get; set; }
        public string Descripcion { get; set; }
        public string TextoEmergencia { get; set; }

        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public DateTime FechaHora { get; set; }
        public int IDUsuarioNotificacion { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }
        public string Nombre { get; set; }
        public string NumeroTelefonico { get; set; }



    }
}
