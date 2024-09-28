using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MiAlertaMVC.Controllers
{
    public class LogController : Controller
    {
        private readonly ILogger<LogController> _logger;
        private readonly string _connectionString;

        public LogController(ILogger<LogController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Log(string filter = null)
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }
            List<LogViewModel> logs = new List<LogViewModel>();
            Dictionary<string, int> countResults = new Dictionary<string, int>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Retrieve logs
                    string query = "SELECT  TOP 200 * FROM Log";
                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE Respuesta = @filter";
                    }
                    query += " ORDER BY FechaCreacion DESC"; // Ordenar por fecha de creación después del filtro

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            command.Parameters.Add(new SqlParameter("@filter", filter));
                        }

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                logs.Add(new LogViewModel
                                {
                                    IDLog = reader.GetInt32(0),
                                    Funcion = reader.GetString(1),
                                    Texto = reader.GetString(2),
                                    Categoria = reader.GetString(3),
                                    Respuesta = reader.GetString(4),
                                    Campo1 = reader.GetString(5),
                                    IDUsuario = reader.GetInt32(6),
                                    FechaCreacion = reader.GetDateTime(7)
                                });
                            }
                        }
                    }

                    // Retrieve log counts
                    countResults = await GetLogCounts();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving logs.");
                // Opcional: Redirigir a una página de error o mostrar un mensaje de error en la vista
            }

            var model = new LogPageViewModel
            {
                Logs = logs,
                LogCounts = countResults
            };

            return View(model);
        }

        private async Task<Dictionary<string, int>> GetLogCounts()
        {
            var counts = new Dictionary<string, int>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT Respuesta, COUNT(*) FROM Log GROUP BY Respuesta";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var respuesta = reader.GetString(0);
                        var count = reader.GetInt32(1);
                        counts[respuesta] = count;
                    }
                }
            }

            return counts;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
