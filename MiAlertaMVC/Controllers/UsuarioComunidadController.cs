using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml.Table;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OfficeOpenXml.Style;

public class UsuarioComunidadController : Controller
{
    private readonly string connectionString;

    public UsuarioComunidadController(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpPost]
    public async Task<IActionResult> ExportToExcel([FromForm] string planIds)
    {
        try
        {
            var comunidadStrings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(planIds);
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

            using (var package = new ExcelPackage())
            {
                var worksheetNames = new HashSet<string>();

                foreach (var comunidadId in comunidadIds)
                {
                    // Obtener la información de la comunidad
                    var comunidad = await ObtenerComunidadPorId(comunidadId);

                    if (comunidad != null)
                    {
                        // Obtener los usuarios de la comunidad
                        var usuariosComunidad = await ObtenerUsuariosDeComunidad(comunidad.IDComunidad);

                        // Verificar si hay usuarios para esta comunidad antes de crear la hoja y la tabla
                        if (usuariosComunidad.Any())
                        {
                            // Crear una nueva hoja en el paquete para esta comunidad
                            var sanitizedComunidadName = SanitizeWorksheetName($"{comunidad.IDComunidad}_{comunidad.Descripcion}");
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
                            worksheet.Cells[1, 1].Value = "ID Usuario";
                            worksheet.Cells[1, 2].Value = "Nombre";
                            worksheet.Cells[1, 3].Value = "Dirección";
                            worksheet.Cells[1, 4].Value = "Correo";
                            worksheet.Cells[1, 5].Value = "Teléfono";
                            worksheet.Cells[1, 6].Value = "Es Admin";
                            worksheet.Cells[1, 7].Value = "Fecha Creación";
                            worksheet.Cells[1, 8].Value = "Validado";
                            // Agregar datos y pintar filas dependiendo del valor de la columna "Validado"
                            var row = 2;
                            foreach (var usuario in usuariosComunidad)
                            {
                                worksheet.Cells[row, 1].Value = usuario.IDUsuario;
                                worksheet.Cells[row, 2].Value = usuario.Nombre;
                                worksheet.Cells[row, 3].Value = usuario.Direccion;
                                worksheet.Cells[row, 4].Value = usuario.Correo;
                                worksheet.Cells[row, 5].Value = usuario.NumeroTelefonico;
                                worksheet.Cells[row, 6].Value = usuario.EsAdmin;
                                worksheet.Cells[row, 7].Value = usuario.FechaCreacion.ToString("yyyy-MM-dd");
                                worksheet.Cells[row, 8].Value = usuario.Validado == 1 ? "Sí" : "No";

                                // Pintar la fila dependiendo del valor de la columna "Validado"
                                var fillColor = usuario.Validado == 1 ? System.Drawing.Color.FromArgb(224, 255, 224) : System.Drawing.Color.FromArgb(255, 228, 228);
                                worksheet.Cells[row, 1, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(fillColor);

                                row++;
                            }
                            if (row > 2)
                            {
                                var tableRange = worksheet.Cells[1, 1, row - 1, 8];
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
                var fileName = "Comunidades_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    private async Task<Comunidad> ObtenerComunidadPorId(int comunidadId)
    {
        Comunidad comunidad = null;
        using (var connection = new SqlConnection(connectionString))
        {
            var query = "SELECT IDComunidad, Descripcion FROM Comunidad WHERE IDComunidad = @IDComunidad";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IDComunidad", comunidadId);
                connection.Open();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        comunidad = new Comunidad
                        {
                            IDComunidad = (int)reader["IDComunidad"],
                            Descripcion = reader["Descripcion"].ToString()
                        };
                    }
                }
            }
        }
        return comunidad;
    }

    private async Task<List<UsuarioComunidadViewModel>> ObtenerUsuariosDeComunidad(int comunidadId)
    {
        var usuarios = new List<UsuarioComunidadViewModel>();
        using (var connection = new SqlConnection(connectionString))
        {
            var query = @"
        SELECT 
            UC.IDUsuario, 
            U.Nombre, 
            U.Direccion, 
            U.Correo, 
            U.NumeroTelefonico, 
            UC.EsAdmin, 
            UC.FechaCreacion,
	        U.Validado
        FROM 
            UsuarioComunidad UC
        INNER JOIN 
            Usuario U ON UC.IDUsuario = U.IDUsuario
        WHERE 
            UC.IDComunidad = @IDComunidad";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IDComunidad", comunidadId);
                connection.Open();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        usuarios.Add(new UsuarioComunidadViewModel
                        {
                            IDUsuario = (int)reader["IDUsuario"],
                            Nombre = reader["Nombre"].ToString(),
                            Direccion = reader["Direccion"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            NumeroTelefonico = reader["NumeroTelefonico"].ToString(),
                            EsAdmin = (int)reader["EsAdmin"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            Validado= (int)reader["Validado"],

                        });
                    }
                }
            }
        }
        return usuarios;
    }
    private string SanitizeWorksheetName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "_";
        }

        // Reemplazar caracteres no válidos con un guión bajo
        name = Regex.Replace(name, @"[\\/:*?\[\]]", "_");

        // Reemplazar espacios por guiones bajos
        name = name.Replace(" ", "_");

        // Asegurarse de que el nombre no empiece con un número o carácter inválido
        if (!char.IsLetter(name[0]))
        {
            name = "Comunidad_" + name;
        }

        // Limitar la longitud del nombre a 31 caracteres (máximo permitido por Excel)
        if (name.Length > 31)
        {
            name = name.Substring(0, 31);
        }

        return name;
    }


    [HttpPost]
    public JsonResult EliminarUsuarioComunidad(int idUsuario, int idComunidad)
    {
        bool resultado = EliminarUsuarioComunidad2(idUsuario, idComunidad);
        return Json(new { success = resultado });
    }

    [HttpPost]
    public JsonResult BloquearUsuarioComunidad(int idUsuario, int estado, int idComunidad, int idUsuarioBloqueador)
    {
        // Obtener el id del usuario bloqueador desde la sesión
        int idusuarioBloqueador = Convert.ToInt32(HttpContext.Session.GetString("idusuario"));

        // Intentar bloquear al usuario
        bool bloqueoExitoso = InsertarUsuarioBloqueado(idUsuario, 1, idComunidad, idusuarioBloqueador);

        // Si el bloqueo fue exitoso, proceder a eliminar al usuario de la comunidad
        bool eliminacionExitosa = false;
        if (bloqueoExitoso)
        {
            eliminacionExitosa = EliminarUsuarioComunidad2(idUsuario, idComunidad);
        }

        // Retornar el resultado de la operación de eliminación
        return Json(new { success = eliminacionExitosa });
    }

    public bool InsertarUsuarioBloqueado(int idUsuario, int estado, int idComunidad, int idUsuarioBloqueador)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            INSERT INTO UsuarioBloqueado (IDUsuario, Estado, IDComunidad, IDUsuarioBloqueador, FechaRegistro)
            VALUES (@IDUsuario, @Estado, @IDComunidad, @IDUsuarioBloqueador, GETDATE());";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IDUsuario", idUsuario);
                    command.Parameters.AddWithValue("@Estado", estado);
                    command.Parameters.AddWithValue("@IDComunidad", idComunidad);
                    command.Parameters.AddWithValue("@IDUsuarioBloqueador", idUsuarioBloqueador);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("UsuarioBloqueado insertado correctamente.");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al insertar en UsuarioBloqueado: " + ex.Message);
            return false;
        }
    }
    private bool EliminarUsuarioComunidad2(int idUsuario, int idComunidad)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                DELETE FROM UsuarioComunidad
                WHERE IDUsuario = @IDUsuario
                AND IDComunidad = @IDComunidad";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IDUsuario", idUsuario);
                    command.Parameters.AddWithValue("@IDComunidad", idComunidad);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        catch (Exception ex)
        {
            // Registrar log de error
            Console.WriteLine("Error al eliminar en UsuarioComunidad: " + ex.Message);
            return false;
        }
    }
    public async Task<IActionResult> Index(int comunidadId = 0, string filter = "Todos", string searchQuery = "")
    {
        var idUsuario = HttpContext.Session.GetString("idusuario");

        if (string.IsNullOrEmpty(idUsuario))
        {
            // Manejar el caso donde no haya un idUsuario en la sesión
            return RedirectToAction("Index", "Login");
        }
        var usuarios = new List<UsuarioComunidadViewModel>();
        var comunidades = new List<Comunidad>();
        SetSession();

        int? usuarioId = Convert.ToInt32(idUsuario);

        using (var connection = new SqlConnection(connectionString))
        {
            var queryComunidades = @"
            SELECT U.IDComunidad, U.Descripcion
            FROM Comunidad U
            INNER JOIN UsuarioComunidad AS UC ON UC.IDComunidad = U.IDComunidad AND UC.EsAdmin = 1
            WHERE UC.EsAdmin = 1 AND UC.IDUsuario = @IDUsuario";

            using (var command = new SqlCommand(queryComunidades, connection))
            {
                command.Parameters.AddWithValue("@IDUsuario", idUsuario );
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

        using (var connection = new SqlConnection(connectionString))
        {
            var queryUsuarios = @"
            SELECT 
                UC.IDUsuarioComunidad, 
                UC.IDUsuario, 
                UC.IDComunidad, 
                UC.FechaCreacion, 
                UC.EsAdmin, 
                U.Nombre, 
                U.Direccion, 
                U.Token, 
                U.Correo, 
                U.Contraseña, 
                U.Admin, 
                U.Validado, 
                U.NumeroTelefonico,
	            cl.UrlLogo

            FROM 
                UsuarioComunidad UC
            INNER JOIN 
                Usuario U ON U.IDUsuario = UC.IDUsuario
            INNER JOIN 
		        Comunidad C ON C.IDComunidad = UC.IDComunidad
            LEFT JOIN 
                ComunidadLogo as CL ON cl.IDComunidad = c.IDComunidad
            WHERE 
                (@IDComunidad = 0 OR UC.IDComunidad = @IDComunidad)
                AND C.IDUsuario = @IDUsuario";

            if (!string.IsNullOrEmpty(searchQuery))
            {
                queryUsuarios += @" AND 
                (U.IDUsuario LIKE @SearchQuery OR 
                U.Nombre LIKE @SearchQuery OR 
                U.Correo LIKE @SearchQuery OR 
                U.Direccion LIKE @SearchQuery)";
            }

            if (filter == "OK")
            {
                queryUsuarios += " AND U.Validado = 1";
            }
            else if (filter == "ERROR")
            {
                queryUsuarios += " AND U.Validado = 0";
            }

            queryUsuarios += " ORDER BY UC.FechaCreacion DESC";

            using (var command = new SqlCommand(queryUsuarios, connection))
            {
                command.Parameters.AddWithValue("@IDComunidad", comunidadId);
                command.Parameters.AddWithValue("@IDUsuario", usuarioId ?? 0);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                connection.Open();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        usuarios.Add(new UsuarioComunidadViewModel
                        {
                            IDUsuarioComunidad = (int)reader["IDUsuarioComunidad"],
                            IDUsuario = (int)reader["IDUsuario"],
                            IDComunidad = (int)reader["IDComunidad"],
                            FechaCreacion = (DateTime)reader["FechaCreacion"],
                            EsAdmin = (int)reader["EsAdmin"],
                            Nombre = reader["Nombre"].ToString(),
                            Direccion = reader["Direccion"].ToString(),
                            Token = reader["Token"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            Contraseña = reader["Contraseña"].ToString(),
                            Admin = (int)reader["Admin"],
                            Validado = (int)reader["Validado"],
                            NumeroTelefonico = reader["NumeroTelefonico"].ToString(),
                            LogoComunidad = reader["UrlLogo"].ToString()
                        });
                    }
                }
            }
        }

        ViewBag.ComunidadId = comunidadId;
        ViewBag.Comunidades = comunidades;
        ViewBag.Filter = filter;
        ViewBag.SearchQuery = searchQuery;
        return View(usuarios);
    }
    public async Task<IActionResult> DeleteUser(int idUsuario, int idComunidad)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            var query = "DELETE FROM UsuarioComunidad WHERE IDUsuario = @IDUsuario AND IDComunidad = @IDComunidad";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IDUsuario", idUsuario);
                command.Parameters.AddWithValue("@IDComunidad", idComunidad);
                connection.Open();
                await command.ExecuteNonQueryAsync();
            }
        }

        return RedirectToAction("Index", new { comunidadId = idComunidad });
    }

    public IActionResult SetSession()
    {
        // Establecer una sesión de prueba con IDUsuario = 1
        HttpContext.Session.SetInt32("IDUsuario", 1);
        return RedirectToAction("Index");
    }
}
