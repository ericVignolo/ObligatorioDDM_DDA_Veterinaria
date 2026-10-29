namespace Veterinaria.Blazor.Datos.Demo;

/// <summary>
/// Datos institucionales ficticios para el prototipo. Se reemplazan por los
/// reales de la clínica antes de publicar (RNF-LEG-08, RN-45).
/// </summary>
public static class ClinicaDemo
{
    public const string Nombre = "Clínica Veterinaria Demo";
    public const string Lema = "Atención para animales de compañía, equinos y de producción.";
    public const string Direccion = "Calle Ficticia 123, Nueva Palmira, Colonia";
    public const string Telefono = "4544 0000";
    public const string TelefonoGuardia = "099 000 000";
    public const string Correo = "contacto@clinica-demo.example";
    public const string Horario = "Lunes a viernes de 9:00 a 19:00 · Sábados de 9:00 a 13:00";

    /// <summary>Número en formato para enlaces tel: (E.164).</summary>
    public const string TelefonoGuardiaE164 = "+59899000000";
    public const string TelefonoE164 = "+59845440000";
}
