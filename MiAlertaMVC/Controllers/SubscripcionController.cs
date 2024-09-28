using Microsoft.AspNetCore.Mvc;
using MiAlertaMVC.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Numerics;
using OfficeOpenXml;
using Newtonsoft.Json;
using OfficeOpenXml.Table;
using System.Text.RegularExpressions;

namespace MiAlertaMVC.Controllers
{
    public class SubscripcionController : Controller
    {
        private readonly ILogger<SubscripcionController> _logger;
        private readonly HttpClient _client = new HttpClient();

        private readonly string connectionString;
        private readonly string _ApiBase = "https://b270-190-153-149-233.ngrok-free.app";
        private readonly string _apiBaseUrl = "https://sandbox.flow.cl/api";
        private readonly string _apiKey = "7846952F-415B-40B7-94C0-20L6049C4BCA";
        private readonly string _secretKey = "ae85dbb00102bd85a89895a4c2b186f543ab0fec";

        public SubscripcionController(ILogger<SubscripcionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index(int? comunidadId = null, string filter = null)
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");

            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }
            List<SubscripcionViewModel> subscripciones = new List<SubscripcionViewModel>();
            List<ConfiguracionSubscripcionViewModel> configuraciones = new List<ConfiguracionSubscripcionViewModel>();
            List<SuscripcionDetalleViewModel> detallessubscripcion_flow_plan = new List<SuscripcionDetalleViewModel>();
            List<PlanViewModel> detallesPlan= new List<PlanViewModel>();


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Query to load subscriptions
                string query = "SELECT IDCustomerFlowSuscripcion, IDSuscripcionFlow, CustomerID, EstadoRegistro, FechaCreacion, PlanID, IDComunidad, IDUsuario, Correo FROM CustomerFlowSuscripcion";
                if (!string.IsNullOrEmpty(filter))
                {
                    if (filter == "OK")
                    {
                        query += " WHERE EstadoRegistro = 1";
                    }
                    else if (filter == "ERROR")
                    {
                        query += " WHERE EstadoRegistro = 0";
                    }
                }
                query += " ORDER BY IDCustomerFlowSuscripcion DESC";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    subscripciones.Add(new SubscripcionViewModel
                    {
                        IDCustomerFlowSuscripcion = (int)reader["IDCustomerFlowSuscripcion"],
                        IDSuscripcionFlow = reader["IDSuscripcionFlow"].ToString(),
                        CustomerID = reader["CustomerID"].ToString(),
                        EstadoRegistro = (int)reader["EstadoRegistro"],
                        FechaCreacion = (DateTime)reader["FechaCreacion"],
                        PlanID = reader["PlanID"].ToString(),
                        IDComunidad = (int)reader["IDComunidad"],
                        IDUsuario = (int)reader["IDUsuario"],
                        Correo = reader["Correo"].ToString()
                    });
                }
                reader.Close();

                string configQuery = "SELECT IDSuscripcion, Precio, RangoUsuarios, UsuariosLimite FROM Suscripcion ORDER BY Precio ASC";
                SqlCommand configCommand = new SqlCommand(configQuery, connection);
                SqlDataReader configReader = configCommand.ExecuteReader();

