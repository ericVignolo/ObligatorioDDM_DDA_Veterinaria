using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Data;

// Segunda parte de la base (migración ModulosClinicos, Diego 2026-11-19): consulta e historia clínica,
// consentimientos, vacunas, inventario, cobro, internación, asistencia y comunicaciones.
// Se llama desde AppDbContext.OnModelCreating. Semilla con los mismos datos de demostración que Blazor.
public static class ConfiguracionModulos
{
    // Contraseña de demostración del personal: «Vitalia2026!» (la misma que PersonalService en Blazor).
    // PBKDF2-SHA256, 100 000 iteraciones, sal fija solo para la semilla. Formato "sal.derivado" en Base64.
    public const string HashContrasenaDemo = "Vml0YWxpYVNlbWlsbGEwMQ==.rmvKwlUzU6PwWHiDTL50BHoRzzzOXikAepJ8q13h22k=";

    public static void Configurar(ModelBuilder modelBuilder)
    {
        // --- Consulta e historia clínica ---
        modelBuilder.Entity<Consulta>().HasIndex(c => c.TurnoId).IsUnique();
        modelBuilder.Entity<Consulta>().HasIndex(c => new { c.AnimalId, c.FechaInicio });
        modelBuilder.Entity<Consulta>().Property(c => c.Version).IsConcurrencyToken();
        modelBuilder.Entity<Consulta>().Property(c => c.TemperaturaC).HasPrecision(4, 1);
        modelBuilder.Entity<Consulta>().Property(c => c.PesoKg).HasPrecision(8, 2);

        modelBuilder.Entity<Enmienda>()
            .HasOne(e => e.Consulta).WithMany(c => c.Enmiendas).HasForeignKey(e => e.ConsultaId);

        modelBuilder.Entity<LecturaHistoria>().HasIndex(l => new { l.AnimalId, l.Fecha });

        modelBuilder.Entity<DocumentoConsentimiento>().HasIndex(d => new { d.PrestacionId, d.Version }).IsUnique();
        modelBuilder.Entity<DocumentoConsentimiento>().Property(d => d.FechaVigencia).HasColumnType("date");
        modelBuilder.Entity<Consentimiento>().HasIndex(c => new { c.TurnoId, c.Fecha });

        // --- Vacunas e inventario ---
        modelBuilder.Entity<Articulo>().HasIndex(a => a.Nombre).IsUnique();

        modelBuilder.Entity<LoteProducto>().ToTable("Lotes", t => t.HasCheckConstraint("CK_Lotes_CantidadNoNegativa", "`Cantidad` >= 0")); // RN-26
        modelBuilder.Entity<LoteProducto>().Property(l => l.Vencimiento).HasColumnType("date");
        modelBuilder.Entity<LoteProducto>().HasIndex(l => new { l.ArticuloId, l.Numero }).IsUnique();
        modelBuilder.Entity<LoteProducto>().HasIndex(l => new { l.ArticuloId, l.Inhabilitado, l.Vencimiento }); // FEFO (RN-27)

        modelBuilder.Entity<MovimientoStock>().HasIndex(m => new { m.ArticuloId, m.Fecha });
        modelBuilder.Entity<ReservaStock>().HasIndex(r => new { r.LoteId, r.Confirmada });
        modelBuilder.Entity<AlertaStock>().HasIndex(a => new { a.Atendida, a.Fecha });

        modelBuilder.Entity<ProductoSanitario>().HasIndex(p => p.ArticuloId).IsUnique();
        modelBuilder.Entity<ProductoSanitarioEspecie>().HasKey(pe => new { pe.ProductoSanitarioId, pe.EspecieId });
        modelBuilder.Entity<ProductoSanitarioEspecie>()
            .HasOne(pe => pe.ProductoSanitario).WithMany(p => p.Especies).HasForeignKey(pe => pe.ProductoSanitarioId);

        modelBuilder.Entity<AplicacionSanitaria>().HasIndex(a => new { a.AnimalId, a.ProductoSanitarioId, a.FechaAplicacion });
        modelBuilder.Entity<AplicacionSanitaria>().HasIndex(a => a.FechaRefuerzo); // avisos de refuerzo
        modelBuilder.Entity<AplicacionSanitaria>().Property(a => a.LoteVence).HasColumnType("date");
        modelBuilder.Entity<AplicacionSanitaria>().Property(a => a.FechaAplicacion).HasColumnType("date");
        modelBuilder.Entity<AplicacionSanitaria>().Property(a => a.FechaRefuerzo).HasColumnType("date");

        // --- Cobro ---
        modelBuilder.Entity<TarifaPrestacion>().HasIndex(t => new { t.PrestacionId, t.VigenteDesde }).IsUnique();
        modelBuilder.Entity<TarifaPrestacion>().Property(t => t.VigenteDesde).HasColumnType("date");
        modelBuilder.Entity<TarifaPrestacion>().Property(t => t.Precio).HasPrecision(12, 2);

        modelBuilder.Entity<Cargo>().HasIndex(c => c.ConsultaId).IsUnique(); // RN-32: un cargo por consulta
        modelBuilder.Entity<Cargo>().HasIndex(c => c.SolicitudAsistenciaId).IsUnique();
        modelBuilder.Entity<Cargo>().HasIndex(c => new { c.ClienteId, c.Fecha });
        modelBuilder.Entity<Cargo>().Property(c => c.Monto).HasPrecision(12, 2);

        modelBuilder.Entity<Pago>().ToTable("Pagos", t => t.HasCheckConstraint("CK_Pagos_MontoPositivo", "`Monto` > 0"));
        modelBuilder.Entity<Pago>().HasIndex(p => p.Numero).IsUnique();
        modelBuilder.Entity<Pago>().HasIndex(p => p.CargoId);
        modelBuilder.Entity<Pago>().Property(p => p.Monto).HasPrecision(12, 2);

        modelBuilder.Entity<NotaCredito>().HasIndex(n => n.Numero).IsUnique();
        modelBuilder.Entity<NotaCredito>().HasIndex(n => n.PagoId).IsUnique();
        modelBuilder.Entity<NotaCredito>().Property(n => n.Monto).HasPrecision(12, 2);

        modelBuilder.Entity<Numerador>().HasKey(n => n.Clave);

        // --- Internación ---
        modelBuilder.Entity<Box>().HasIndex(b => b.Nombre).IsUnique();
        modelBuilder.Entity<Internacion>().HasIndex(i => new { i.AnimalId, i.Estado }); // RN-21
        modelBuilder.Entity<Internacion>().HasIndex(i => new { i.BoxId, i.Estado });    // RN-22
        modelBuilder.Entity<Internacion>().Property(i => i.Version).IsConcurrencyToken();
        modelBuilder.Entity<NovedadInternacion>()
            .HasOne(n => n.Internacion).WithMany(i => i.Novedades).HasForeignKey(n => n.InternacionId);
        modelBuilder.Entity<NovedadInternacion>()
            .HasOne(n => n.Rectifica).WithMany().HasForeignKey(n => n.RectificaId);

        // --- Asistencia ---
        modelBuilder.Entity<SolicitudAsistencia>().HasIndex(s => new { s.ClienteId, s.ClaveIdempotencia }).IsUnique();
        modelBuilder.Entity<SolicitudAsistencia>().HasIndex(s => new { s.Estado, s.Nivel, s.FechaCreacion }); // cola por prioridad
        modelBuilder.Entity<SolicitudAsistencia>().Property(s => s.Costo).HasPrecision(12, 2);
        modelBuilder.Entity<SolicitudAsistencia>().Property(s => s.Latitud).HasPrecision(9, 6);
        modelBuilder.Entity<SolicitudAsistencia>().Property(s => s.Longitud).HasPrecision(9, 6);
        modelBuilder.Entity<SolicitudAsistencia>().Property(s => s.Version).IsConcurrencyToken();
        modelBuilder.Entity<SolicitudAsistencia>()
            .HasOne(s => s.Veterinario).WithMany().HasForeignKey(s => s.VeterinarioId);
        modelBuilder.Entity<SolicitudAsistencia>()
            .HasOne(s => s.Cliente).WithMany().HasForeignKey(s => s.ClienteId);

        modelBuilder.Entity<OfertaAsistencia>().HasKey(o => new { o.SolicitudAsistenciaId, o.VeterinarioId });
        modelBuilder.Entity<OfertaAsistencia>()
            .HasOne(o => o.SolicitudAsistencia).WithMany(s => s.Ofertas).HasForeignKey(o => o.SolicitudAsistenciaId);

        modelBuilder.Entity<EventoAsistencia>()
            .HasOne(e => e.SolicitudAsistencia).WithMany(s => s.Eventos).HasForeignKey(e => e.SolicitudAsistenciaId);

        // --- Comunicaciones y derechos ---
        modelBuilder.Entity<TipoComunicacion>().HasIndex(t => t.Nombre).IsUnique();
        modelBuilder.Entity<PreferenciaComunicacion>().HasKey(p => new { p.CuentaId, p.TipoComunicacionId });
        modelBuilder.Entity<Comunicacion>().HasIndex(c => new { c.CuentaId, c.TipoComunicacionId, c.Clave }).IsUnique();
        modelBuilder.Entity<Comunicacion>().HasIndex(c => new { c.Estado, c.FechaCreacion }); // cola de envío
        modelBuilder.Entity<Comunicacion>()
            .HasOne(c => c.Cuenta).WithMany().HasForeignKey(c => c.CuentaId);
        modelBuilder.Entity<Comunicacion>()
            .HasOne(c => c.GeneradaPor).WithMany().HasForeignKey(c => c.GeneradaPorId);
        modelBuilder.Entity<Comunicacion>()
            .HasOne(c => c.ReenvioDe).WithMany().HasForeignKey(c => c.ReenvioDeId);
        modelBuilder.Entity<SolicitudDerecho>().HasIndex(s => new { s.CuentaId, s.Estado });
        modelBuilder.Entity<SolicitudDerecho>()
            .HasOne(s => s.Cuenta).WithMany().HasForeignKey(s => s.CuentaId);
        modelBuilder.Entity<SolicitudDerecho>()
            .HasOne(s => s.ResueltaPor).WithMany().HasForeignKey(s => s.ResueltaPorId);
    }

