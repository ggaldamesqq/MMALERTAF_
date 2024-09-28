using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace MiAlertaMVC.Controllers
{
    public class PerfilController : Controller
    {
        private readonly ILogger<PerfilController> _logger;
        private readonly string _connectionString;

        public PerfilController(ILogger<PerfilController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<IActionResult> Index()
        {
            // Obtener el idusuario desde la sesión
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }

            UsuarioViewModel usuario = await ObtenerUsuarioPorIdAsync(idUsuario);

            if (usuario == null)
            {
                // Manejar el caso donde no se encuentre el usuario
                return NotFound();
            }

            return View(usuario);
        }

        private async Task<UsuarioViewModel> ObtenerUsuarioPorIdAsync(string idUsuario)
        {
            UsuarioViewModel usuario = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT Nombre, Correo, Direccion, Token, Contraseña, NumeroTelefonico FROM usuario WHERE idusuario = @IdUsuario";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                conn.Open();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        usuario = new UsuarioViewModel
                        {
                            Nombre = reader["Nombre"].ToString(),
                            Email = reader["Correo"].ToString(),
                            Direccion = reader["Direccion"].ToString(),
                            Token = reader["Token"].ToString(),
                            Password = reader["Contraseña"].ToString(),
                            NumeroTelefonico = reader["NumeroTelefonico"].ToString(),
                        };
                    }
                }
            }

            return usuario;
        }
        [HttpPost]
        public IActionResult ActualizarPerfil(UsuarioViewModel model)
        {

            // Validar que el número telefónico tenga 9 caracteres y comience con '9'
            if (model.NumeroTelefonico.Length != 9 || model.NumeroTelefonico[0] != '9')
            {
                ModelState.AddModelError("NumeroTelefonico", "El número telefónico debe tener 9 dígitos y comenzar con el número 9.");
            }

                var idUsuario = HttpContext.Session.GetString("idusuario");

                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = @"
                UPDATE usuario
                SET Nombre = @Nombre,
                    Direccion = @Direccion,
                    NumeroTelefonico = @NumeroTelefonico,
                    Contraseña = @Password
                WHERE idusuario = @IdUsuario";

                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                    cmd.Parameters.AddWithValue("@Direccion", model.Direccion);
                    cmd.Parameters.AddWithValue("@NumeroTelefonico", model.NumeroTelefonico);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("Index");
        }

    }
}
