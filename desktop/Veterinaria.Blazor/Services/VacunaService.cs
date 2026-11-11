using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Vacunas y antiparasitarios de demostración en memoria (A-021, RF-VET-04, RF-CLI-07).
// En la sección C lo reemplaza VacunaApiService: la API guarda catálogo, lotes y aplicaciones, descuenta el stock
// en la misma transacción que la firma (A-022), y manda los avisos de refuerzo por la bandeja y por correo.
// Fuera de esta versión: stock por lote y movimientos (A-022), bandeja de notificaciones y correo de aviso,
// y el parámetro de días de aviso editable por el Administrador (RF-ADM-10, panel de administración).
public class VacunaService
{
    public const string Vigente = "Vigente";
    public const string RefuerzoProximo = "Refuerzo próximo";
    public const string Vencida = "Vencida";

    // El catálogo de esta versión no lo validó un veterinario (decisión de Eric, 2026-11-11).
    public const string AvisoCatalogoDemo = "Catálogo de demostración: productos, especies e intervalos de refuerzo a validar por un veterinario antes de usarlos en la clínica.";

    private readonly ConsultaService _consultas;
    private readonly AnimalService _animales;
    private readonly List<AplicacionSanitaria> _aplicaciones = new();
    private int _ultimoId;

    // Días de anticipación del aviso: entre 3 y 5, por defecto 5 (decisión del 31/10). Lo configura el Administrador (RF-ADM-10).
    public int DiasAvisoRefuerzo { get; set; } = 5;

