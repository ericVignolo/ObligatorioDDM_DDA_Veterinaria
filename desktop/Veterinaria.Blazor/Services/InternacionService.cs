using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Internaciones de demostración en memoria (A-024, RF-VET-09 a RF-VET-11, RF-CLI-09, RN-21 a RN-25).
// Recepción admite (datos administrativos); el Veterinario hace el ingreso clínico con box y plan,
// publica novedades y evoluciones y da el alta (decisión de Eric del 31/10).
// En la sección C lo reemplaza InternacionApiService: la API protege el box con una restricción de la base
// y envía las novedades visibles en tiempo real (RF-CLI-09); acá se ven al actualizar (RNF-DIS-04).
public class InternacionService
{
    public const string Admitida = "Admitida";
    public const string Internado = "Internado";
    public const string Alta = "Alta";
    public const string Anulada = "Anulada";

    public const string Novedad = "Novedad";
    public const string Evolucion = "Evolución";
    public const string Rectificacion = "Rectificación";

    public const string PequenosAnimales = "Pequeños animales";
    public const string GrandesAnimales = "Grandes animales";

    public const string AvisoDemo = "Demostración: las internaciones viven solo mientras la aplicación está abierta. En la versión final las guarda la API y las novedades llegan al cliente en tiempo real.";

    // Especies que van a un box de grandes animales.
    private static readonly string[] EspeciesGrandes = { "Caballo", "Vaca" };

    private readonly AnimalService _animales;
    private readonly List<Box> _boxes = new()
    {
        new() { Id = 1, Nombre = "Box 1", Tipo = PequenosAnimales },
        new() { Id = 2, Nombre = "Box 2", Tipo = PequenosAnimales },
        new() { Id = 3, Nombre = "Box 3", Tipo = PequenosAnimales },
        new() { Id = 4, Nombre = "Box equino", Tipo = GrandesAnimales }
    };
    private readonly List<Internacion> _internaciones = new();
    private readonly List<NovedadInternacion> _novedades = new();
    private int _ultimaInternacion;
    private int _ultimaNovedad;

    public InternacionService(AnimalService animales)
    {
        _animales = animales;
        CargarDemo();
    }

    // Canela (yegua de Pedro Gómez) internada hace dos días en el box equino, para que la pantalla no arranque vacía.
    private void CargarDemo()
    {
        var ingreso = DateTime.Today.AddDays(-2).AddHours(9);
        var canela = new Internacion
        {
            Id = ++_ultimaInternacion,
            AnimalId = 4,
            ClienteId = 101,
            Estado = Internado,
            Motivo = "Cólico leve, observación y fluidoterapia",
            TelefonoContacto = "+59899333444",
            FechaAdmision = ingreso,
            AdmitidoPor = "Laura Benítez",
            BoxId = 4,
            PlanTerapeutico = "Fluidoterapia IV 48 h, ayuno 12 h y reintroducción gradual de forraje. Control de frecuencia cardíaca y motilidad cada 6 h.",
            FechaIngreso = ingreso.AddMinutes(40),
            VeterinarioIngreso = "Dra. Valentina Acosta"
        };
        _internaciones.Add(canela);

        AgregarDemo(canela.Id, Evolucion, "Ingresa estable, FC 52 lpm, motilidad disminuida en cuadrante izquierdo.", false, ingreso.AddHours(1));
        AgregarDemo(canela.Id, Novedad, "Canela ingresó bien y está tranquila en su box. Le estamos dando suero.", true, ingreso.AddHours(2));
        AgregarDemo(canela.Id, Evolucion, "Motilidad normalizada. Se inicia reintroducción de forraje.", false, ingreso.AddDays(1));
        AgregarDemo(canela.Id, Novedad, "Canela comió heno con ganas y está más activa. Si sigue así, mañana evaluamos el alta.", true, ingreso.AddDays(1).AddHours(1));
    }

    private void AgregarDemo(int internacionId, string tipo, string texto, bool visible, DateTime fecha)
    {
        _novedades.Add(new NovedadInternacion
        {
            Id = ++_ultimaNovedad,
            InternacionId = internacionId,
            Tipo = tipo,
            Texto = texto,
            Visible = visible,
            Fecha = fecha,
            AutorId = 4,
            AutorNombre = "Dra. Valentina Acosta"
        });
    }

    public List<Box> Boxes()
    {
        return _boxes.ToList();
    }

    // Alta de un box (A-027, RF-ADM-03). El permiso lo controla AdministracionService.
    public Box AgregarBox(string nombre, string tipo)
    {
        var box = new Box { Id = _boxes.Max(b => b.Id) + 1, Nombre = nombre.Trim(), Tipo = tipo };
        _boxes.Add(box);
        return box;
    }

    public Box? ObtenerBox(int id)
    {
        return _boxes.FirstOrDefault(b => b.Id == id);
    }

    // Internación que ocupa el box, si hay (RN-22).
    public Internacion? OcupanteDe(int boxId)
    {
        return _internaciones.FirstOrDefault(i => i.BoxId == boxId && i.Estado == Internado);
    }

