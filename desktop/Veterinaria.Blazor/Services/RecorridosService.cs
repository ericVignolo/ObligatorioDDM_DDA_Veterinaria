using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Guía de recorridos demostrativos por rol (A-030). Los datos son ficticios y se vuelven a cargar al recargar la pestaña,
// así cada recorrido se puede repetir con el mismo punto de partida. No es una prueba de seguridad ni de persistencia.
public class RecorridosService
{
    public const string Visitante = "Visitante";
    public const string Cliente = "Cliente";

    private readonly List<Recorrido> _recorridos = new()
    {
        new()
        {
            Rol = Visitante,
            Titulo = "Conocer la clínica sin cuenta",
            Resumen = "Navegación pública: nadie necesita registrarse para informarse. Reservar sí pide ingresar y luego retoma la intención.",
            Pasos =
            {
                new() { Accion = "Abrí el inicio.", Ruta = "/", Resultado = "Se ve la clínica, sus especies atendidas y los accesos al catálogo." },
                new() { Accion = "Mirá el catálogo de servicios y abrí uno.", Ruta = "/servicios", Resultado = "Lista con precios vigentes; el detalle muestra la información del servicio." },
                new() { Accion = "Conocé a los profesionales.", Ruta = "/profesionales", Resultado = "Fichas con especialidad y especies que atienden." },
                new() { Accion = "Revisá cómo se pide asistencia urgente.", Ruta = "/urgencias", Resultado = "Teléfono de la guardia 24 horas, señales de alarma y niveles de asistencia." },
                new() { Accion = "Intentá reservar sin haber ingresado.", Ruta = "/reservar", Resultado = "Se pide ingresar o registrarse; al volver se retoma el servicio que habías elegido (A-011)." },
                new() { Accion = "Leé la política de privacidad.", Ruta = "/privacidad", Resultado = "Cada versión tiene número y fecha; se conserva el historial de versiones aceptadas." }
            }
        },
        new()
        {
            Rol = Cliente,
            Titulo = "Ana reserva, consulta su historia y gestiona sus datos",
            Resumen = "Ana Rodríguez tiene dos animales (Toby y Mishi) y turnos de hoy y mañana en los días en que el profesional trabaja.",
            Correo = "ana.demo@vitalia.uy",
            Pasos =
            {
                new() { Accion = "Ingresá con la cuenta de Ana.", Ruta = "/ingresar", Resultado = "Entrás sin registrarte; la barra superior muestra tu nombre." },
                new() { Accion = "Mirá tus animales.", Ruta = "/animales", Resultado = "Aparecen Toby (perro) y Mishi (gata)." },
                new() { Accion = "Reservá un turno para Toby.", Ruta = "/reservar", Resultado = "Se ofrecen horarios orientativos; el horario se vuelve a verificar al confirmar y el turno queda en «Mis turnos»." },
                new() { Accion = "Reservá la castración de Toby (cirugía).", Ruta = "/reservar?servicio=8", Resultado = "Si el primer día no tiene lugar para 90 min, se salta solo al primer día con horarios. El turno queda «Pendiente de seña» hasta que Recepción cobre el 30 % (RN-35)." },
                new() { Accion = "Revisá tus turnos, cancelá o reprogramá uno.", Ruta = "/turnos", Resultado = "Se muestran las consecuencias de la política antes de confirmar." },
                new() { Accion = "Abrí un animal y entrá a su historia clínica.", Ruta = "/animales", Resultado = "Lectura de las consultas firmadas; las nuevas aparecen después del recorrido del Veterinario." },
                new() { Accion = "Mirá tus comunicaciones y tus derechos sobre los datos.", Ruta = "/cuenta", Resultado = "Preferencias por tipo de aviso (lo opcional viene apagado), exportación y pedido de eliminación." }
            }
        },
        new()
        {
            Rol = PersonalService.Recepcion,
            Titulo = "Laura atiende el mostrador",
            Resumen = "Agenda del día, cobro, internación, asistencia y comunicaciones.",
            Correo = "recepcion@vitalia.uy",
            Pasos =
            {
                new() { Accion = "Ingresá como Recepción.", Ruta = "/ingresar", Resultado = "Vas directo al panel con los atajos de Recepción." },
                new() { Accion = "Abrí la agenda del día.", Ruta = "/panel/agenda", Resultado = "Turnos de todos los profesionales; podés marcar «Llegó» o «Ausente»." },
                new() { Accion = "Cobrá la seña de la castración que reservó Ana.", Ruta = "/panel/cobros", Resultado = "Al pagar el 30 % el turno pasa a Confirmado; la seña se descuenta del cobro final (RN-35)." },
                new() { Accion = "Dá un sobreturno en un horario ocupado.", Ruta = "/panel/agenda", Resultado = "Pide motivo, queda marcado como sobreturno y en la bitácora (RN-09)." },
                new() { Accion = "Cobrá una prestación finalizada.", Ruta = "/panel/cobros", Resultado = "Se registra el pago en persona y se emite el comprobante." },
                new() { Accion = "Admití un animal en internación.", Ruta = "/panel/internacion", Resultado = "El animal ocupa un box libre; el box ocupado no se puede reasignar." },
                new() { Accion = "Gestioná las solicitudes de asistencia a domicilio.", Ruta = "/panel/asistencia", Resultado = "Cola ordenada por prioridad con su estado." },
                new() { Accion = "Reenviá un comprobante desde la bandeja de comunicaciones.", Ruta = "/panel/comunicaciones", Resultado = "El envío queda simulado: nunca sale un correo real." }
            }
        },
        new()
        {
            Rol = PersonalService.Veterinario,
            Titulo = "La Dra. Ferreira atiende sus consultas",
            Resumen = "Agenda propia, consulta con firma, internación, asistencias e inventario.",
            Correo = "ferreira@vitalia.uy",
            Pasos =
            {
                new() { Accion = "Ingresá como Veterinario.", Ruta = "/ingresar", Resultado = "Vas al panel con los atajos de Veterinario." },
                new() { Accion = "Abrí tu agenda e iniciá la atención de un turno.", Ruta = "/panel/mi-agenda", Resultado = "Tus turnos del día; «Iniciar atención» abre la consulta." },
                new() { Accion = "Registrá la consulta y firmala desde «Continuar consulta».", Ruta = "/panel/mi-agenda", Resultado = "Al firmar queda inmutable y se encola el resumen para el Cliente." },
                new() { Accion = "Adelantá una vacuna a Toby (polivalente aplicada hace 60 días) y firmá un tratamiento con una alergia registrada.", Ruta = "/panel/mi-agenda", Resultado = "Pide justificación del adelanto (RN-16) y confirmación del choque con la alergia (RN-17); ambas quedan en la historia." },
                new() { Accion = "Revisá las internaciones a tu cargo.", Ruta = "/panel/internacion", Resultado = "Evolución, novedades y alta." },
                new() { Accion = "Aceptá o rechazá una asistencia.", Ruta = "/panel/asistencia", Resultado = "El estado cambia y queda en el seguimiento." },
                new() { Accion = "Mirá el stock en solo lectura.", Ruta = "/panel/inventario", Resultado = "Lotes y vencimientos, sin botones de compra ni ajuste." }
            }
        },
        new()
        {
            Rol = PersonalService.Administrador,
            Titulo = "Gonzalo administra la clínica",
            Resumen = "Servicios, horarios, usuarios, auditoría, indicadores, parámetros, comunicaciones y datos personales.",
            Correo = "admin@vitalia.uy",
            Pasos =
            {
                new() { Accion = "Ingresá como Administrador.", Ruta = "/ingresar", Resultado = "Vas al panel con todos los atajos de administración." },
                new() { Accion = "Programá un precio con fecha de vigencia.", Ruta = "/panel/admin/servicios", Resultado = "El precio nuevo rige desde la fecha elegida; el histórico no cambia." },
                new() { Accion = "Cargá un feriado o una licencia.", Ruta = "/panel/admin/horarios", Resultado = "Ese día deja de ofrecer horarios." },
                new() { Accion = "Gestioná las cuentas del personal.", Ruta = "/panel/admin/personal", Resultado = "Alta, rol y bloqueo de cuentas." },
                new() { Accion = "Consultá la bitácora y los indicadores.", Ruta = "/panel/admin/auditoria", Resultado = "Registros sin texto clínico; indicadores de ocupación e ingresos." },
                new() { Accion = "Resolvé un pedido de eliminación de datos.", Ruta = "/panel/admin/derechos", Resultado = "Si no hay obstáculos, la cuenta se anonimiza y los animales se conservan." }
            }
        }
    };

    public List<Recorrido> Todos()
    {
        return _recorridos;
    }

    public Recorrido? DeRol(string rol)
    {
        return _recorridos.FirstOrDefault(r => r.Rol == rol);
    }
}
