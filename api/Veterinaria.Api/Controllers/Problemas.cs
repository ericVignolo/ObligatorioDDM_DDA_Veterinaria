using Microsoft.AspNetCore.Mvc;

namespace Veterinaria.Api.Controllers;

// Errores de toda la API en formato Problem Details, con un código propio estable (D-CAT-03).
// Sin datos internos ni clínicos.
public static class Problemas
{
    public static ProblemDetails Crear(int status, string titulo, string detalle, string codigo)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle,
            Type = $"https://vitalia.uy/problemas/{codigo.ToLowerInvariant().Replace('_', '-')}"
        };
        problema.Extensions["code"] = codigo;
        return problema;
    }
}
