namespace MiAlertaMVC.Models
{
    public class SubscripcionViewModel
    {
        public int IDCustomerFlowSuscripcion { get; set; }
        public string IDSuscripcionFlow { get; set; }
        public string CustomerID { get; set; }
        public int EstadoRegistro { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string PlanID { get; set; }
        public int IDComunidad { get; set; }
        public int IDUsuario { get; set; }
        public string Correo { get; set; }
    }
    public class ConfiguracionSubscripcionViewModel 
    {
        public int IDSuscripcion { get; set; }
        public int Precio { get; set; }
        public string RangoUsuarios { get; set; }
        public int UsuariosLimite { get; set; }
    }
}
