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

        public async Task<IActionResult> Log(int pageNumber = 1, int pageSize = 10, string filter = null)
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                return RedirectToAction("Index", "Login");
            }

            List<LogViewModel> logs = new List<LogViewModel>();
            Dictionary<string, int> countResults = new Dictionary<string, int>();
            int totalLogs = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Retrieve total log count
                    string countQuery = "SELECT COUNT(*) FROM Log";
                    if (!string.IsNullOrEmpty(filter))
                    {
                        countQuery += " WHERE Respuesta = @filter";
                    }

                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            countCommand.Parameters.Add(new SqlParameter("@filter", filter));
                        }
                        totalLogs = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // Retrieve logs with pagination
                    string query = @"
                SELECT * FROM (
                    SELECT ROW_NUMBER() OVER(ORDER BY FechaCreacion DESC) AS RowNum, * 
                    FROM Log";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE Respuesta = @filter";
                    }

                    query += $@"
                ) AS Result
                WHERE RowNum BETWEEN @startRow AND @endRow
                ORDER BY FechaCreacion DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            command.Parameters.Add(new SqlParameter("@filter", filter));
                        }

                        int startRow = (pageNumber - 1) * pageSize + 1;
                        int endRow = startRow + pageSize - 1;

                        command.Parameters.AddWithValue("@startRow", startRow);
                        command.Parameters.AddWithValue("@endRow", endRow);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                logs.Add(new LogViewModel
                                {
                                    IDLog = reader.GetInt32(1),
                                    Funcion = reader.GetString(2),
                                    Texto = reader.GetString(3),
                                    Categoria = reader.GetString(4),
                                    Respuesta = reader.GetString(5),
                                    Campo1 = reader.GetString(6),
                                    IDUsuario = reader.GetInt32(7),
                                    FechaCreacion = reader.GetDateTime(8)
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
            }

            var model = new LogPageViewModel
            {
                Logs = logs,
                LogCounts = countResults,
                CurrentPage = pageNumber,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalLogs = totalLogs
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