    public static void CargarSemilla(ModelBuilder modelBuilder)
    {
        var alta = new DateTime(2026, 11, 1);

        // Personal de demostración (PersonalService en Blazor). Contraseña: Vitalia2026!
        modelBuilder.Entity<Cuenta>().HasData(
            Personal(1, "Laura", "Benítez", "recepcion@vitalia.uy", alta),
            Personal(2, "Lucía", "Ferreira", "ferreira@vitalia.uy", alta),
            Personal(3, "Martín", "Silveira", "silveira@vitalia.uy", alta),
            Personal(4, "Valentina", "Acosta", "acosta@vitalia.uy", alta),
            Personal(5, "Gonzalo", "Rivas", "admin@vitalia.uy", alta));

        // Roles: 2 Recepcionista, 3 Veterinario, 4 Administrador.
        modelBuilder.Entity<CuentaRol>().HasData(
            new CuentaRol { CuentaId = 1, RolId = 2 },
            new CuentaRol { CuentaId = 2, RolId = 3 },
            new CuentaRol { CuentaId = 3, RolId = 3 },
            new CuentaRol { CuentaId = 4, RolId = 3 },
            new CuentaRol { CuentaId = 5, RolId = 4 });

        modelBuilder.Entity<Box>().HasData(
            new Box { Id = 1, Nombre = "Box 1", Tipo = "Pequeños animales", Activo = true },
            new Box { Id = 2, Nombre = "Box 2", Tipo = "Pequeños animales", Activo = true },
            new Box { Id = 3, Nombre = "Box 3", Tipo = "Pequeños animales", Activo = true },
            new Box { Id = 4, Nombre = "Box equino", Tipo = "Grandes animales", Activo = true });

        // Inventario: 1 a 7 son los productos sanitarios; del 8 en adelante, insumos (InventarioService).
        modelBuilder.Entity<Articulo>().HasData(
            new Articulo { Id = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 5, Activo = true },
            new Articulo { Id = 2, Nombre = "Vacuna antirrábica", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 5, Activo = true },
            new Articulo { Id = 3, Nombre = "Vacuna triple felina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 4, Activo = true },
            new Articulo { Id = 4, Nombre = "Vacuna antitetánica equina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 3, Activo = true },
            new Articulo { Id = 5, Nombre = "Vacuna clostridial", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 20, Activo = true },
            new Articulo { Id = 6, Nombre = "Antiparasitario interno", Tipo = "Antiparasitario", Unidad = "dosis", StockMinimo = 5, Activo = true },
            new Articulo { Id = 7, Nombre = "Antiparasitario externo", Tipo = "Antiparasitario", Unidad = "dosis", StockMinimo = 5, Activo = true },
            new Articulo { Id = 8, Nombre = "Jeringa descartable 3 ml", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 20, Activo = true },
            new Articulo { Id = 9, Nombre = "Guantes de examen", Tipo = "Insumo", Unidad = "par", StockMinimo = 10, Activo = true },
            new Articulo { Id = 10, Nombre = "Gasa estéril", Tipo = "Insumo", Unidad = "sobre", StockMinimo = 10, Activo = true },
            new Articulo { Id = 11, Nombre = "Suero fisiológico 500 ml", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 4, Activo = true },
            new Articulo { Id = 12, Nombre = "Venda elástica", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 5, Activo = true });

        // Catálogo sanitario (VacunaService). Intervalos de demostración, a validar por un veterinario.
        modelBuilder.Entity<ProductoSanitario>().HasData(
            new ProductoSanitario { Id = 1, ArticuloId = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Norte", IntervaloRefuerzoDias = 365, IntervaloMinimoDias = 330 },
            new ProductoSanitario { Id = 2, ArticuloId = 2, Nombre = "Vacuna antirrábica", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Sur", IntervaloRefuerzoDias = 365, IntervaloMinimoDias = 330 },
            new ProductoSanitario { Id = 3, ArticuloId = 3, Nombre = "Vacuna triple felina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Norte", IntervaloRefuerzoDias = 365, IntervaloMinimoDias = 330 },
            new ProductoSanitario { Id = 4, ArticuloId = 4, Nombre = "Vacuna antitetánica equina", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Campo", IntervaloRefuerzoDias = 365, IntervaloMinimoDias = 330 },
            new ProductoSanitario { Id = 5, ArticuloId = 5, Nombre = "Vacuna clostridial", Tipo = "Vacuna", Laboratorio = "Laboratorio Demo Campo", IntervaloRefuerzoDias = 180, IntervaloMinimoDias = 150 },
            new ProductoSanitario { Id = 6, ArticuloId = 6, Nombre = "Antiparasitario interno", Tipo = "Antiparasitario", Laboratorio = "Laboratorio Demo Sur", IntervaloRefuerzoDias = 90, IntervaloMinimoDias = 75 },
            new ProductoSanitario { Id = 7, ArticuloId = 7, Nombre = "Antiparasitario externo", Tipo = "Antiparasitario", Laboratorio = "Laboratorio Demo Sur", IntervaloRefuerzoDias = 30, IntervaloMinimoDias = 25 });

        // Especies (Id): 1 Perro, 2 Gato, 7 Vaca, 8 Oveja, 9 Cabra, 11 Caballo.
        modelBuilder.Entity<ProductoSanitarioEspecie>().HasData(
            PE(1, 1), PE(2, 1), PE(2, 2), PE(3, 2), PE(4, 11), PE(5, 7), PE(5, 8), PE(5, 9), PE(6, 1), PE(6, 2), PE(7, 1), PE(7, 2));

        // Lotes con fechas fijas (en Blazor son relativas a hoy). Cada vacuna tiene uno que vence pronto (FEFO lo usa primero)
        // y uno lejano. La antirrábica arranca debajo del mínimo y el antiparasitario externo justo encima, como en Blazor.
        var ingreso = new DateTime(2026, 10, 10);
        var lotes = new List<LoteProducto>();
        void Lote(int articuloId, string numero, DateTime vence, int cantidad)
            => lotes.Add(new LoteProducto { Id = lotes.Count + 1, ArticuloId = articuloId, Numero = numero, Vencimiento = vence, Cantidad = cantidad, FechaIngreso = ingreso });

        for (var articulo = 1; articulo <= 7; articulo++)
        {
            Lote(articulo, $"L{articulo}-A", new DateTime(2026, 12, 10), 3);
            Lote(articulo, $"L{articulo}-B", new DateTime(2027, 9, 30), articulo == 2 ? 1 : articulo == 7 ? 3 : 10);
        }

        Lote(8, "JER-2611", new DateTime(2029, 11, 30), 120);
        Lote(9, "GUA-2610", new DateTime(2028, 11, 30), 40);
        Lote(10, "GAS-2609", new DateTime(2026, 12, 15), 8);
        Lote(10, "GAS-2611", new DateTime(2027, 11, 30), 30);
        Lote(11, "SUE-2608", new DateTime(2027, 7, 31), 6);
        Lote(12, "VEN-2605", new DateTime(2028, 11, 30), 15);

        modelBuilder.Entity<LoteProducto>().HasData(lotes);

        // Cada lote entra con su movimiento de compra (RN-30: la cantidad solo cambia con un movimiento).
        modelBuilder.Entity<MovimientoStock>().HasData(lotes.Select(l => new MovimientoStock
        {
            Id = l.Id,
            ArticuloId = l.ArticuloId,
            LoteId = l.Id,
            Tipo = "Compra",
            Cantidad = l.Cantidad,
            Origen = "Carga inicial",
            Motivo = "Carga inicial de demostración",
            Fecha = ingreso
        }));

        // Tarifa inicial de cada prestación = su precio de referencia (PrestacionService en Blazor).
        modelBuilder.Entity<TarifaPrestacion>().HasData(
            Tarifa(1, 1, 1200), Tarifa(2, 2, 600), Tarifa(3, 3, 450), Tarifa(4, 4, 1500),
            Tarifa(5, 5, 2500), Tarifa(6, 6, 900), Tarifa(7, 7, 700), Tarifa(8, 8, 4500));

        modelBuilder.Entity<Numerador>().HasData(
            new Numerador { Clave = "Comprobante", Ultimo = 0 },
            new Numerador { Clave = "NotaCredito", Ultimo = 0 });

        modelBuilder.Entity<DocumentoConsentimiento>().HasData(new DocumentoConsentimiento
        {
            Id = 1,
            PrestacionId = 8,
            Version = "1.0",
            Titulo = "Consentimiento informado para castración con anestesia general",
            Texto = "Autorizo al equipo veterinario de VITALIA a realizar la castración del animal indicado, bajo anestesia general. "
                + "Me explicaron en qué consiste el procedimiento, los cuidados previos (ayuno de 8 horas de comida y 2 de agua) y los posteriores. "
                + "Entiendo que toda anestesia y cirugía tienen riesgos, poco frecuentes pero posibles: reacciones a los fármacos, sangrado, "
                + "infección de la herida y, excepcionalmente, complicaciones graves o la muerte. "
                + "Autorizo al veterinario a tomar las medidas necesarias si surge una complicación durante la cirugía y me comprometo a "
                + "informar cualquier enfermedad, medicación o alergia del animal. Puedo hacer preguntas antes de firmar y retirar este "
                + "consentimiento avisando a la clínica antes del procedimiento.",
            FechaVigencia = alta,
            Vigente = true
        });

        // Tipos de comunicación (ComunicacionesService). Canales: "Correo", "Solo en la app", "No recibir".
        modelBuilder.Entity<TipoComunicacion>().HasData(
            new TipoComunicacion { Id = 1, Nombre = "Resumen clínico", Descripcion = "Resumen de la consulta cuando el veterinario la firma (RF-CLI-10).", Esencial = true, CanalPorDefecto = "Correo" },
            new TipoComunicacion { Id = 2, Nombre = "Comprobante de cobro", Descripcion = "Comprobante de un cobro, cuando Recepción lo envía.", Esencial = true, CanalPorDefecto = "Correo" },
            new TipoComunicacion { Id = 3, Nombre = "Aviso de la clínica", Descripcion = "Cambios que afectan tu atención: cierres, horarios, feriados.", Esencial = true, CanalPorDefecto = "Correo" },
            new TipoComunicacion { Id = 4, Nombre = "Aviso de refuerzo", Descripcion = "Vacunas y desparasitaciones próximas a vencer de tus animales (RF-CLI-07).", Esencial = false, CanalPorDefecto = "Correo" },
            new TipoComunicacion { Id = 5, Nombre = "Alerta sanitaria", Descripcion = "Alertas de la clínica para animales de tu misma especie, como un brote en la zona.", Esencial = false, CanalPorDefecto = "No recibir" });

        // Política de conservación (DerechosService).
        modelBuilder.Entity<PoliticaConservacion>().HasData(
            new PoliticaConservacion { Id = 1, Categoria = "Datos de la cuenta (nombre, contacto, documento, domicilio)", Finalidad = "Identificarte, contactarte y atender tus animales.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se reemplazan por valores neutros." },
            new PoliticaConservacion { Id = 2, Categoria = "Animales e historia clínica", Finalidad = "Continuidad de la atención veterinaria y obligaciones sanitarias.", Acceso = "Vos (solo lectura), el personal clínico y el Administrador; toda lectura queda auditada.", Conservacion = "El plazo que fije la normativa veterinaria aplicable (a confirmar en la revisión legal previa a producción).", AlEliminar = "Se conserva, sin identificar al titular." },
            new PoliticaConservacion { Id = 3, Categoria = "Consentimientos informados", Finalidad = "Constancia de tu decisión antes de un procedimiento.", Acceso = "Vos, el personal clínico y el Administrador.", Conservacion = "Junto con la historia clínica.", AlEliminar = "Se conservan, sin identificar al titular." },
            new PoliticaConservacion { Id = 4, Categoria = "Cargos, pagos y comprobantes", Finalidad = "Registro de lo cobrado en la clínica.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "El plazo que fijen las obligaciones contables y tributarias aplicables (a confirmar).", AlEliminar = "Se conservan, sin identificar al titular." },
            new PoliticaConservacion { Id = 5, Categoria = "Comunicaciones enviadas y preferencias", Finalidad = "Entregarte resúmenes, avisos y comprobantes por el canal que elegiste.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se borran." },
            new PoliticaConservacion { Id = 6, Categoria = "Ubicación en una asistencia a domicilio", Finalidad = "Despachar al profesional durante la asistencia (RNF-LEG-09).", Acceso = "El personal asignado.", Conservacion = "Se deja de usar al terminar la asistencia.", AlEliminar = "No aplica: no se conserva el seguimiento." },
            new PoliticaConservacion { Id = 7, Categoria = "Aceptaciones de términos y privacidad", Finalidad = "Probar qué versión aceptaste y cuándo (RN-43).", Acceso = "El Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se reemplazan junto con la cuenta." });
    }

    private static Cuenta Personal(int id, string nombre, string apellido, string correo, DateTime alta) => new()
    {
        Id = id,
        Nombre = nombre,
        Apellido = apellido,
        Correo = correo,
        HashContrasena = HashContrasenaDemo,
        Verificada = true,
        Activa = true,
        FechaCreacion = alta
    };

    private static ProductoSanitarioEspecie PE(int productoId, int especieId)
        => new() { ProductoSanitarioId = productoId, EspecieId = especieId };

    private static TarifaPrestacion Tarifa(int id, int prestacionId, decimal precio)
        => new() { Id = id, PrestacionId = prestacionId, Precio = precio, VigenteDesde = new DateTime(2026, 11, 1) };
}