    public static string TipoDeBoxPara(Animal animal)
    {
        return EspeciesGrandes.Contains(animal.Especie) ? GrandesAnimales : PequenosAnimales;
    }

    // Boxes libres del tipo que corresponde a la especie del animal.
    public List<Box> BoxesLibresPara(Animal animal)
    {
        var tipo = TipoDeBoxPara(animal);
        return _boxes.Where(b => b.Activo && b.Tipo == tipo && OcupanteDe(b.Id) is null).ToList();
    }

    public Internacion? ObtenerPorId(int id)
    {
        return _internaciones.FirstOrDefault(i => i.Id == id);
    }

    public static bool EstaAbierta(Internacion internacion)
    {
        return internacion.Estado == Admitida || internacion.Estado == Internado;
    }

    // RN-21: a lo sumo una abierta por animal.
    public Internacion? AbiertaDe(int animalId)
    {
        return _internaciones.FirstOrDefault(i => i.AnimalId == animalId && EstaAbierta(i));
    }

    // Admitidas e internadas, primero las que esperan el ingreso clínico.
    public List<Internacion> Abiertas()
    {
        return _internaciones
            .Where(EstaAbierta)
            .OrderBy(i => i.Estado == Admitida ? 0 : 1)
            .ThenBy(i => i.FechaAdmision)
            .ToList();
    }

    public List<Internacion> Altas()
    {
        return _internaciones.Where(i => i.Estado == Alta).OrderByDescending(i => i.FechaAlta).ToList();
    }

    // Lo que ve el cliente de un animal suyo: abiertas y altas, no las admisiones anuladas (RN-18).
    public List<Internacion> DeAnimal(int animalId, int clienteId)
    {
        return _internaciones
            .Where(i => i.AnimalId == animalId && i.ClienteId == clienteId && i.Estado != Anulada)
            .OrderByDescending(i => i.FechaAdmision)
            .ToList();
    }

    // Recepción carga los datos administrativos. El animal no puede tener otra internación abierta (RN-21)
    // y un grupo de animales no se interna (se atiende en el campo).
    public ResultadoOperacion Admitir(AdmisionFormulario datos, Personal? persona)
    {
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción admite una internación." };
        }

