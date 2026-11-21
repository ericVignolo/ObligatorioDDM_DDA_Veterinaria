using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Veterinaria.Api.Controllers;

// Dos personas cambiaron el mismo registro a la vez (turno, consulta, internación…): el segundo guardado choca con la
// versión (RNF-CON-05) y responde 409, sin pisar el cambio del primero ni mostrar un error técnico.
public class ManejadorConcurrencia : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        if (excepcion is not DbUpdateConcurrencyException)
        {
            return false;
        }

        contexto.Response.StatusCode = StatusCodes.Status409Conflict;
        await contexto.Response.WriteAsJsonAsync(
            Problemas.Crear(409, "Cambio simultáneo", "Alguien modificó este registro al mismo tiempo. Recargá y volvé a intentar.", "CONFLICTO_DE_VERSION"),
            cancelacion);
        return true;
    }
}
