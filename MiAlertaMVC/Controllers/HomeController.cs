using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Diagnostics;

namespace MiAlertaMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }
            var data = GetMonthlyData();
            return View(data);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Log()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private List<MonthlyDataViewModel> GetMonthlyData()
        {
            List<MonthlyDataViewModel> monthlyData = new List<MonthlyDataViewModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string comunidadQuery = @"
                    SELECT 
                        DATEPART(YEAR, Fechacreacion) AS Year,
                        DATEPART(MONTH, Fechacreacion) AS Month,
                        COUNT(*) AS Comunidades
                    FROM 
                        comunidad
                    GROUP BY 
                        DATEPART(YEAR, Fechacreacion), DATEPART(MONTH, Fechacreacion)
                    ORDER BY 
                        DATEPART(YEAR, Fechacreacion), DATEPART(MONTH, Fechacreacion)";

                string usuarioQuery = @"
                    SELECT 
                        DATEPART(YEAR, Fechacreacion) AS Year,
                        DATEPART(MONTH, Fechacreacion) AS Month,
                        COUNT(*) AS Usuarios
                    FROM 
                        usuario
                    GROUP BY 
                        DATEPART(YEAR, Fechacreacion), DATEPART(MONTH, Fechacreacion)
                    ORDER BY 
                        DATEPART(YEAR, Fechacreacion), DATEPART(MONTH, Fechacreacion)";

                string subscripcionQuery = @"
                    SELECT 
                        DATEPART(YEAR, FechaCreacion) AS Year,
                        DATEPART(MONTH, FechaCreacion) AS Month,
                        COUNT(*) AS Subscripciones
                    FROM 
                        CustomerFlowSuscripcion
                    GROUP BY 
                        DATEPART(YEAR, FechaCreacion), DATEPART(MONTH, FechaCreacion)
                    ORDER BY 
                        DATEPART(YEAR, FechaCreacion), DATEPART(MONTH, FechaCreacion)";

                var comunidadData = new Dictionary<string, int>();
                var usuarioData = new Dictionary<string, int>();
                var subscripcionData = new Dictionary<string, int>();

                SqlCommand comunidadCommand = new SqlCommand(comunidadQuery, connection);
                SqlCommand usuarioCommand = new SqlCommand(usuarioQuery, connection);
                SqlCommand subscripcionCommand = new SqlCommand(subscripcionQuery, connection);

                connection.Open();

                SqlDataReader reader;

                // Read comunidad data
                reader = comunidadCommand.ExecuteReader();
                while (reader.Read())
                {
                    string key = $"{reader["Year"]}-{reader["Month"]:00}";
                    comunidadData[key] = (int)reader["Comunidades"];
                }
                reader.Close();

                // Read usuario data
                reader = usuarioCommand.ExecuteReader();
                while (reader.Read())
                {
                    string key = $"{reader["Year"]}-{reader["Month"]:00}";
                    usuarioData[key] = (int)reader["Usuarios"];
                }
                reader.Close();

                // Read subscripcion data
                reader = subscripcionCommand.ExecuteReader();
                while (reader.Read())
                {
                    string key = $"{reader["Year"]}-{reader["Month"]:00}";
                    subscripcionData[key] = (int)reader["Subscripciones"];
                }
                reader.Close();

                connection.Close();

                // Combine data
                var allKeys = new HashSet<string>(comunidadData.Keys);
                allKeys.UnionWith(usuarioData.Keys);
                allKeys.UnionWith(subscripcionData.Keys);

                foreach (var key in allKeys)
                {
                    var split = key.Split('-');
                    int year = int.Parse(split[0]);
                    int month = int.Parse(split[1]);

                    monthlyData.Add(new MonthlyDataViewModel
                    {
                        Year = year,
                        Month = month,
                        Comunidades = comunidadData.ContainsKey(key) ? comunidadData[key] : 0,
                        Usuarios = usuarioData.ContainsKey(key) ? usuarioData[key] : 0,
                        Subscripciones = subscripcionData.ContainsKey(key) ? subscripcionData[key] : 0
                    });
                }

                return monthlyData.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();
            }
        }
    }
}