                while (configReader.Read())
                {
                    configuraciones.Add(new ConfiguracionSubscripcionViewModel
                    {
                        IDSuscripcion = (int)configReader["IDSuscripcion"],
                        Precio = (int)configReader["Precio"],
                        RangoUsuarios = configReader["RangoUsuarios"].ToString(),
                        UsuariosLimite = (int)configReader["UsuariosLimite"]
                    });
                }
            }

            // Await the task to get the subscription details
            var detallesSubscripciones = await DetallesSuscripciones(comunidadId?.ToString());

            // Add each subscription detail to the list
            foreach (var suscripcion in detallesSubscripciones)
            {
                detallessubscripcion_flow_plan.Add(new SuscripcionDetalleViewModel
                {
                    SubscriptionId = suscripcion.SubscriptionId,
                    CustomerId = suscripcion.CustomerId,
                    Name = suscripcion.Name,
                    Created = suscripcion.Created,
                    subscription_start = suscripcion.subscription_start,
                    subscription_end = suscripcion.subscription_end,
                    period_start = suscripcion.period_start,
                    period_end = suscripcion.period_end,
                    trial_period_days = suscripcion.trial_period_days,
                    trial_start = suscripcion.trial_start,
                    trial_end = suscripcion.trial_end,
                    cancel_at_period_end = suscripcion.cancel_at_period_end,
                    cancel_at = suscripcion.cancel_at,
                    next_invoice_date = suscripcion.next_invoice_date,
                    status = suscripcion.status,
                    planId = suscripcion.planId,
                    PlanExternalId = suscripcion.PlanExternalId,
                    Morose = suscripcion.Morose
                });
            }

            var planResponse = JsonConvert.DeserializeObject<PlanResponse>(await GetSubscriptionPlansAsync());

            // Ordenar los planes por status y luego por precio
            var sortedPlans = planResponse.Data
                .OrderByDescending(plan => plan.Status) // Primero los que tienen Status = 1
                .ThenBy(plan => plan.Amount)            // Dentro de cada grupo, ordenar por precio
                .ToList();

            // Ahora puedes acceder a los planes dentro de sortedPlans
            foreach (var plan in sortedPlans)
            {
                detallesPlan.Add(new PlanViewModel
                {
                    Id = plan.Id,
                    UserId = plan.UserId,
                    PlanId = plan.PlanId,
                    Name = plan.Name,
                    Currency = plan.Currency,
                    Amount = plan.Amount,
                    Interval = plan.Interval,
                    IntervalCount = plan.IntervalCount,
                    Created = plan.Created,
                    PeriodsNumber = plan.PeriodsNumber,
                    TrialPeriodDays = plan.TrialPeriodDays,
                    DaysUntilDue = plan.DaysUntilDue,
                    UrlCallback = plan.UrlCallback,
                    ChargesRetriesNumber = plan.ChargesRetriesNumber,
                    CurrencyConvertOption = plan.CurrencyConvertOption,
                    Status = plan.Status,
                    Public = plan.Public,
                    CreatedAt = plan.CreatedAt,
                    UpdatedAt = plan.UpdatedAt
                });

                Console.WriteLine($"Plan Name: {plan.Name}, Amount: {plan.Amount}");
            }

            ViewBag.ListadoFlowPlanes = detallesPlan;
            ViewBag.ConfiguracionSubscripciones = configuraciones;
            ViewBag.ListadoFlowSubscripciones = detallessubscripcion_flow_plan;

            ViewBag.ComunidadId = comunidadId;
            return View(subscripciones);
        }
        [HttpPost]
        public async Task<JsonResult> CreatePlan(string planName, int UsuariosLimite)
        {
            try
            {
                // Validación de los parámetros de entrada
                if (string.IsNullOrWhiteSpace(planName))
                {
                    return Json(new { success = false, message = "El nombre del plan no puede estar vacío." });
                }

                if (UsuariosLimite <= 0)
                {
                    return Json(new { success = false, message = "El límite de usuarios debe ser mayor que cero." });
                }

                // Lógica para crear el plan
                string planId = await CreateSubscriptionPlan(planName, UsuariosLimite);

                if (string.IsNullOrEmpty(planId))
                {
                    return Json(new { success = false, message = "No se pudo crear el plan de suscripción." });
                }

                // Inserta el plan en la base de datos
                JsonResult dbResult = await InsertarPlan_BBDD(planName, UsuariosLimite);

                // Verifica si la inserción en la base de datos fue exitosa
                var dbSuccess = (bool)dbResult.Value.GetType().GetProperty("success")?.GetValue(dbResult.Value, null);
                if (dbSuccess)
                {
                    return Json(new { success = true, planId = planId });
                }
                else
                {
                    var dbMessage = (string)dbResult.Value.GetType().GetProperty("message")?.GetValue(dbResult.Value, null);
                    return Json(new { success = false, message = $"Error al insertar el plan en la base de datos: {dbMessage}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> EliminarPlan([FromForm] string deletePlanId)
        {
            try
            {
                bool result = await EliminarPlan_BBDD(deletePlanId);
                if (result)
                {
                    return Ok(new { success = true, message = "Plan eliminado exitosamente" });
                }
                else
                {
                    return NotFound(new { success = false, message = "No se encontró el plan para eliminar" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al eliminar el plan: {ex.Message}" });
            }
        }
        public async Task<bool> EliminarPlan_BBDD(string deletePlanId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("DELETE FROM suscripcion WHERE precio = @deleteplanid", conn))
                    {
                        cmd.Parameters.AddWithValue("@deleteplanid", deletePlanId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return rowsAffected > 0; // Devuelve true si se eliminó al menos una fila
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de la excepción
                Console.WriteLine($"Error al eliminar el plan: {ex.Message}");
                return false;
            }
        }
        public async Task<JsonResult> InsertarPlan_BBDD(string planName, int UsuariosLimite)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                INSERT INTO suscripcion (PRECIO, RANGOUSUARIOS, USUARIOSLIMITE)
                VALUES (@PLANNAME, '0 - ' + CAST(@USUARIOSLIMITE AS VARCHAR(10)), @USUARIOSLIMITE)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Agregar los parámetros
                        command.Parameters.AddWithValue("@PLANNAME", planName);
                        command.Parameters.AddWithValue("@USUARIOSLIMITE", UsuariosLimite);

                        connection.Open();
                        int result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return new JsonResult(new { success = true, message = "Plan insertado correctamente." });
                        }
                        else
                        {
                            return new JsonResult(new { success = false, message = "No se pudo insertar el plan." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return new JsonResult(new { success = false, message = "Error: " + ex.Message });
            }
        }
        public async Task<string> CreateSubscriptionPlan(string name, int UsuariosLimite)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                string planId = name;
                string currency = "CLP";
                int amount = Convert.ToInt32(name);
                int daysUntilDue = 7;
                int interval = 3;
                string urlCallback = _ApiBase + "/api/payment/confirmation";

                var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"planId", planId},
            {"name", name},
            {"currency", currency},
            {"amount", amount.ToString()},
            {"interval", interval.ToString()},
            {"days_until_due", daysUntilDue.ToString()},
            {"urlCallback", urlCallback},
        };

                parameters["s"] = ComputeHMACSHA256(parameters, _secretKey);

                var content = new FormUrlEncodedContent(parameters);
                var response = await _client.PostAsync($"{_apiBaseUrl}/plans/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonResponse);
                    string createdPlanId = jsonObject["planId"].ToString();

                    return createdPlanId;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error en la solicitud: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excepción: {ex.Message}");
            }
        }
        public async Task<List<SuscripcionDetalleViewModel>> DetallesSuscripciones(string planId)
        {

            var suscripcionesJson = await ObtenerSuscripcionesAsync(planId ?? "6000");

            // Deserializar el JSON a una lista de objetos
            var suscripciones = JObject.Parse(suscripcionesJson)["data"].ToObject<List<SuscripcionDetalleViewModel>>();

            return suscripciones;
        }
        [HttpPost]
        public async Task<IActionResult > GuardarDetallesPlan(string idSuscripcion, int precio, string rangoUsuarios, int usuariosLimite)
        {
            try
            {
                var plan = precio;
                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan no encontrado" });
                }
                bool exito =  await ActualizarSuscripcionAsync(idSuscripcion, precio, rangoUsuarios, usuariosLimite);

                // SaveChanges(); // Método para guardar los cambios en la base de datos

                return Json(new { success = true, data = new {  } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<bool> ActualizarSuscripcionAsync(string planName, int precio, string rangoUsuarios, int usuariosLimite)
        {
            // Definir la consulta SQL de actualización
            string query = @"
            UPDATE Suscripcion
            SET Precio = @Precio,
                RangoUsuarios = @RangoUsuarios,
                UsuariosLimite = @UsuariosLimite
            WHERE IDSuscripcion= @PlanName;";

            try
            {
                // Crear una conexión a la base de datos
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Crear el comando SQL con la consulta y la conexión
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Definir los parámetros del comando
                        command.Parameters.AddWithValue("@Precio", precio);
                        command.Parameters.AddWithValue("@RangoUsuarios", rangoUsuarios);
                        command.Parameters.AddWithValue("@UsuariosLimite", usuariosLimite);
                        command.Parameters.AddWithValue("@PlanName", planName);

                        // Abrir la conexión a la base de datos
                        await connection.OpenAsync();

                        // Ejecutar la consulta SQL
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // Verificar si la actualización fue exitosa
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar la excepción (escribir en un log, lanzar una excepción personalizada, etc.)
                Console.WriteLine("Error al actualizar la suscripción: " + ex.Message);
                return false;
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetConfiguraciones()
        {
            List<ConfiguracionSubscripcionViewModel> configuraciones = new List<ConfiguracionSubscripcionViewModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string configQuery = "SELECT IDSuscripcion, Precio, RangoUsuarios, UsuariosLimite FROM Suscripcion ORDER BY Precio ASC";
                SqlCommand configCommand = new SqlCommand(configQuery, connection);
                await connection.OpenAsync();
                SqlDataReader configReader = await configCommand.ExecuteReaderAsync();

                while (configReader.Read())
                {
                    configuraciones.Add(new ConfiguracionSubscripcionViewModel
                    {
                        IDSuscripcion = (int)configReader["IDSuscripcion"],
                        Precio = (int)configReader["Precio"],
                        RangoUsuarios = configReader["RangoUsuarios"].ToString(),
                        UsuariosLimite = (int)configReader["UsuariosLimite"]
                    });
                }
            }

            return Json(configuraciones);
        }

        [HttpPost]
        public IActionResult EliminarSuscripcion(int id)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = "DELETE FROM Suscripcion WHERE IDSuscripcion = @ID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            return NotFound("Suscripción no encontrada.");
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Manejar el error (loggear el error si es necesario)
                return StatusCode(500, "Hubo un error al eliminar la suscripción.");
            }
        }
        public async Task<string> DeleteSubscriptionPlan(string planId)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                // Parámetros para la solicitud
                var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"planId", planId}
        };

                // Firma de los parámetros con el secretKey
                parameters["s"] = ComputeHMACSHA256(parameters, _secretKey);

                // Configuración del contenido de la solicitud
                var content = new FormUrlEncodedContent(parameters);

                // Realiza la solicitud POST al endpoint de eliminación
                var response = await _client.PostAsync($"{_apiBaseUrl}/plans/delete", content);

                // Manejo de la respuesta de la API
                if (response.IsSuccessStatusCode)
                {
                    // Procesa la respuesta exitosa
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonResponse);
                    string deletedPlanId = jsonObject["planId"].ToString();
                    bool success = await EliminarPlan_BBDD(deletedPlanId);

                    if (success)
                    {
                        Console.WriteLine("Plan eliminado con éxito.");
                    }
                    else
                    {
                        Console.WriteLine("No se pudo eliminar el plan.");
                    }
                    return deletedPlanId;
                }
                else
                {
                    // Manejo de error en la respuesta
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error en la solicitud: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepción en caso de fallo en la solicitud
                throw new Exception($"Excepción: {ex.Message}");
            }
        }

        public async Task<List<SuscripcionDetalleViewModel>> DetallesPlan(string planId,int todos)
        {

            var PlanesJson = await ObtenerSuscripcionesAsync(planId ?? "6000");

            // Deserializar el JSON a una lista de objetos
            var Planes= JObject.Parse(PlanesJson)["data"].ToObject<List<SuscripcionDetalleViewModel>>();

            return Planes;
        }

        [HttpPost]
        public async Task<IActionResult> ObtenerDetallesPlan([FromForm] string planId)
        {
            try
            {
                // Aquí llamamos al método que obtiene los detalles del plan
                var detallesPlan = await DetallesPlan(planId,0);

                // Devolvemos los datos como JSON
                return Json(new { success = true, data = detallesPlan });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ObtenerSuscripciones2(int comunidadId)
        {
            var detallesSuscripciones = await DetallesSuscripciones(comunidadId.ToString());
            return PartialView("SubscripcionFlowDetalle", detallesSuscripciones);
        }
        public async Task<string> ObtenerSuscripcionesAsync(string planId, int start = 0, int limit = 10, string filter = null, int? status = null)
        {
            var parameters = new Dictionary<string, string>
    {
        {"apiKey", _apiKey},
        {"planId", planId},
        {"start", start.ToString()},
        {"limit", limit.ToString()}
    };

            if (!string.IsNullOrEmpty(filter))
                parameters["filter"] = filter;

            if (status.HasValue)
                parameters["status"] = status.Value.ToString();

            // Generar la firma
            string signature = ComputeHMACSHA256(parameters, _secretKey);
            parameters["s"] = signature;

            // Construir la URL con los parámetros
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var requestUrl = $"{_apiBaseUrl}/subscription/list?{queryString}";

            try
            {
                var response = await _client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    // Manejar el error de la API
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // Aquí podrías deserializar el errorContent para obtener detalles del error
                    throw new Exception($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones (por ejemplo, loguear el error)
                throw new Exception($"Exception: {ex.Message}", ex);
            }
        }
        public async Task<string> GetSubscriptionPlansAsync(int start = 0, int limit = 10, string filter = null, int? status = null)
        {
            var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"start", start.ToString()},
            {"limit", limit.ToString()}
        };

            if (!string.IsNullOrEmpty(filter))
                parameters["filter"] = filter;

            if (status.HasValue)
                parameters["status"] = status.Value.ToString();

            // Generar la firma
            string signature = ComputeHMACSHA256(parameters, _secretKey);
            parameters["s"] = signature;

            // Construir la URL con los parámetros
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var requestUrl = $"{_apiBaseUrl}/plans/list?{queryString}";

            try
            {
                var response = await _client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception: {ex.Message}", ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string planIds)
        {
            try
            {
                // Deserializar el JSON recibido en una lista de strings
                var planNames = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(planIds);
                planNames.RemoveAll(planName => planName == "Plan Mensual");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Cambia a Commercial si tienes una licencia comercial

                using (var package = new ExcelPackage())
                {
                    var worksheetNames = new HashSet<string>(); // Para manejar nombres duplicados

                    foreach (var planName in planNames)
                    {
                        // Limpiar el nombre de la hoja para asegurarse de que sea válido
                        var sanitizedPlanName = SanitizeWorksheetName(planName);

                        // Si el nombre ya existe, agregar un sufijo numérico
                        string finalPlanName = sanitizedPlanName;
                        int counter = 1;
                        while (worksheetNames.Contains(finalPlanName))
                        {
                            finalPlanName = $"{sanitizedPlanName}_{counter}";
                            counter++;
                        }
                        worksheetNames.Add(finalPlanName);

                        // Obtener los detalles de suscripciones para el plan actual
                        var detallesSuscripciones = await DetallesSuscripciones(planName);
                        var detallesplan_flow_plan = detallesSuscripciones.Select(suscripcion => new SuscripcionDetalleViewModel
                        {
                            SubscriptionId = suscripcion.SubscriptionId,
                            CustomerId = suscripcion.CustomerId,
                            Name = suscripcion.Name,
                            Created = suscripcion.Created,
                            subscription_start = suscripcion.subscription_start,
                            subscription_end = suscripcion.subscription_end,
                            period_start = suscripcion.period_start,
                            period_end = suscripcion.period_end,
                            trial_period_days = suscripcion.trial_period_days,
                            trial_start = suscripcion.trial_start,
                            trial_end = suscripcion.trial_end,
                            cancel_at_period_end = suscripcion.cancel_at_period_end,
                            cancel_at = suscripcion.cancel_at,
                            next_invoice_date = suscripcion.next_invoice_date,
                            status = suscripcion.status,
                            planId = suscripcion.planId,
                            PlanExternalId = suscripcion.PlanExternalId,
                            Morose = suscripcion.Morose
                        }).ToList();

                        // Crear una nueva hoja en el paquete para este plan
                        var worksheet = package.Workbook.Worksheets.Add(finalPlanName);

                        // Agregar encabezados
                        worksheet.Cells[1, 1].Value = "ID Subscripción";
                        worksheet.Cells[1, 2].Value = "ID Customer";
                        worksheet.Cells[1, 3].Value = "Nombre";
                        worksheet.Cells[1, 4].Value = "Fecha Creado";
                        worksheet.Cells[1, 5].Value = "Fecha Inicio";
                        worksheet.Cells[1, 6].Value = "Fecha Final";
                        worksheet.Cells[1, 7].Value = "Periodo Inicio";
                        worksheet.Cells[1, 8].Value = "Periodo Final";
                        worksheet.Cells[1, 9].Value = "Estado";
                        worksheet.Cells[1, 10].Value = "ID Plan";
                        worksheet.Cells[1, 11].Value = "Plan EXTERNAL ($)";
                        worksheet.Cells[1, 12].Value = "Morose";

                        // Agregar datos
                        var row = 2;
                        foreach (var suscripcion in detallesplan_flow_plan)
                        {
                            worksheet.Cells[row, 1].Value = suscripcion.SubscriptionId;
                            worksheet.Cells[row, 2].Value = suscripcion.CustomerId;
                            worksheet.Cells[row, 3].Value = suscripcion.Name;
                            worksheet.Cells[row, 4].Value = suscripcion.Created.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cells[row, 5].Value = suscripcion.subscription_start?.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 6].Value = suscripcion.subscription_end?.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 7].Value = suscripcion.period_start.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 8].Value = suscripcion.period_end.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 9].Value = suscripcion.status;
                            worksheet.Cells[row, 10].Value = suscripcion.planId;
                            worksheet.Cells[row, 11].Value = suscripcion.PlanExternalId;
                            worksheet.Cells[row, 12].Value = suscripcion.Morose;

                            row++;
                        }

                        var tableRange = worksheet.Cells[1, 1, row - 1, 12];
                        var table = worksheet.Tables.Add(tableRange, $"{finalPlanName}Table");
                        table.TableStyle = TableStyles.Medium9;
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    }

                    // Guardar el archivo en un MemoryStream
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    var fileName = "Suscripciones_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // Registrar el error
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        private string SanitizeWorksheetName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "Plan";
            }

            // Reemplazar caracteres no válidos con un guión bajo
            name = Regex.Replace(name, @"[\\/:*?\[\]]", "_");

            // Asegurarse de que el nombre no empiece con un número o carácter inválido
            if (!char.IsLetter(name[0]))
            {
                name = "Plan_" + name;
            }

            // Limitar la longitud del nombre a 31 caracteres (máximo permitido por Excel)
            if (name.Length > 31)
            {
                name = name.Substring(0, 31);
            }

            return name;
        }

        private static string ComputeHMACSHA256(Dictionary<string, string> data, string secretKey)
        {
            var encoding = new UTF8Encoding();
            var sortedParameters = data.OrderBy(p => p.Key);
            var toSign = string.Join("", sortedParameters.Select(p => p.Key + p.Value));
            var keyBytes = encoding.GetBytes(secretKey);
            var dataBytes = encoding.GetBytes(toSign);

            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyBytes))
            {
                var hashBytes = hmacsha256.ComputeHash(dataBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