    public List<ProductoSanitario> Productos { get; } = new()
    {
        new() { Id = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Norte", Especies = { "Perro" }, IntervaloRefuerzoDias = 365 },
        new() { Id = 2, Nombre = "Vacuna antirrábica", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Sur", Especies = { "Perro", "Gato" }, IntervaloRefuerzoDias = 365 },
        new() { Id = 3, Nombre = "Vacuna triple felina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Norte", Especies = { "Gato" }, IntervaloRefuerzoDias = 365 },
        new() { Id = 4, Nombre = "Vacuna antitetánica equina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Campo", Especies = { "Caballo" }, IntervaloRefuerzoDias = 365 },
        new() { Id = 5, Nombre = "Vacuna clostridial", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Campo", Especies = { "Vaca", "Oveja", "Cabra" }, IntervaloRefuerzoDias = 180 },
        new() { Id = 6, Nombre = "Antiparasitario interno", Tipo = "Antiparasitario", Laboratorio = "Laboratorio Demo Sur", Especies = { "Perro", "Gato" }, IntervaloRefuerzoDias = 90 },
        new() { Id = 7, Nombre = "Antiparasitario externo", Tipo = "Antiparasitario", Laboratorio = "Laboratorio Demo Sur", Especies = { "Perro", "Gato" }, IntervaloRefuerzoDias = 30 }
    };

    // Dos lotes por producto: uno vigente y uno vencido, para ver la regla RN-15.
    public List<LoteProducto> Lotes { get; } = new();

    public VacunaService(ConsultaService consultas, AnimalService animales)
    {
        _consultas = consultas;
        _animales = animales;
        CargarLotesDemo();
        CargarCarnetDemo();
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    private void CargarLotesDemo()
    {
        var id = 0;
        foreach (var producto in Productos)
        {
            Lotes.Add(new LoteProducto { Id = ++id, ProductoId = producto.Id, Numero = $"L{producto.Id}-VIG", Vencimiento = Hoy.AddMonths(10) });
            Lotes.Add(new LoteProducto { Id = ++id, ProductoId = producto.Id, Numero = $"L{producto.Id}-VENC", Vencimiento = Hoy.AddDays(-20) });
        }
    }

    // Carnet de los animales de demostración: uno vigente, uno con refuerzo próximo y uno vencido,
    // para que las tres situaciones se vean sin tener que esperar meses.
    private void CargarCarnetDemo()
    {
        // La polivalente de Toby es la de su consulta firmada de la historia demo; las demás son anteriores al sistema.
        var consultaDeToby = _consultas.HistoriaDe(1).FirstOrDefault();
        AgregarDemo(1, consultaDeToby?.Id ?? 0, 1, 60, "Dra. Lucía Ferreira", 2);
        AgregarDemo(1, 0, 6, 86, "Dra. Lucía Ferreira", 2);
        AgregarDemo(1, 0, 2, 400, "Dra. Lucía Ferreira", 2);
        AgregarDemo(2, 0, 3, 200, "Dra. Lucía Ferreira", 2);
        AgregarDemo(4, 0, 4, 100, "Dra. Valentina Acosta", 4);
    }

    // Carnet de ejemplo para los animales que carga el botón «Cargar ejemplos» de Mis animales (A-013):
    // el caballo con el refuerzo por vencer y la majada con uno vencido. Así el cliente de una cuenta nueva
    // ve las alertas sanitarias sin esperar meses. Si el animal ya tiene aplicaciones, no se repite.
    public void CargarCarnetEjemplo(int responsableId)
    {
        foreach (var animal in _animales.ObtenerDe(responsableId))
        {
            if (_aplicaciones.Any(a => a.AnimalId == animal.Id))
            {
                continue;
            }

            if (animal.Nombre == "Lucero" && animal.Especie == "Caballo")
            {
                AgregarDemo(animal.Id, 0, 4, 361, "Dra. Valentina Acosta", 4);
            }
            else if (animal.Nombre == "Majada del potrero norte" && animal.Especie == "Oveja")
            {
                AgregarDemo(animal.Id, 0, 5, 200, "Dra. Valentina Acosta", 4);
            }
        }
    }

    private void AgregarDemo(int animalId, int consultaId, int productoId, int haceDias, string autor, int autorId)
    {
        var producto = Productos.First(p => p.Id == productoId);
        var lote = Lotes.First(l => l.ProductoId == productoId && l.Vencimiento >= Hoy);
        var fecha = Hoy.AddDays(-haceDias);
        _aplicaciones.Add(Crear(animalId, consultaId, producto, lote, fecha, autorId, autor));
    }

    private AplicacionSanitaria Crear(int animalId, int consultaId, ProductoSanitario producto, LoteProducto lote, DateOnly fecha, int autorId, string autor)
    {
        return new AplicacionSanitaria
        {
            Id = ++_ultimoId,
            AnimalId = animalId,
            ConsultaId = consultaId,
            ProductoId = producto.Id,
            Producto = producto.Nombre,
            Tipo = producto.Tipo,
            Laboratorio = producto.Laboratorio,
            LoteId = lote.Id,
            Lote = lote.Numero,
            LoteVence = lote.Vencimiento,
            FechaAplicacion = fecha,
            // RN-19: la fecha de refuerzo la calcula el producto, nadie la escribe.
            FechaRefuerzo = fecha.AddDays(producto.IntervaloRefuerzoDias),
            AplicadaPorId = autorId,
            AplicadaPor = autor
        };
    }

    public ProductoSanitario? ObtenerProducto(int id)
    {
        return Productos.FirstOrDefault(p => p.Id == id);
    }

    public List<LoteProducto> LotesDe(int productoId)
    {
        return Lotes.Where(l => l.ProductoId == productoId).OrderByDescending(l => l.Vencimiento).ToList();
    }

    // RN-15: un lote vencido no se aplica. El día de vencimiento todavía sirve.
    public bool LoteVencido(LoteProducto lote)
    {
        return lote.Vencimiento < Hoy;
    }

    // Un producto no aplica a una especie para la que no está indicado.
    public bool Aplica(ProductoSanitario producto, string especie)
    {
        return producto.Especies.Contains(especie);
    }

    // RF-VET-04: registra la aplicación dentro de la consulta abierta de quien la atiende.
    // Valida todo antes de guardar; si algo falla, no se registra nada.
    public ResultadoOperacion Registrar(int consultaId, AplicacionFormulario datos, Personal? veterinario)
    {
        var consulta = _consultas.ObtenerPorId(consultaId);
        if (consulta is null || !_consultas.PuedeEditar(consulta, veterinario) || !_consultas.EstaAbierta(consulta))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta consulta ya no se puede editar." };
        }

        var producto = ObtenerProducto(datos.ProductoId);
        if (producto is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un producto." };
        }

        var lote = Lotes.FirstOrDefault(l => l.Id == datos.LoteId && l.ProductoId == producto.Id);
        if (lote is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un lote de ese producto." };
        }

        var animal = _animales.ObtenerParaPersonal(consulta.AnimalId);
        if (animal is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No se encontró el animal de la consulta." };
        }

        if (!Aplica(producto, animal.Especie))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"No aplicable: {producto.Nombre} no está indicado para la especie {animal.Especie.ToLower()}." };
        }

        if (LoteVencido(lote))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"El lote {lote.Numero} venció el {lote.Vencimiento:dd/MM/yyyy}: no se puede aplicar (RN-15)." };
        }

