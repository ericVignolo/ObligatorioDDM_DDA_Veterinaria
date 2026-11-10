using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Data;

// Base de datos "vitalia" en MySQL 8.4 (DT-BD-01, DT-BD-02).
// Primera versión: identidad, animales, catálogo, profesionales con agenda, recursos y turnos.
// Consulta, historia clínica, stock, cargos, internación y asistencia se agregan con nuevas migraciones.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Identidad y legales
    public DbSet<Cuenta> Cuentas { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<CuentaRol> CuentaRoles { get; set; }
    public DbSet<CodigoCuenta> CodigosCuenta { get; set; }
    public DbSet<DocumentoLegal> DocumentosLegales { get; set; }
    public DbSet<AceptacionLegal> AceptacionesLegales { get; set; }

    // Animales
    public DbSet<Especie> Especies { get; set; }
    public DbSet<Animal> Animales { get; set; }
    public DbSet<Responsabilidad> Responsabilidades { get; set; }

    // Catálogo y agenda
    public DbSet<Prestacion> Prestaciones { get; set; }
    public DbSet<Profesional> Profesionales { get; set; }
    public DbSet<ProfesionalEspecie> ProfesionalEspecies { get; set; }
    public DbSet<ProfesionalPrestacion> ProfesionalPrestaciones { get; set; }
    public DbSet<FranjaHoraria> FranjasHorarias { get; set; }
    public DbSet<Ausencia> Ausencias { get; set; }
    public DbSet<Recurso> Recursos { get; set; }
    public DbSet<Turno> Turnos { get; set; }
    public DbSet<TurnoRecurso> TurnoRecursos { get; set; }

    // Configuración y auditoría
    public DbSet<Clinica> Clinicas { get; set; }
    public DbSet<Parametro> Parametros { get; set; }
    public DbSet<DiaNoLaborable> DiasNoLaborables { get; set; }
    public DbSet<Auditoria> Auditorias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarTablas(modelBuilder);
        CargarSemilla(modelBuilder);

        // Nada se borra en cascada: las bajas son lógicas y la historia se conserva (RN-14).
        foreach (var relacion in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relacion.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigurarTablas(ModelBuilder modelBuilder)
    {
        // --- Identidad ---
        modelBuilder.Entity<Cuenta>().HasIndex(c => c.Correo).IsUnique();
        modelBuilder.Entity<Cuenta>().HasIndex(c => new { c.TipoDocumento, c.NumeroDocumento });

        modelBuilder.Entity<Rol>().HasIndex(r => r.Nombre).IsUnique();

        modelBuilder.Entity<CuentaRol>().HasKey(cr => new { cr.CuentaId, cr.RolId });
        modelBuilder.Entity<CuentaRol>()
            .HasOne(cr => cr.Cuenta).WithMany(c => c.Roles).HasForeignKey(cr => cr.CuentaId);

        modelBuilder.Entity<CodigoCuenta>().HasIndex(c => new { c.CuentaId, c.Tipo, c.FechaCreacion });

        modelBuilder.Entity<DocumentoLegal>().HasIndex(d => new { d.Codigo, d.Version }).IsUnique();
        modelBuilder.Entity<DocumentoLegal>().Property(d => d.VigenteDesde).HasColumnType("date");

        modelBuilder.Entity<AceptacionLegal>()
            .HasOne(a => a.Cuenta).WithMany(c => c.Aceptaciones).HasForeignKey(a => a.CuentaId);
        modelBuilder.Entity<AceptacionLegal>().HasIndex(a => new { a.CuentaId, a.DocumentoLegalId }).IsUnique();

        // --- Animales ---
        modelBuilder.Entity<Especie>().HasIndex(e => e.Nombre).IsUnique();

        modelBuilder.Entity<Animal>().HasIndex(a => a.Microchip).IsUnique(); // RN-41
        modelBuilder.Entity<Animal>().Property(a => a.FechaNacimiento).HasColumnType("date");

        modelBuilder.Entity<Responsabilidad>()
            .HasOne(r => r.Cuenta).WithMany(c => c.Responsabilidades).HasForeignKey(r => r.CuentaId);
        modelBuilder.Entity<Responsabilidad>()
            .HasOne(r => r.Animal).WithMany(a => a.Responsables).HasForeignKey(r => r.AnimalId);
        modelBuilder.Entity<Responsabilidad>().HasIndex(r => new { r.CuentaId, r.AnimalId, r.Hasta });

        // --- Catálogo y agenda ---
        modelBuilder.Entity<Prestacion>().Property(p => p.PrecioDesde).HasPrecision(12, 2);

        modelBuilder.Entity<Profesional>().HasIndex(p => p.CuentaId).IsUnique();

        modelBuilder.Entity<ProfesionalEspecie>().HasKey(pe => new { pe.ProfesionalId, pe.EspecieId });
        modelBuilder.Entity<ProfesionalEspecie>()
            .HasOne(pe => pe.Profesional).WithMany(p => p.Especies).HasForeignKey(pe => pe.ProfesionalId);

        modelBuilder.Entity<ProfesionalPrestacion>().HasKey(pp => new { pp.ProfesionalId, pp.PrestacionId });
        modelBuilder.Entity<ProfesionalPrestacion>()
            .HasOne(pp => pp.Profesional).WithMany(p => p.Prestaciones).HasForeignKey(pp => pp.ProfesionalId);

        modelBuilder.Entity<FranjaHoraria>()
            .HasOne(f => f.Profesional).WithMany(p => p.Franjas).HasForeignKey(f => f.ProfesionalId);
        modelBuilder.Entity<FranjaHoraria>().HasIndex(f => new { f.ProfesionalId, f.Dia });

        modelBuilder.Entity<Ausencia>()
            .HasOne(a => a.Profesional).WithMany(p => p.Ausencias).HasForeignKey(a => a.ProfesionalId);
        modelBuilder.Entity<Ausencia>().Property(a => a.Desde).HasColumnType("date");
        modelBuilder.Entity<Ausencia>().Property(a => a.Hasta).HasColumnType("date");
        modelBuilder.Entity<Ausencia>().HasIndex(a => new { a.ProfesionalId, a.Desde, a.Hasta });

        // Índices de solapamiento de DT-BD-02: (profesional, inicio, fin), (animal, inicio, fin), (recurso, inicio, fin).
        modelBuilder.Entity<Turno>().HasIndex(t => new { t.ProfesionalId, t.Inicio, t.Fin });
        modelBuilder.Entity<Turno>().HasIndex(t => new { t.AnimalId, t.Inicio, t.Fin });
        modelBuilder.Entity<Turno>().HasIndex(t => new { t.ClienteId, t.Inicio });
        modelBuilder.Entity<Turno>().Property(t => t.Version).IsConcurrencyToken();

        modelBuilder.Entity<TurnoRecurso>().HasKey(tr => new { tr.TurnoId, tr.RecursoId });
        modelBuilder.Entity<TurnoRecurso>()
            .HasOne(tr => tr.Turno).WithMany(t => t.Recursos).HasForeignKey(tr => tr.TurnoId);
        modelBuilder.Entity<TurnoRecurso>().HasIndex(tr => new { tr.RecursoId, tr.Inicio, tr.Fin });

        // --- Configuración y auditoría ---
        modelBuilder.Entity<Parametro>().HasKey(p => p.Clave);

        modelBuilder.Entity<DiaNoLaborable>().Property(d => d.Fecha).HasColumnType("date");
        modelBuilder.Entity<DiaNoLaborable>().HasIndex(d => d.Fecha).IsUnique();

        modelBuilder.Entity<Auditoria>().HasIndex(a => new { a.Entidad, a.EntidadId });
        modelBuilder.Entity<Auditoria>().HasIndex(a => a.Fecha);
    }

    // Semilla con HasData, igual que en el curso. Mismos datos ficticios que la sección A (Blazor).
    // No se cargan cuentas: necesitan contraseña con hash y se crean desde la API.
    private static void CargarSemilla(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "Cliente" },
            new Rol { Id = 2, Nombre = "Recepcionista" },
            new Rol { Id = 3, Nombre = "Veterinario" },
            new Rol { Id = 4, Nombre = "Administrador" });

        modelBuilder.Entity<DocumentoLegal>().HasData(
            new DocumentoLegal { Id = 1, Codigo = "privacidad", Titulo = "Política de Privacidad", Version = "1.0", VigenteDesde = new DateTime(2026, 11, 2), Ruta = "privacidad" },
            new DocumentoLegal { Id = 2, Codigo = "terminos", Titulo = "Términos y Condiciones", Version = "1.0", VigenteDesde = new DateTime(2026, 11, 2), Ruta = "terminos" },
            new DocumentoLegal { Id = 3, Codigo = "cookies", Titulo = "Política de Cookies", Version = "1.0", VigenteDesde = new DateTime(2026, 11, 2), Ruta = "cookies" });

        modelBuilder.Entity<Especie>().HasData(
            new Especie { Id = 1, Nombre = "Perro" },
            new Especie { Id = 2, Nombre = "Gato" },
            new Especie { Id = 3, Nombre = "Ave" },
            new Especie { Id = 4, Nombre = "Conejo" },
            new Especie { Id = 5, Nombre = "Reptil" },
            new Especie { Id = 6, Nombre = "Otra" },
            new Especie { Id = 7, Nombre = "Vaca" },
            new Especie { Id = 8, Nombre = "Oveja" },
            new Especie { Id = 9, Nombre = "Cabra" },
            new Especie { Id = 10, Nombre = "Cerdo" },
            new Especie { Id = 11, Nombre = "Caballo" });

        modelBuilder.Entity<Prestacion>().HasData(
            new Prestacion { Id = 1, Nombre = "Consulta clínica general", Descripcion = "Revisión completa: anamnesis, examen físico, diagnóstico y tratamiento. Animales de compañía, equinos y otras especies.", PrecioDesde = 1200, ReservableEnLinea = true, DuracionMinutos = 30, Publicado = true },
            new Prestacion { Id = 2, Nombre = "Vacunación", Descripcion = "Aplicación con registro de producto, lote y fecha de refuerzo.", PrecioDesde = 600, ReservableEnLinea = true, DuracionMinutos = 15, Publicado = true },
            new Prestacion { Id = 3, Nombre = "Desparasitación", Descripcion = "Tratamiento antiparasitario indicado por el veterinario.", PrecioDesde = 450, ReservableEnLinea = true, DuracionMinutos = 15, Publicado = true },
            new Prestacion { Id = 4, Nombre = "Revisión de ovinos y bovinos", Descripcion = "Revisión individual o por grupo. Se coordina con la clínica.", PrecioDesde = 1500, ReservableEnLinea = false, DuracionMinutos = 0, Publicado = true },
            new Prestacion { Id = 5, Nombre = "Rehabilitación equina", Descripcion = "Fisioterapia para caballos. Requiere evaluación previa.", PrecioDesde = 2500, ReservableEnLinea = false, DuracionMinutos = 0, Publicado = true },
            new Prestacion { Id = 6, Nombre = "Baño y estética", Descripcion = "Baño, secado, corte y cuidado estético de perros y gatos.", PrecioDesde = 900, ReservableEnLinea = true, DuracionMinutos = 60, Publicado = true },
            new Prestacion { Id = 7, Nombre = "Guardería", Descripcion = "No publicado: no debe aparecer.", PrecioDesde = 700, ReservableEnLinea = false, DuracionMinutos = 0, Publicado = false });

        modelBuilder.Entity<Profesional>().HasData(
            new Profesional { Id = 1, Nombre = "Dra. Lucía Ferreira", Especialidad = "Clínica de pequeños animales", Descripcion = "Consultas generales, vacunación y controles de perros, gatos y otros animales de compañía.", Publicado = true, Activo = true },
            new Profesional { Id = 2, Nombre = "Dr. Martín Silveira", Especialidad = "Grandes animales", Descripcion = "Atención de bovinos, ovinos, caprinos y porcinos, revisiones de rodeo y visitas a campo.", Publicado = true, Activo = true },
            new Profesional { Id = 3, Nombre = "Dra. Valentina Acosta", Especialidad = "Equinos", Descripcion = "Clínica y rehabilitación de caballos. La rehabilitación requiere evaluación previa.", Publicado = true, Activo = true },
            new Profesional { Id = 4, Nombre = "Dr. Andrés Pereira", Especialidad = "Clínica general", Descripcion = "No publicado: no debe aparecer.", Publicado = false, Activo = true },
            new Profesional { Id = 5, Nombre = "Sofía Méndez", Especialidad = "Baño y estética", Descripcion = "Baño, corte y cuidado estético de perros y gatos. No realiza atención clínica.", Publicado = true, Activo = true });

        modelBuilder.Entity<ProfesionalEspecie>().HasData(
            // Ferreira: pequeños animales
            Esp(1, 1), Esp(1, 2), Esp(1, 3), Esp(1, 4), Esp(1, 5), Esp(1, 6),
            // Silveira: grandes animales
            Esp(2, 7), Esp(2, 8), Esp(2, 9), Esp(2, 10),
            // Acosta: caballos
            Esp(3, 11),
            // Méndez: perros y gatos
            Esp(5, 1), Esp(5, 2));

        modelBuilder.Entity<ProfesionalPrestacion>().HasData(
            Pre(1, 1), Pre(1, 2), Pre(1, 3),
            Pre(2, 1), Pre(2, 2), Pre(2, 3),
            Pre(3, 1),
            Pre(5, 6));

        // Jornadas y descansos: los mismos de ProfesionalService en Blazor.
        var lunesAViernes = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var sabado = new[] { DayOfWeek.Saturday };
        var martesYJueves = new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday };

        var franjas = new List<FranjaHoraria>();
        void Agregar(int profesionalId, string tipo, DayOfWeek[] dias, int hIni, int mIni, int hFin, int mFin)
        {
            foreach (var dia in dias)
            {
                franjas.Add(new FranjaHoraria
                {
                    Id = franjas.Count + 1,
                    ProfesionalId = profesionalId,
                    Tipo = tipo,
                    Dia = dia,
                    Inicio = new TimeSpan(hIni, mIni, 0),
                    Fin = new TimeSpan(hFin, mFin, 0)
                });
            }
        }

        Agregar(1, "Jornada", lunesAViernes, 9, 0, 14, 0);
        Agregar(1, "Descanso", lunesAViernes, 11, 30, 12, 0);
        Agregar(2, "Jornada", lunesAViernes, 14, 0, 19, 0);
        Agregar(2, "Jornada", sabado, 9, 0, 13, 0);
        Agregar(2, "Descanso", lunesAViernes, 16, 30, 17, 0);
        Agregar(3, "Jornada", martesYJueves, 9, 0, 17, 0);
        Agregar(3, "Descanso", martesYJueves, 12, 30, 13, 30);
        Agregar(5, "Jornada", lunesAViernes, 10, 0, 18, 0);
        Agregar(5, "Jornada", sabado, 9, 0, 13, 0);
        Agregar(5, "Descanso", lunesAViernes, 13, 0, 14, 0);

        modelBuilder.Entity<FranjaHoraria>().HasData(franjas);

        // En Blazor las ausencias son relativas a hoy; en la semilla tienen que ser fechas fijas.
        modelBuilder.Entity<Ausencia>().HasData(
            new Ausencia { Id = 1, ProfesionalId = 1, Desde = new DateTime(2026, 11, 18), Hasta = new DateTime(2026, 11, 18), Motivo = "Capacitación" },
            new Ausencia { Id = 2, ProfesionalId = 2, Desde = new DateTime(2026, 11, 28), Hasta = new DateTime(2026, 12, 2), Motivo = "Licencia" });

        modelBuilder.Entity<Recurso>().HasData(
            new Recurso { Id = 1, Nombre = "Consultorio 1", Tipo = "Consultorio", Activo = true },
            new Recurso { Id = 2, Nombre = "Consultorio 2", Tipo = "Consultorio", Activo = true },
            new Recurso { Id = 3, Nombre = "Sala de estética", Tipo = "Sala", Activo = true },
            new Recurso { Id = 4, Nombre = "Corral de grandes animales", Tipo = "Corral", Activo = true });

        modelBuilder.Entity<Clinica>().HasData(new Clinica
        {
            Id = 1,
            Nombre = "VITALIA",
            RazonSocial = "VITALIA Servicios Veterinarios S.A.S.",
            Lema = "Clínica veterinaria para animales de compañía y de campo",
            Presentacion = "Atendemos perros, gatos, caballos, ovinos y bovinos. Trabajamos en la clínica y, cuando hace falta, vamos a tu casa o a tu campo.",
            Direccion = "Calle 18 de Julio 1234",
            Ciudad = "Nueva Palmira, Colonia",
            Telefono = "+598 4544 0000",
            TelefonoGuardia = "+598 99 000 000",
            Correo = "contacto@vitalia.uy",
            Horario = "Lunes a viernes de 9 a 19 h · Sábados de 9 a 13 h",
            Cobertura = "Asistencia a domicilio hasta 30 km desde la clínica."
        });

        // Valores por defecto de P-RC-02, D-REG-02 y las decisiones de Eric del 2026-10-31.
        modelBuilder.Entity<Parametro>().HasData(
            new Parametro { Clave = "agenda.anticipacion_minima_horas", Valor = "2", Descripcion = "Horas mínimas entre la reserva y el turno." },
            new Parametro { Clave = "agenda.horizonte_dias", Valor = "60", Descripcion = "Días hacia adelante en que se puede reservar." },
            new Parametro { Clave = "agenda.intervalo_inicio_minutos", Valor = "15", Descripcion = "Cada cuántos minutos puede empezar un turno." },
            new Parametro { Clave = "agenda.cancelacion_sin_costo_horas", Valor = "24", Descripcion = "Horas antes del turno hasta las que se cancela sin costo." },
            new Parametro { Clave = "registro.codigo_vigencia_minutos", Valor = "15", Descripcion = "Minutos de validez del código enviado por correo." },
            new Parametro { Clave = "registro.codigo_intentos", Valor = "5", Descripcion = "Intentos por código." },
            new Parametro { Clave = "registro.reenvios_por_hora", Valor = "3", Descripcion = "Reenvíos de código permitidos por hora." },
            new Parametro { Clave = "registro.dias_sin_verificar", Valor = "7", Descripcion = "Días tras los que se elimina una cuenta no verificada." },
            new Parametro { Clave = "avisos.refuerzo_dias_antes", Valor = "5", Descripcion = "Días de anticipación del aviso de refuerzo de vacunas y desparasitaciones." },
            new Parametro { Clave = "asistencia.cobertura_km", Valor = "30", Descripcion = "Zona de cobertura desde la clínica." },
            new Parametro { Clave = "asistencia.escalado_rojo_minutos", Valor = "10", Descripcion = "Espera de aceptación de un rojo antes de escalar a Recepción." });

        // Feriados que ya usa AgendaService en Blazor.
        modelBuilder.Entity<DiaNoLaborable>().HasData(
            new DiaNoLaborable { Id = 1, Fecha = new DateTime(2026, 12, 25), Motivo = "Navidad" },
            new DiaNoLaborable { Id = 2, Fecha = new DateTime(2027, 1, 1), Motivo = "Año Nuevo" },
            new DiaNoLaborable { Id = 3, Fecha = new DateTime(2027, 1, 6), Motivo = "Día de Reyes" });
    }

    private static ProfesionalEspecie Esp(int profesionalId, int especieId)
        => new() { ProfesionalId = profesionalId, EspecieId = especieId };

    private static ProfesionalPrestacion Pre(int profesionalId, int prestacionId)
        => new() { ProfesionalId = profesionalId, PrestacionId = prestacionId };
}
