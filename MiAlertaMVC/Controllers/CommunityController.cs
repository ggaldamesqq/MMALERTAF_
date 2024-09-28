using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MiAlertaMVC.Controllers
{
    public class CommunityController : Controller
    {
        private readonly ILogger<CommunityController> _logger;
        private readonly string _connectionString;

        public CommunityController(ILogger<CommunityController> logger, IConfiguration configuration)
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
                    GROUP BY 
                        c.IDComunidad, 
                        c.Descripcion, 
                        c.FechaCreacion, 
                        c.IDUsuario, 
                        c.CodigoIngreso, 
                        c.EsConDominio, 
                        cl.LimiteUsuarios ORDER BY 
        c.IDComunidad DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
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
    }
}
