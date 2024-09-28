namespace MiAlertaMVC.Extensiones
{
    public static class DateTimeExtensions
    {
        public static string ObtenerTiempoTranscurrido(this DateTime fecha)
        {
            var tiempoTranscurrido = DateTime.Now - fecha;

            if (tiempoTranscurrido.TotalSeconds < 60)
                return $"{tiempoTranscurrido.Seconds} segundos atrás";
            if (tiempoTranscurrido.TotalMinutes < 60)
                return $"{tiempoTranscurrido.Minutes} minutos y {tiempoTranscurrido.Seconds} segundos atrás";
            if (tiempoTranscurrido.TotalHours < 24)
                return $"{tiempoTranscurrido.Hours} horas y {tiempoTranscurrido.Minutes} minutos atrás";
            if (tiempoTranscurrido.TotalDays < 7)
                return $"{tiempoTranscurrido.Days} días y {tiempoTranscurrido.Hours} horas atrás";
            if (tiempoTranscurrido.TotalDays < 30)
                return $"{tiempoTranscurrido.Days / 7} semanas y {tiempoTranscurrido.Days % 7} días atrás";
            if (tiempoTranscurrido.TotalDays < 365)
                return $"{tiempoTranscurrido.Days / 30} meses y {tiempoTranscurrido.Days % 30} días atrás";

            return $"{tiempoTranscurrido.Days / 365} años y {(tiempoTranscurrido.Days % 365) / 30} meses atrás";
        }
    }
}