        if (_aplicaciones.Any(a => a.ConsultaId == consulta.Id && a.ProductoId == producto.Id))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Ya registraste {producto.Nombre} en esta consulta." };
        }

        var aplicacion = Crear(animal.Id, consulta.Id, producto, lote, Hoy, veterinario!.Id, veterinario.Nombre);
        _aplicaciones.Add(aplicacion);
        return new ResultadoOperacion
        {
            Exito = true,
            Mensaje = $"{producto.Nombre} registrada. Refuerzo calculado: {aplicacion.FechaRefuerzo:dd/MM/yyyy}."
        };
    }

    // Mientras la consulta es un borrador abierto se puede deshacer una aplicación; firmada, no (RN-12).
    public ResultadoOperacion Quitar(int aplicacionId, Personal? veterinario)
    {
        var aplicacion = _aplicaciones.FirstOrDefault(a => a.Id == aplicacionId);
        var consulta = aplicacion is null ? null : _consultas.ObtenerPorId(aplicacion.ConsultaId);
        if (aplicacion is null || consulta is null || !_consultas.PuedeEditar(consulta, veterinario) || !_consultas.EstaAbierta(consulta))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta aplicación ya no se puede quitar." };
        }

        _aplicaciones.Remove(aplicacion);
        return new ResultadoOperacion { Exito = true, Mensaje = $"{aplicacion.Producto} quitada de la consulta." };
    }

    public List<AplicacionSanitaria> DeConsulta(int consultaId)
    {
        return _aplicaciones.Where(a => a.ConsultaId == consultaId && consultaId != 0).OrderBy(a => a.Id).ToList();
    }

    // Una aplicación cuenta para el carnet si es anterior al sistema o si su consulta ya se firmó:
    // un borrador, o una consulta cancelada sin atención, no se muestra como vacuna aplicada.
    private bool Cuenta(AplicacionSanitaria aplicacion)
    {
        return aplicacion.ConsultaId == 0 || _consultas.ObtenerPorId(aplicacion.ConsultaId)?.Estado == "Firmada";
    }

    public string EstadoDe(AplicacionSanitaria aplicacion)
    {
        var dias = aplicacion.FechaRefuerzo.DayNumber - Hoy.DayNumber;
        if (dias < 0)
        {
            return Vencida;
        }

        return dias <= DiasAvisoRefuerzo ? RefuerzoProximo : Vigente;
    }

    // Todas las aplicaciones que cuentan, de la más reciente a la más antigua.
    public List<AplicacionSanitaria> HistorialDe(int animalId)
    {
        return _aplicaciones
            .Where(a => a.AnimalId == animalId && Cuenta(a))
            .OrderByDescending(a => a.FechaAplicacion)
            .ThenByDescending(a => a.Id)
            .ToList();
    }

    // Carnet: la última aplicación de cada producto, con el estado de su refuerzo. Primero lo más urgente.
    public List<ItemCarnet> CarnetDe(int animalId)
    {
        return HistorialDe(animalId)
            .GroupBy(a => a.ProductoId)
            .Select(g => g.First())
            .Select(a => new ItemCarnet { Aplicacion = a, Estado = EstadoDe(a), DiasParaRefuerzo = a.FechaRefuerzo.DayNumber - Hoy.DayNumber })
            .OrderBy(i => i.Aplicacion.FechaRefuerzo)
            .ToList();
    }

    // RF-CLI-07: refuerzos próximos o vencidos de los animales de un responsable (RN-18: solo los suyos).
    public List<(Animal Animal, ItemCarnet Item)> AlertasDe(int responsableId)
    {
        var alertas = new List<(Animal, ItemCarnet)>();
        foreach (var animal in _animales.ObtenerDe(responsableId))
        {
            foreach (var item in CarnetDe(animal.Id).Where(i => i.Estado != Vigente))
            {
                alertas.Add((animal, item));
            }
        }

        return alertas.OrderBy(a => a.Item2.DiasParaRefuerzo).ToList();
    }
}
