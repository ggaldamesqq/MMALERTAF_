namespace MiAlertaMVC.Models
{
    public class CommunityViewModel
    {
        public int IDComunidad { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int IDUsuario { get; set; }
        public string? CodigoIngreso { get; set; }
        public int EsConDominio { get; set; }
        public int TotalUsuarios { get; set; }
        public int? LimiteUsuarios { get; set; } // Nuevo campo para el límite de usuarios
        public string? LogoComunidad { get; set; }

    }
}
