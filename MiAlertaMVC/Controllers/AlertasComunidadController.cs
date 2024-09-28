using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MiAlertaMVC.Controllers
{
    public class AlertasComunidadController : Controller
    {
        private readonly ILogger<AlertasComunidadController> _logger;
        private readonly string _connectionString;
        public AlertasComunidadController(ILogger<AlertasComunidadController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<IActionResult> Index(int idComunidad = 0)
        {
            var idUsuario = HttpContext.Session.GetString("idusuario");
            if (string.IsNullOrEmpty(idUsuario))
            {
                // Manejar el caso donde no haya un idUsuario en la sesión
                return RedirectToAction("Index", "Login");
            }

            int? usuarioId = Convert.ToInt32(idUsuario);
            var comunidades = new List<Comunidad>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var queryComunidades = @"
        SELECT U.IDComunidad, U.Descripcion
        FROM Comunidad U
        INNER JOIN UsuarioComunidad AS UC ON UC.IDComunidad = U.IDComunidad AND UC.EsAdmin = 1
        WHERE UC.EsAdmin = 1 AND UC.IDUsuario = @IDUsuario";

                using (var command = new SqlCommand(queryComunidades, connection))
                {
                    command.Parameters.AddWithValue("@IDUsuario", idUsuario);
                    connection.Open();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            comunidades.Add(new Comunidad
                            {
                                IDComunidad = (int)reader["IDComunidad"],
                                Descripcion = reader["Descripcion"].ToString()
                            });
                        }
                    }
                }
            }

            List<AlertasComunidadViewModel> communities = new List<AlertasComunidadViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
        SELECT U.IDComunidad,
               U.Descripcion,
               n.TextoEmergencia,
               n.Latitud,
               n.Longitud,
               n.FechaHora,
               n.IDUsuario as IDUsuarioNotificacion,
               COALESCE(UU.Correo,'-') AS Correo,
               COALESCE(UU.Direccion,'-') as Direccion,
               COALESCE(UU.Nombre,'-') as Nombre,
               COALESCE(UU.NumeroTelefonico,'-') AS NumeroTelefonico
        FROM Comunidad U
        INNER JOIN UsuarioComunidad AS UC ON UC.IDComunidad = U.IDComunidad AND UC.EsAdmin = 1
        INNER JOIN Notificacion AS N ON N.IDComunidad = U.IDComunidad
        LEFT JOIN Usuario AS UU ON UU.IDUsuario = N.IDUSUARIO
        WHERE UC.EsAdmin = 1 
              AND UC.IDUsuario = @IDUSUARIO
              AND (@IDCOMUNIDAD = 0 OR N.IDComunidad = @IDCOMUNIDAD)
        ORDER BY FechaHora DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Agrega los parámetros a la consulta
                    command.Parameters.AddWithValue("@IDUSUARIO", idUsuario);
                    command.Parameters.AddWithValue("@IDCOMUNIDAD", idComunidad);

                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        communities.Add(new AlertasComunidadViewModel
                        {
                            IDComunidad = reader.GetInt32(0),
                            Descripcion = reader.GetString(1),
                            TextoEmergencia = reader.GetString(2) ?? "",
                            Latitud = reader.GetDecimal(3),
                            Longitud = reader.GetDecimal(4),
                            FechaHora = reader.GetDateTime(5),
                            IDUsuarioNotificacion = reader.GetInt32(6),
                            Correo = reader.GetString(7) ?? "",
                            Direccion = reader.GetString(8) ?? "",
                            Nombre = reader.GetString(9) ?? "",
                            NumeroTelefonico = reader.GetString(10) ?? ""
                        });
                    }
                }
            }

            ViewBag.Comunidades = comunidades;
            ViewBag.ComunidadId = idComunidad;
            return View(communities);
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string idComunidades)
        {
            try
            {
                var comunidadStrings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(idComunidades);
                var comunidadIds = comunidadStrings
                    .Where(comunidad => !comunidad.Equals("Todas las comunidades", StringComparison.OrdinalIgnoreCase))
                    .Select(comunidad =>
                    {
                        // Extraer el primer valor numérico de cada string
                        var idString = comunidad.Split(' ')[0];
                        int.TryParse(idString, out var comunidadId);
                        return comunidadId;
                    })
                    .ToList();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var idUsuario = HttpContext.Session.GetString("idusuario");
                if (string.IsNullOrEmpty(idUsuario))
                {
                    // Manejar el caso donde no haya un idUsuario en la sesión
                    return RedirectToAction("Index", "Login");
                }

                int IDUsuario = Convert.ToInt32(idUsuario);

                using (var package = new ExcelPackage())
                {
                    var worksheetNames = new HashSet<string>();

                    foreach (var comunidadId in comunidadIds)
                    {

                        if (comunidadId != null)
                        {
                            // Obtener los usuarios de la comunidad
                            var usuariosComunidad = await Obtener_Alertas_Comunidades(IDUsuario,comunidadId);

                            // Verificar si hay usuarios para esta comunidad antes de crear la hoja y la tabla
                            if (usuariosComunidad.Any())
                            {
                                // Crear una nueva hoja en el paquete para esta comunidad
                                var sanitizedComunidadName = SanitizeWorksheetName($"{comunidadId}_{IDUsuario}");
                                string finalComunidadName = sanitizedComunidadName;
                                int counter = 1;
                                while (worksheetNames.Contains(finalComunidadName))
                                {
                                    finalComunidadName = $"{sanitizedComunidadName}_{counter}";
                                    counter++;
                                }
                                worksheetNames.Add(finalComunidadName);

                                var worksheet = package.Workbook.Worksheets.Add(finalComunidadName);

                                // Agregar encabezados
                                worksheet.Cells[1, 1].Value = "ID Comunidad";
                                worksheet.Cells[1, 2].Value = "Descripcion";
                                worksheet.Cells[1, 3].Value = "TextoEmergencia";
                                worksheet.Cells[1, 4].Value = "Latitud";
                                worksheet.Cells[1, 5].Value = "Longitud";
                                worksheet.Cells[1, 6].Value = "FechaHora";
                                worksheet.Cells[1, 7].Value = "Usuario Notificacion";
                                worksheet.Cells[1, 8].Value = "Correo";
                                worksheet.Cells[1, 9].Value = "Direccion";
                                worksheet.Cells[1, 10].Value = "Nombre";
                                worksheet.Cells[1, 11].Value = "NumeroTelefonico";
                                // Agregar datos y pintar filas dependiendo del valor de la columna "Validado"
                                var row = 2;
                                foreach (var usuario in usuariosComunidad)
                                {
                                    worksheet.Cells[row, 1].Value = usuario.IDComunidad;
                                    worksheet.Cells[row, 2].Value = usuario.Descripcion;
                                    worksheet.Cells[row, 3].Value = usuario.TextoEmergencia;
                                    worksheet.Cells[row, 4].Value = usuario.Latitud;
                                    worksheet.Cells[row, 5].Value = usuario.Longitud;
                                    worksheet.Cells[row, 6].Value = usuario.FechaHora.ToString("yyyy-MM-dd");
                                    worksheet.Cells[row, 7].Value = usuario.IDUsuarioNotificacion;
                                    worksheet.Cells[row, 8].Value = usuario.Correo;
                                    worksheet.Cells[row, 9].Value = usuario.Direccion;
                                    worksheet.Cells[row, 10].Value = usuario.Nombre;
                                    worksheet.Cells[row, 11].Value = usuario.NumeroTelefonico;

                                    row++;
                                }
                                if (row > 2)
                                {
                                    var tableRange = worksheet.Cells[1, 1, row - 1, 11];
                                    var table = worksheet.Tables.Add(tableRange, $"{finalComunidadName}Table");
                                    table.TableStyle = TableStyles.Medium9;
                                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                                }
                            }
                        }
                    }

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    var fileName = "AlertasComunidades_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private string SanitizeWorksheetName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "Alertas_";
            }

            // Reemplazar caracteres no válidos con un guión bajo
            name = Regex.Replace(name, @"[\\/:*?\[\]]", "_");

            // Asegurarse de que el nombre no empiece con un número o carácter inválido
            if (!char.IsLetter(name[0]))
            {
                name = "Alertas_" + name;
            }

            // Limitar la longitud del nombre a 31 caracteres (máximo permitido por Excel)
            if (name.Length > 31)
            {
                name = name.Substring(0, 31);
            }

            return name;
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


        public async Task<List<AlertasComunidadViewModel>> Obtener_Alertas_Comunidades(int idUsuario, int idComunidad)
        {
            List<AlertasComunidadViewModel> communities = new List<AlertasComunidadViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
SELECT U.IDComunidad,
       U.Descripcion,
       n.TextoEmergencia,
       n.Latitud,
       n.Longitud,
       n.FechaHora,
       n.IDUsuario as IDUsuarioNotificacion,
       COALESCE(UU.Correo,'-') AS Correo,
       COALESCE(UU.Direccion,'-') as Direccion,
       COALESCE(UU.Nombre,'-') as Nombre,
       COALESCE(UU.NumeroTelefonico,'-') AS NumeroTelefonico
FROM Comunidad U
INNER JOIN UsuarioComunidad AS UC ON UC.IDComunidad = U.IDComunidad AND UC.EsAdmin = 1
INNER JOIN Notificacion AS N ON N.IDComunidad = U.IDComunidad
LEFT JOIN Usuario AS UU ON UU.IDUsuario = N.IDUSUARIO
WHERE UC.EsAdmin = 1 
      AND UC.IDUsuario = @IDUSUARIO
      AND (@IDCOMUNIDAD = 0 OR N.IDComunidad = @IDCOMUNIDAD)
ORDER BY FechaHora DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Agrega los parámetros a la consulta
                    command.Parameters.AddWithValue("@IDUSUARIO", idUsuario);
                    command.Parameters.AddWithValue("@IDCOMUNIDAD", idComunidad);

                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        communities.Add(new AlertasComunidadViewModel
                        {
                            IDComunidad = reader.GetInt32(0),
                            Descripcion = reader.GetString(1),
                            TextoEmergencia = reader.GetString(2) ?? "",
                            Latitud = reader.GetDecimal(3),
                            Longitud = reader.GetDecimal(4),
                            FechaHora = reader.GetDateTime(5),
                            IDUsuarioNotificacion = reader.GetInt32(6),
                            Correo = reader.GetString(7) ?? "",
                            Direccion = reader.GetString(8) ?? "",
                            Nombre = reader.GetString(9) ?? "",
                            NumeroTelefonico = reader.GetString(10) ?? ""
                        });
                    }
                }
            }

            return communities; // Retorna la lista de comunidades
        }


    }
}
