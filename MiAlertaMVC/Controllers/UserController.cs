using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace MiAlertaMVC.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly string _connectionString;

        public UserController(ILogger<UserController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT TOP 500 * FROM Usuario ORDER BY FechaCreacion DESC"; // Ordenado por fecha de creación

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserViewModel
                        {
                            IDUsuario = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            Nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Direccion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Token = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Correo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Contrasena = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            FechaCreacion = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                            Admin = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                            Validado = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            NumeroTelefonico = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                        });
                    }
                }
            }

            return View(users);
        }

    }
}
