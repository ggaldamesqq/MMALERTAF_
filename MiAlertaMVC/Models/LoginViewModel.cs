namespace MiAlertaMVC.Models
{
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
    public class ValidarCredencialesAdminResultado
    {
        public bool EsValido { get; set; }
        public int IDUsuario { get; set; }
        public int IDComunidad { get; set; }
        public string Token { get; set; }
    }
}
