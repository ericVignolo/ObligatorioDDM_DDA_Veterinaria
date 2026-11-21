namespace Veterinaria.Api.Services;

// Sección "Jwt" de appsettings.json (RNF-SEG-03).
public class OpcionesJwt
{
    public string Emisor { get; set; } = "Veterinaria.Api";
    public string Audiencia { get; set; } = "Vitalia";

    // Clave de firma de al menos 32 caracteres. La de appsettings.json es solo para desarrollo.
    public string Clave { get; set; } = "";

    public int MinutosAcceso { get; set; } = 15;
    public int DiasRefresco { get; set; } = 7;
}

// Reglas de cuentas y códigos (D-REG-02, D-REG-03, RNF-SEG-09).
public class OpcionesCuentas
{
    public int MinutosCodigo { get; set; } = 15;
    public int IntentosPorCodigo { get; set; } = 5;

    // Códigos por hora: el primero más 3 reenvíos.
    public int CodigosPorHora { get; set; } = 4;

    public int DiasSinVerificar { get; set; } = 7;

    // Desde el quinto intento fallido se bloquea 1, 2, 4… minutos, hasta 30.
    public int IntentosAntesDeBloquear { get; set; } = 5;
    public int MinutosBloqueoMaximo { get; set; } = 30;

    // SOLO DESARROLLO: devuelve el código en la respuesta porque todavía no se envían correos.
    // Program.cs lo activa únicamente con ASPNETCORE_ENVIRONMENT=Development.
    public bool MostrarCodigoEnRespuesta { get; set; }
}
