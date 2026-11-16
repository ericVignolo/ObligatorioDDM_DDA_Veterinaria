using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Genera los avisos de refuerzo de vacunas y desparasitaciones (A-028, RF-CLI-07).
// En la sección C lo hace un proceso programado de la API una vez por día; acá lo dispara el personal con un botón.
// Es idempotente: el mismo refuerzo de un animal no genera el aviso dos veces (RNF-CON-10).
public class AvisosRefuerzoService
{
    private readonly ComunicacionesService _comunicaciones;
    private readonly VacunaService _vacunas;
    private readonly CuentaService _cuentas;

    public AvisosRefuerzoService(ComunicacionesService comunicaciones, VacunaService vacunas, CuentaService cuentas)
    {
        _comunicaciones = comunicaciones;
        _vacunas = vacunas;
        _cuentas = cuentas;
    }

    // Devuelve cuántos avisos nuevos se generaron. Respeta la preferencia de cada cliente.
    public int Generar()
    {
        var generados = 0;
        foreach (var cuenta in _cuentas.Todas().Where(c => c.Verificada && !c.Eliminada))
        {
            foreach (var (animal, item) in _vacunas.AlertasDe(cuenta.Id))
            {
                var aplicacion = item.Aplicacion;
                var fecha = aplicacion.FechaRefuerzo.ToString("dd/MM/yyyy");
                var situacion = item.DiasParaRefuerzo < 0 ? $"venció el {fecha}" : $"vence el {fecha}";
                var asunto = $"Refuerzo de {aplicacion.Producto} para {animal.Nombre}";
                var cuerpo = $"Hola, {cuenta.Nombre}. El refuerzo de {aplicacion.Producto} de {animal.Nombre} {situacion}. Podés reservar el turno desde tu cuenta.";
                var clave = $"{animal.Id}|{aplicacion.Producto}|{aplicacion.FechaRefuerzo:yyyyMMdd}";

                if (_comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, asunto, cuerpo, clave) is not null)
                {
                    generados++;
                }
            }
        }

        return generados;
    }
}
