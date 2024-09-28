using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.Data.SqlClient;

namespace MiAlertaMVC.Controllers
{
    public class AccountController : Controller
    {

        private readonly ILogger<AccountController> _logger;
        private readonly string _connectionString;

        public AccountController(ILogger<AccountController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        // Acción de login
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var idusuario = HttpContext.Session.GetString("idusuario");
            var token = HttpContext.Session.GetString("token");

            Console.WriteLine("idusuario:" + idusuario);
            Console.WriteLine("token:" + token);

            base.OnActionExecuting(context);
        }
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var idusuario = HttpContext.Session.GetString("idusuario");
            var token = HttpContext.Session.GetString("token");

            Console.Write("idusuario:" + idusuario);
            Console.Write("token:" + token);

            Console.WriteLine("idusuario:" + idusuario);
            Console.WriteLine("token:" + token);


            return View();
        }
        [HttpGet]
        public IActionResult GetNombreUsuario()
        {
            var nombreUsuario = HttpContext.Session.GetString("nombreusuario");
            return Json(nombreUsuario);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            // Autenticación lógica aquí, e.g., validar usuario y contraseña

            var resultado = ValidarCredencialesAdmin(model.Username, model.Password); // Método ficticio para la autenticación

            if (resultado.EsValido)
            {
                // Almacena los valores en la sesión utilizando HttpContext.Session
                HttpContext.Session.SetString("idusuario", resultado.IDUsuario.ToString());
                HttpContext.Session.SetString("token", resultado.Token.ToString());
                HttpContext.Session.SetString("email", model.Username.ToString());
                HttpContext.Session.SetString("nombreusuario", model.Username.ToString());
                //lblNombreUsuario

                if (model.Username.ToString() == "gonzalogaldamesq@gmail.com")
                {
                    return RedirectToAction("Index", "Home");

                }
                else {
                    return RedirectToAction("Index", "UsuarioComunidad");

                }
            }
            else
            {
                ModelState.AddModelError("", "Username or password is incorrect.");
                return RedirectToAction("Index", "Login");
            }
        }
        public ValidarCredencialesAdminResultado ValidarCredencialesAdmin(string correo, string contrasena)
        {
            ValidarCredencialesAdminResultado resultado = new ValidarCredencialesAdminResultado();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT TOP 1 U.IDUsuario, 0 as IDComunidad, U.Token 
                FROM USUARIO U 
                WHERE Correo = @Correo AND Contraseña = @Contrasena AND Admin = 1";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Correo", correo);
                        command.Parameters.AddWithValue("@Contrasena", contrasena);

                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            resultado.EsValido = true;
                            resultado.IDUsuario = Convert.ToInt32(reader["IDUsuario"]);
                            resultado.IDComunidad = Convert.ToInt32(reader["IDComunidad"]);
                            resultado.Token = reader["Token"].ToString();

                        }
                        else
                        {
                            resultado.EsValido = false;
                        }

                        reader.Close();
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                resultado.EsValido = false;
            }

            return resultado;
        }

        public bool AuthenticateUser(string username, string password)
        {
            return true;
        }
        // Acción de logout
        public ActionResult Logout()
        {
            //FormsAuthentication.SignOut();
            HttpContext.Session.Remove("idusuario");
            HttpContext.Session.Remove("token");
            HttpContext.Session.Remove("email");


            // También puedes usar HttpContext.Session.Clear() para eliminar todas las variables de sesión
            // HttpContext.Session.Clear();

            // Redirigir al usuario a la página de inicio de sesión
            return RedirectToAction("Index", "Login");
        }
    }
}
