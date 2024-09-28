using MiAlertaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MailKit.Net.Smtp;
using MimeKit;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace MiAlertaMVC.Controllers
{
    public class InicioController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly string _connectionString;

        public InicioController(ILogger<UserController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<JsonResult> EnviarCorreoContacto(string CorreoContacto, string NombreContacto, string MensajeContacto)
        {
            try
            {
                // Crear el mensaje de correo
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Mi Alerta", "contacto@mialerta.cl")); // Remitente
                message.To.Add(new MailboxAddress(NombreContacto, CorreoContacto)); // Destinatario
                message.Subject = "Contacto por " + NombreContacto; // Asunto del correo

                // Contenido del correo con formato HTML
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                    <div style='background-color: #f7f7f7; padding: 20px; border-radius: 8px; max-width: 600px; margin: auto;'>
                        <h2 style='text-align: center; color: #4CAF50;'>Nuevo Mensaje de Contacto</h2>
                        <p style='font-size: 16px;'>Mensaje a través del formulario de contacto en <strong>Mi Alerta</strong>.</p>
                        <hr style='border: 1px solid #4CAF50;'>
                        <p><strong>Nombre:</strong> {NombreContacto}</p>
                        <p><strong>Correo Electrónico:</strong> {CorreoContacto}</p>
                        <p><strong>Mensaje:</strong></p>
                        <blockquote style='margin-left: 20px; color: #555;'>{MensajeContacto}</blockquote>
                        <hr style='border: 1px solid #4CAF50;'>
                        <p style='text-align: center; font-size: 12px !important'>Este es un mensaje automático. Por favor, no responda a este correo.</p>
                    </div>
                </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                // Configuración del cliente SMTP usando MailKit
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // Ignorar la validación del certificado
                    client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                    await client.ConnectAsync("mail.mialerta.cl", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync("contacto@mialerta.cl", "4321Xalito.");

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                // Retornar éxito
                return Json(new { success = true, message = "Correo enviado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo");
                return Json(new { success = false, message = $"Error al enviar el correo: {ex.Message}" });
            }
        }


    }
}
