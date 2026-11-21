using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Errores de toda la API en formato Problem Details, con un código propio estable (D-CAT-03).
// Sin datos internos ni clínicos.
public static class Problemas
{
    // Política de límite de pedidos por origen para registro, ingreso y recuperación (RNF-SEG-09, Program.cs).
    public const string PoliticaCuentas = "cuentas";

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

    // Convierte el Resultado de un Service en la respuesta HTTP: el valor si salió bien, Problem Details si no.
    public static ActionResult Desde<T>(Resultado<T> resultado)
    {
        if (resultado.Exito)
        {
            return new ObjectResult(resultado.Valor) { StatusCode = resultado.Estado };
        }

        return new ObjectResult(Crear(resultado.Estado, resultado.Titulo, resultado.Mensaje, resultado.Codigo)) { StatusCode = resultado.Estado };
    }
}
