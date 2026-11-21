using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Veterinaria.Api.Data;

// Transacción breve con bloqueo de filas (DT-BD-02): READ COMMITTED, SELECT … FOR UPDATE de las filas que
// protegen la operación (en un orden fijo, para no trabarse entre dos pedidos) y hasta 3 reintentos ante un
// interbloqueo, con esperas de 50, 100 y 200 ms. La usan turnos, consulta, stock y cobro.
public static class Transacciones
{
    private static readonly int[] EsperasMs = { 50, 100, 200 };

    // Tablas que se pueden bloquear (el nombre va en el SQL; nunca sale de un dato del pedido).
    public const string Profesionales = "Profesionales";
    public const string Animales = "Animales";
    public const string Consultas = "Consultas";
    public const string Articulos = "Articulos";
    public const string Numeradores = "Numeradores";
    public const string Cargos = "Cargos";

    private static readonly HashSet<string> Permitidas = new() { Profesionales, Animales, Consultas, Articulos, Cargos };

    // `bloqueos`: (tabla, id) en el orden en que se toman. `accion` vuelve a leer lo que necesita, valida y guarda.
    public static async Task<T> EjecutarAsync<T>(AppDbContext context, IEnumerable<(string Tabla, int Id)> bloqueos, Func<Task<T>> accion,
        string? numerador = null)
    {
        var filas = bloqueos.ToList();
        if (filas.Any(f => !Permitidas.Contains(f.Tabla)))
        {
            throw new ArgumentException("Tabla no permitida para bloquear.");
        }

        for (var intento = 0; ; intento++)
        {
            await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                foreach (var (tabla, id) in filas)
                {
                    // El nombre de la tabla es una constante de esta clase; el id va como parámetro.
#pragma warning disable EF1002
                    await context.Database.ExecuteSqlRawAsync($"SELECT `Id` FROM `{tabla}` WHERE `Id` = {{0}} FOR UPDATE", id);
#pragma warning restore EF1002
                }

                // Numerador de comprobantes (clave de texto): siempre al final, después de las filas por Id.
                if (numerador != null)
                {
                    await context.Database.ExecuteSqlInterpolatedAsync($"SELECT `Clave` FROM `Numeradores` WHERE `Clave` = {numerador} FOR UPDATE");
                }

                var resultado = await accion();
                await transaccion.CommitAsync();
                return resultado;
            }
            catch (Exception ex) when (intento < EsperasMs.Length && EsInterbloqueo(ex))
            {
                await transaccion.RollbackAsync();
                context.ChangeTracker.Clear();
                await Task.Delay(EsperasMs[intento]);
            }
        }
    }

    // 1213: interbloqueo; 1205: tiempo de espera del bloqueo agotado.
    public static bool EsInterbloqueo(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is MySqlException mysql && (mysql.Number == 1213 || mysql.Number == 1205))
            {
                return true;
            }
        }

        return false;
    }
}
