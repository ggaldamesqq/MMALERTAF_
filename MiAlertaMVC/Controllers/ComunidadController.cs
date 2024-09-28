using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace MiAlertaMVC.Controllers
{
    public class ComunidadController : Controller
    {
        private readonly ILogger<ComunidadController> _logger;
        private readonly string _connectionString;
        public ComunidadController(ILogger<ComunidadController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<IActionResult> Index()
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }

            List<CommunityViewModel> communities = new List<CommunityViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT 
                c.IDComunidad,
                c.Descripcion,
                c.FechaCreacion,
                c.IDUsuario,
                c.CodigoIngreso,
                c.EsConDominio,
                COUNT(uc.IDUsuario) AS TotalUsuarios,
                cl.LimiteUsuarios
            FROM 
                Comunidad c
            INNER JOIN 
                UsuarioComunidad uc ON uc.IDComunidad = c.IDComunidad
            LEFT JOIN 
                ComunidadLimiteClientes cl ON cl.IDComunidad = c.IDComunidad
            WHERE 
                c.IDUsuario = @IDUSUARIO
            GROUP BY 
                c.IDComunidad, 
                c.Descripcion, 
                c.FechaCreacion, 
                c.IDUsuario, 
                c.CodigoIngreso, 
                c.EsConDominio, 
                cl.LimiteUsuarios
            ORDER BY 
                c.IDComunidad DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Agrega el parámetro @IDUSUARIO a la consulta
                    command.Parameters.AddWithValue("@IDUSUARIO", idUsuario);

                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        communities.Add(new CommunityViewModel
                        {
                            IDComunidad = reader.GetInt32(0),
                            Descripcion = reader.GetString(1),
                            FechaCreacion = reader.GetDateTime(2),
                            IDUsuario = reader.GetInt32(3),
                            CodigoIngreso = reader.GetString(4),
                            EsConDominio = reader.GetInt32(5),
                            TotalUsuarios = reader.GetInt32(6),
                            LimiteUsuarios = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7) // Obtener el límite de usuarios
                        });
                    }
                }
            }

            return View(communities);
        }
         [HttpPost]
        public async Task<JsonResult> ActualizarComunidad(CommunityViewModel model)
        {
            // Validar los datos del modelo
            if (string.IsNullOrEmpty(model.Descripcion))
            {
                return Json(new { success = false, message = "La descripción es requerida." });
            }

            // Obtener el ID del usuario desde la sesión
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                return Json(new { success = false, message = "No hay un usuario en la sesión." });
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = @"
                UPDATE Comunidad
                SET 
                    Descripcion = @Descripcion
                WHERE 
                    IDComunidad = @IDComunidad AND
                    IDUsuario = @IDUsuario";

                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Descripcion", model.Descripcion);
                    cmd.Parameters.AddWithValue("@IDComunidad", model.IDComunidad);
                    cmd.Parameters.AddWithValue("@IDUsuario", idUsuario);

                    connection.Open();
                    await cmd.ExecuteNonQueryAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                _logger.LogError(ex, "Error al actualizar la comunidad");

                return Json(new { success = false, message = "Error al intentar guardar los cambios." });
            }
        }
    }
}
