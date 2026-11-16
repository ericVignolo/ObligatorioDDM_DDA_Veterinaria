using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// Arma los servicios de demostración igual que Program.cs, para usarlos en las pruebas.
public class Fabrica
{
    public DocumentoLegalService Legales { get; } = new();
    public PrestacionService Prestaciones { get; } = new();
    public ProfesionalService Profesionales { get; } = new();
    public PersonalService Personal { get; } = new();
    public AnimalService Animales { get; } = new();
    public CuentaService Cuentas { get; }
    public SesionService Sesion { get; }
    public AgendaService Agenda { get; }
    public InventarioService Inventario { get; } = new();
    public ConsultaService Consultas { get; }
    public VacunaService Vacunas { get; }

    public Fabrica()
    {
        Cuentas = new CuentaService(Legales);
        Sesion = new SesionService(Cuentas, Personal);
        Agenda = new AgendaService(Profesionales, Prestaciones, Animales);
        Consultas = new ConsultaService(Agenda, Personal, Profesionales, Inventario);
        Vacunas = new VacunaService(Consultas, Animales, Inventario);
    }

    public static RegistroCliente RegistroValido(string correo = "cliente@prueba.uy", string cedula = "1.234.567-2")
    {
        return new RegistroCliente
        {
            Nombre = "Juana",
            Apellido = "Pérez",
            Correo = correo,
            Telefono = "099 123 456",
            TipoDocumento = "CI",
            NumeroDocumento = cedula,
            Calle = "Rivera",
            NumeroPuerta = "100",
            Localidad = "Nueva Palmira",
            Departamento = "Colonia",
            Contrasena = "clave-segura-1",
            RepetirContrasena = "clave-segura-1",
            AceptaTerminos = true,
            AceptaPrivacidad = true
        };
    }

    // Registra y verifica un cliente; devuelve su cuenta.
    public Cuenta ClienteVerificado(string correo = "cliente@prueba.uy", string cedula = "1.234.567-2")
    {
        Cuentas.Registrar(RegistroValido(correo, cedula));
        var cuenta = Cuentas.ObtenerPorCorreo(correo)!;
        Cuentas.Verificar(correo, cuenta.CodigoDemo);
        return cuenta;
    }

    // Registra un animal para el cliente y lo devuelve.
    public Animal AnimalDe(int clienteId, string especie = "Perro", string nombre = "Firulais", string microchip = "")
    {
        Animales.Crear(new AnimalFormulario { Nombre = nombre, Especie = especie, Sexo = "Macho", Microchip = microchip }, clienteId);
        return Animales.ObtenerDe(clienteId).Last(a => a.Nombre == nombre);
    }

    // Primer día, desde dentro de 3 días, en que el profesional atiende (sin feriado ni ausencia).
    public DateOnly DiaQueAtiende(int profesionalId)
    {
        var profesional = Profesionales.ObtenerPorId(profesionalId)!;
        var dia = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        while (!profesional.Jornada.Any(f => f.Dia == dia.DayOfWeek)
               || profesional.Ausencias.Any(a => dia >= a.Desde && dia <= a.Hasta)
               || Agenda.EsFeriado(dia))
        {
            dia = dia.AddDays(1);
        }

        return dia;
    }

    public Personal PersonalDe(string correo)
    {
        return Personal.ValidarCredenciales(correo, PersonalService.ContrasenaDemo)!;
    }

    // Turno de hoy de la Dra. Ferreira para Toby, con la llegada ya registrada (En espera).
    public Turno TurnoEnEspera()
    {
        var inicio = DateTime.Today.AddHours(7);
        Agenda.OcuparComoOtroCliente(1, 1, inicio);
        var turno = Agenda.TurnosDelDia(DateOnly.FromDateTime(inicio), 1).Last(t => t.Inicio == inicio);
        Agenda.RegistrarLlegada(turno.Id);
        return turno;
    }

    public static ConsultaFormulario ConsultaCompleta()
    {
        return new ConsultaFormulario { Motivo = "Vómitos", Diagnostico = "Gastritis", TipoDiagnostico = "Presuntivo", PesoKg = 18.2m };
    }

    public static DateTime A(DateOnly dia, int hora, int minuto = 0)
    {
        return dia.ToDateTime(new TimeOnly(hora, minuto));
    }
}