        if (!Valido(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados: animal, motivo y teléfono de contacto." };
        }

        var animal = _animales.ObtenerParaPersonal(datos.AnimalId);
        if (animal is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese animal no existe." };
        }

        if (animal.EsGrupo)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"{animal.Nombre} es un grupo de animales: no se interna. Registrá cada animal por separado." };
        }

        if (AbiertaDe(animal.Id) is not null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"{animal.Nombre} ya tiene una internación abierta (RN-21)." };
        }

        var internacion = new Internacion
        {
            Id = ++_ultimaInternacion,
            AnimalId = animal.Id,
            ClienteId = animal.ResponsableId,
            Estado = Admitida,
            Motivo = datos.Motivo.Trim(),
            TelefonoContacto = datos.TelefonoContacto.Trim(),
            Observaciones = datos.Observaciones.Trim(),
            FechaAdmision = DateTime.Now,
            AdmitidoPor = persona.Nombre
        };
        _internaciones.Add(internacion);

        return new ResultadoOperacion { Exito = true, Mensaje = $"{animal.Nombre} quedó admitido. Falta que un veterinario le asigne box y plan." };
    }

    // Una admisión que no llegó a ingresar se anula con motivo (por ejemplo, el responsable se lo llevó).
    public ResultadoOperacion AnularAdmision(int id, string motivo, Personal? persona)
    {
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción anula una admisión." };
        }

        var internacion = ObtenerPorId(id);
        if (internacion is null || internacion.Estado != Admitida)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se anula una admisión que todavía no tiene ingreso clínico." };
        }

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 200)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo de la anulación (hasta 200 caracteres)." };
        }

        internacion.Estado = Anulada;
        internacion.MotivoAnulacion = motivo.Trim();
        return new ResultadoOperacion { Exito = true, Mensaje = "Admisión anulada." };
    }

    // RF-VET-09: el Veterinario asigna un box libre del tipo adecuado (RN-22) y escribe el plan.
    public ResultadoOperacion IngresarClinico(int id, IngresoClinicoFormulario datos, Personal? persona)
    {
        if (!EsVeterinario(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo un veterinario hace el ingreso clínico." };
        }

        var internacion = ObtenerPorId(id);
        if (internacion is null || internacion.Estado != Admitida)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta internación ya no espera el ingreso clínico." };
        }

        if (!Valido(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un box y escribí el plan terapéutico." };
        }

        var box = ObtenerBox(datos.BoxId);
        var animal = _animales.ObtenerParaPersonal(internacion.AnimalId);
        if (box is null || animal is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese box no existe." };
        }

        if (!box.Activo)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"{box.Nombre} está fuera de servicio." };
        }

        if (OcupanteDe(box.Id) is not null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"{box.Nombre} está ocupado: elegí otro (RN-22)." };
        }

        if (box.Tipo != TipoDeBoxPara(animal))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"{box.Nombre} es de {box.Tipo.ToLower()}: no sirve para un {animal.Especie.ToLower()}." };
        }

        internacion.BoxId = box.Id;
        internacion.PlanTerapeutico = datos.PlanTerapeutico.Trim();
        internacion.FechaIngreso = DateTime.Now;
        internacion.VeterinarioIngreso = persona!.Nombre;
        internacion.Estado = Internado;

        return new ResultadoOperacion { Exito = true, Mensaje = $"{animal.Nombre} internado en {box.Nombre}." };
    }

    // RF-VET-10, RF-VET-11: novedades (visibles o internas) y evolución diaria. Solo el Veterinario publica (RN-23).
    public ResultadoOperacion Publicar(int id, NovedadFormulario datos, Personal? persona)
    {
        var error = ValidarPublicacion(id, datos, persona);
        if (error is not null)
        {
            return error;
        }

        if (datos.Tipo != Novedad && datos.Tipo != Evolucion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí si es una novedad o una evolución." };
        }

        Agregar(id, datos.Tipo, datos.Texto, datos.Visible, 0, persona!);
        return new ResultadoOperacion { Exito = true, Mensaje = datos.Visible ? "Publicado: el cliente lo ve." : "Nota interna guardada: el cliente no la ve." };
    }

    // RN-24: una entrada publicada no se edita ni se borra; se corrige con una rectificación.
    // La rectificación tiene la misma visibilidad que la entrada que corrige.
    public ResultadoOperacion Rectificar(int id, int novedadId, NovedadFormulario datos, Personal? persona)
    {
        var error = ValidarPublicacion(id, datos, persona);
        if (error is not null)
        {
            return error;
        }

        var original = _novedades.FirstOrDefault(n => n.Id == novedadId && n.InternacionId == id);
        if (original is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esa entrada no existe." };
        }

        Agregar(id, Rectificacion, datos.Texto, original.Visible, original.Id, persona!);
        return new ResultadoOperacion { Exito = true, Mensaje = "Rectificación publicada. La entrada original se conserva." };
    }

    private ResultadoOperacion? ValidarPublicacion(int id, NovedadFormulario datos, Personal? persona)
    {
        if (!EsVeterinario(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo un veterinario publica en la internación (RN-23)." };
        }

        var internacion = ObtenerPorId(id);
        if (internacion is null || internacion.Estado != Internado)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se publica en una internación en curso." };
        }

        if (!Valido(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el texto (hasta 1000 caracteres)." };
        }

        return null;
    }

    private void Agregar(int internacionId, string tipo, string texto, bool visible, int rectificaId, Personal persona)
    {
        _novedades.Add(new NovedadInternacion
        {
            Id = ++_ultimaNovedad,
            InternacionId = internacionId,
            Tipo = tipo,
            Texto = texto.Trim(),
            Visible = visible,
            RectificaId = rectificaId,
            Fecha = DateTime.Now,
            AutorId = persona.Id,
            AutorNombre = persona.Nombre
        });
    }

    // Todas las entradas, para el Veterinario; de la más reciente a la más antigua.
    public List<NovedadInternacion> Novedades(int internacionId)
    {
        return _novedades
            .Where(n => n.InternacionId == internacionId)
            .OrderByDescending(n => n.Fecha)
            .ThenByDescending(n => n.Id)
            .ToList();
    }

    // Lo que ve el cliente (y Recepción y Administración): solo lo marcado como visible (RN-23).
    public List<NovedadInternacion> NovedadesVisibles(int internacionId)
    {
        return Novedades(internacionId).Where(n => n.Visible).ToList();
    }

    public NovedadInternacion? ObtenerNovedad(int id)
    {
        return _novedades.FirstOrDefault(n => n.Id == id);
    }

    // RF-VET-11, RN-25: sin evolución final e indicaciones no hay alta. El box queda libre.
    public ResultadoOperacion DarAlta(int id, AltaFormulario datos, Personal? persona)
    {
        if (!EsVeterinario(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo un veterinario da el alta." };
        }

        var internacion = ObtenerPorId(id);
        if (internacion is null || internacion.Estado != Internado)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se da el alta a un animal internado." };
        }

        if (!Valido(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Para dar el alta escribí la evolución final y las indicaciones para el domicilio (RN-25)." };
        }

        internacion.EvolucionFinal = datos.EvolucionFinal.Trim();
        internacion.IndicacionesDomicilio = datos.IndicacionesDomicilio.Trim();
        internacion.FechaAlta = DateTime.Now;
        internacion.VeterinarioAlta = persona!.Nombre;
        internacion.Estado = Alta;

        return new ResultadoOperacion { Exito = true, Mensaje = "Alta registrada. El box quedó libre y el cliente ve las indicaciones." };
    }

    private static bool EsVeterinario(Personal? persona)
    {
        return persona is not null && persona.Rol == PersonalService.Veterinario;
    }

    // El servicio no confía en la pantalla: vuelve a aplicar las DataAnnotations.
    private static bool Valido(object datos)
    {
        return Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true);
    }
}
