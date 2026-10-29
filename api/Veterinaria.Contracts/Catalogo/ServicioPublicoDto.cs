namespace Veterinaria.Contracts.Catalogo;

/// <summary>
/// Servicio visible en el catálogo público (RF-VIS-01).
/// Contrato de GET /api/v1/servicios y GET /api/v1/servicios/{id}.
/// Ver docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md.
/// </summary>
public sealed record ServicioPublicoDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    bool ReservableEnLinea,
    PrecioReferenciaDto PrecioReferencia);

/// <summary>
/// Importe de referencia que la interfaz muestra como «Desde» (D-CAT-01, D-CAT-04).
/// No es el cargo final: el cargo usa la tarifa vigente al prestar (RN-33).
/// </summary>
public sealed record PrecioReferenciaDto(decimal Importe, string Moneda);
