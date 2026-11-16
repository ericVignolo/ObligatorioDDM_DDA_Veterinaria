using System.Text.RegularExpressions;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra del consentimiento informado (A-025, CU-CLI-10, RF-CLI-11, RN-10, RN-18, RN-43).
// Escritas solo a partir de ESPECIFICACION_CONSENTIMIENTO.md, los modelos y las firmas públicas.
public class ConsentimientoCajaNegraTests
{
    private const int Castracion = 8;
    private const int Ferreira = 1;

    // Castración futura (Confirmado) del perro de Juana Pérez con la Dra. Ferreira.
    private static Turno ReservarCastracion(Fabrica f, out Cuenta cliente, out Animal perro)
    {
        cliente = f.ClienteVerificado();
        perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var resultado = f.Agenda.Reservar(cliente.Id, perro.Id, Castracion, Ferreira, Fabrica.A(f.DiaQueAtiende(Ferreira), 9), out var turno);
        Assert.True(resultado.Exito);
        Assert.NotNull(turno);
        return turno!;
    }

    private static ConsentimientoFormulario FirmaValida(string nombre = "Juana Pérez")
    {
        return new ConsentimientoFormulario { LeyoYAcepta = true, NombreCompleto = nombre };
    }

    private static Personal Persona(Fabrica f, string correo)
    {
        return f.PersonalDe(correo);
    }

    // ---------- Punto de partida ----------

    [Fact]
    public void Castracion_requiere_consentimiento_y_arranca_pendiente()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out _, out _);

        Assert.True(f.Consentimientos.Requiere(turno));
        Assert.Equal("Pendiente", f.Consentimientos.Estado(turno));
        Assert.False(f.Consentimientos.EsValido(turno.Id));
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Null(f.Consentimientos.UltimaRespuesta(turno.Id));
    }

    [Fact]
    public void Documento_vigente_de_castracion_es_la_version_1_0()
    {
        var f = new Fabrica();
        var documento = f.Consentimientos.DocumentoVigente(Castracion);

        Assert.NotNull(documento);
        Assert.Equal("1.0", documento!.Version);
        Assert.Equal(Castracion, documento.PrestacionId);
        Assert.True(documento.Vigente);
        Assert.NotNull(f.Consentimientos.ObtenerDocumento(documento.Id));
    }

    [Fact]
    public void Formulario_nuevo_no_viene_con_la_casilla_marcada()
    {
        var formulario = new ConsentimientoFormulario();

        Assert.False(formulario.LeyoYAcepta);
        Assert.Equal("", formulario.NombreCompleto);
    }

    // ---------- K-03 / K-04: firmar ----------

    [Fact]
    public void Firmar_con_casilla_y_nombre_correcto_queda_firmado()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente);

        Assert.True(resultado.Exito);
        Assert.Equal("Firmado", f.Consentimientos.Estado(turno));
        Assert.True(f.Consentimientos.EsValido(turno.Id));
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Fact]
    public void Firma_guarda_todos_los_datos_y_huella_sha256()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out var perro);
        var documento = f.Consentimientos.DocumentoVigente(Castracion)!;

        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        var firma = f.Consentimientos.UltimaRespuesta(turno.Id);

        Assert.NotNull(firma);
        Assert.Equal(turno.Id, firma!.TurnoId);
        Assert.Equal(cliente.Id, firma.ClienteId);
        Assert.Equal(perro.Id, firma.AnimalId);
        Assert.Equal(Castracion, firma.PrestacionId);
        Assert.Equal(documento.Id, firma.DocumentoId);
        Assert.Equal("1.0", firma.Version);
        Assert.Equal("Aceptado", firma.Respuesta);
        Assert.False(string.IsNullOrWhiteSpace(firma.NombreEscrito));
        Assert.Equal("En línea", firma.Canal);
        Assert.NotEqual(default, firma.Fecha);
        Assert.Matches(new Regex("^[0-9a-fA-F]{64}$"), firma.Hash);
    }

    [Fact]
    public void Firmar_sin_marcar_la_casilla_falla_y_no_guarda_nada()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        var datos = new ConsentimientoFormulario { LeyoYAcepta = false, NombreCompleto = "Juana Pérez" };

        var resultado = f.Consentimientos.Firmar(turno.Id, datos, cliente);

        Assert.False(resultado.Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal("Pendiente", f.Consentimientos.Estado(turno));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Juana")]
    [InlineData("Pérez")]
    [InlineData("Juana Gómez")]
    [InlineData("Ana Rodríguez")]
    [InlineData("Pérez Juana")]
    public void Firmar_con_nombre_vacio_o_distinto_al_de_la_cuenta_falla(string nombre)
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(nombre), cliente);

        Assert.False(resultado.Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal("Pendiente", f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void Firmar_con_nombre_de_121_caracteres_falla()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(new string('a', 121)), cliente);

        Assert.False(resultado.Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Theory]
    [InlineData("JUANA PÉREZ")]
    [InlineData("juana pérez")]
    [InlineData("  Juana Pérez  ")]
    [InlineData("Juana   Pérez")]
    [InlineData("  jUaNa    pÉrEz ")]
    public void Firmar_ignora_mayusculas_y_espacios_de_mas(string nombre)
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(nombre), cliente);

        Assert.True(resultado.Exito);
        Assert.Equal("Firmado", f.Consentimientos.Estado(turno));
    }

    // ---------- K-01: solo el cliente responsable ----------

    [Fact]
    public void Firmar_y_rechazar_sin_sesion_fallan()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out _, out _);

        Assert.False(f.Consentimientos.Firmar(turno.Id, FirmaValida(), null).Exito);
        Assert.False(f.Consentimientos.Rechazar(turno.Id, null).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal("Pendiente", f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void Otro_cliente_no_puede_firmar_ni_rechazar_el_turno_ajeno()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out _, out _);
        var ana = f.Cuentas.ObtenerPorId(100)!;
        var firmaDeAna = FirmaValida(ana.Nombre + " " + ana.Apellido);

        Assert.False(f.Consentimientos.Firmar(turno.Id, firmaDeAna, ana).Exito);
        Assert.False(f.Consentimientos.Rechazar(turno.Id, ana).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal("Pendiente", f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void Firmar_y_rechazar_un_turno_inexistente_fallan()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();

        Assert.False(f.Consentimientos.Firmar(999999, FirmaValida(), cliente).Exito);
        Assert.False(f.Consentimientos.Rechazar(999999, cliente).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(999999));
    }

    // ---------- K-02: estado del turno ----------

    [Fact]
    public void Turno_en_espera_admite_la_firma()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.CirugiaEnEspera(cliente.Id, perro.Id);
        Assert.Equal("En espera", turno.Estado);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente);

        Assert.True(resultado.Exito);
        Assert.True(f.Consentimientos.EsValido(turno.Id));
    }

    [Theory]
    [InlineData("Cancelado")]
    [InlineData("Ausente")]
    [InlineData("Finalizado")]
    [InlineData("En curso")]
    public void Turno_en_estado_no_admitido_no_se_firma_ni_se_rechaza(string estado)
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        f.Agenda.ObtenerPorId(turno.Id)!.Estado = estado;

        Assert.False(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        Assert.False(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
    }

    // ---------- K-05: rechazo e inmutabilidad ----------

    [Fact]
    public void Rechazar_deja_el_consentimiento_rechazado()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        var resultado = f.Consentimientos.Rechazar(turno.Id, cliente);

        Assert.True(resultado.Exito);
        Assert.Equal("Rechazado", f.Consentimientos.Estado(turno));
        Assert.False(f.Consentimientos.EsValido(turno.Id));
        Assert.Equal("Rechazado", f.Consentimientos.UltimaRespuesta(turno.Id)!.Respuesta);
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Fact]
    public void No_se_rechaza_dos_veces_seguidas()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);

        var segundo = f.Consentimientos.Rechazar(turno.Id, cliente);

        Assert.False(segundo.Exito);
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal("Rechazado", f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void No_se_firma_dos_veces_lo_ya_firmado()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        var hashOriginal = f.Consentimientos.UltimaRespuesta(turno.Id)!.Hash;

        var segunda = f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente);

        Assert.False(segunda.Exito);
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.Equal(hashOriginal, f.Consentimientos.UltimaRespuesta(turno.Id)!.Hash);
    }

    [Fact]
    public void Firmar_despues_de_rechazar_agrega_un_registro_y_conserva_el_rechazo()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);
        var rechazo = f.Consentimientos.UltimaRespuesta(turno.Id)!;
        var idRechazo = rechazo.Id;

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente);

        Assert.True(resultado.Exito);
        var respuestas = f.Consentimientos.RespuestasDe(turno.Id);
        Assert.Equal(2, respuestas.Count);
        var rechazoGuardado = respuestas.Single(r => r.Id == idRechazo);
        Assert.Equal("Rechazado", rechazoGuardado.Respuesta);
        Assert.Equal("Aceptado", f.Consentimientos.UltimaRespuesta(turno.Id)!.Respuesta);
        Assert.Equal("Firmado", f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void Una_respuesta_nueva_no_modifica_la_firma_anterior()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        var firma = f.Consentimientos.UltimaRespuesta(turno.Id)!;
        var id = firma.Id;
        var hash = firma.Hash;
        var fecha = firma.Fecha;

        // Sea cual sea el resultado de rechazar después de firmar (la especificación no lo dice),
        // la firma original sigue existiendo y sin cambios.
        f.Consentimientos.Rechazar(turno.Id, cliente);

        var original = f.Consentimientos.RespuestasDe(turno.Id).Single(r => r.Id == id);
        Assert.Equal("Aceptado", original.Respuesta);
        Assert.Equal(hash, original.Hash);
        Assert.Equal(fecha, original.Fecha);
    }

    // ---------- K-09: prestación que no lo pide ----------

    [Fact]
    public void Prestacion_sin_consentimiento_no_admite_firma_ni_rechazo()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.TurnoEnEspera();
        turno.ClienteId = cliente.Id;
        turno.AnimalId = perro.Id;

        Assert.False(f.Consentimientos.Requiere(turno));
        Assert.Equal("No requiere", f.Consentimientos.Estado(turno));
        Assert.False(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        Assert.False(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
        Assert.DoesNotContain(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
    }

    // ---------- K-06: RN-10, iniciar la consulta ----------

    [Fact]
    public void Consulta_de_cirugia_sin_consentimiento_no_se_inicia()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.CirugiaEnEspera(cliente.Id, perro.Id);

        var resultado = f.Consultas.Iniciar(turno.Id, Persona(f, "ferreira@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Equal("En espera", f.Agenda.ObtenerPorId(turno.Id)!.Estado);
        Assert.Null(f.Consultas.ObtenerPorTurno(turno.Id));
    }

    [Fact]
    public void Consulta_de_cirugia_con_consentimiento_rechazado_no_se_inicia()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.CirugiaEnEspera(cliente.Id, perro.Id);
        Assert.True(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);

        var resultado = f.Consultas.Iniciar(turno.Id, Persona(f, "ferreira@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Equal("En espera", f.Agenda.ObtenerPorId(turno.Id)!.Estado);
        Assert.Null(f.Consultas.ObtenerPorTurno(turno.Id));
    }

    [Fact]
    public void Consulta_de_cirugia_con_consentimiento_firmado_se_inicia()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.CirugiaEnEspera(cliente.Id, perro.Id);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);

        var resultado = f.Consultas.Iniciar(turno.Id, Persona(f, "ferreira@vitalia.uy"));

        Assert.True(resultado.Exito);
        Assert.Equal("En curso", f.Agenda.ObtenerPorId(turno.Id)!.Estado);
        Assert.NotNull(f.Consultas.ObtenerPorTurno(turno.Id));
    }

    [Fact]
    public void Consulta_de_cirugia_firmada_con_version_vieja_no_se_inicia()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = f.CirugiaEnEspera(cliente.Id, perro.Id);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        Assert.True(f.Consentimientos.PublicarVersion(Castracion, "2.0", "Texto nuevo del consentimiento.", Persona(f, "admin@vitalia.uy")).Exito);

        var resultado = f.Consultas.Iniciar(turno.Id, Persona(f, "ferreira@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Equal("En espera", f.Agenda.ObtenerPorId(turno.Id)!.Estado);
        Assert.Null(f.Consultas.ObtenerPorTurno(turno.Id));
    }

    // ---------- K-07: versión nueva ----------

    [Fact]
    public void Admin_publica_version_nueva_y_lo_firmado_queda_desactualizado()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        var anterior = f.Consentimientos.DocumentoVigente(Castracion)!;
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);

        var resultado = f.Consentimientos.PublicarVersion(Castracion, "2.0", "Texto nuevo del consentimiento.", Persona(f, "admin@vitalia.uy"));

        Assert.True(resultado.Exito);
        var vigente = f.Consentimientos.DocumentoVigente(Castracion)!;
        Assert.Equal("2.0", vigente.Version);
        Assert.NotEqual(anterior.Id, vigente.Id);
        Assert.False(f.Consentimientos.ObtenerDocumento(anterior.Id)!.Vigente);
        Assert.Equal("Versión nueva", f.Consentimientos.Estado(turno));
        Assert.False(f.Consentimientos.EsValido(turno.Id));
        Assert.Contains(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
        // La firma vieja se conserva.
        Assert.Equal("1.0", f.Consentimientos.UltimaRespuesta(turno.Id)!.Version);
    }

    [Fact]
    public void Despues_de_version_nueva_se_vuelve_a_firmar_con_la_version_vigente()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        Assert.True(f.Consentimientos.PublicarVersion(Castracion, "2.0", "Texto nuevo del consentimiento.", Persona(f, "admin@vitalia.uy")).Exito);

        var resultado = f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente);

        Assert.True(resultado.Exito);
        Assert.Equal(2, f.Consentimientos.RespuestasDe(turno.Id).Count);
        var ultima = f.Consentimientos.UltimaRespuesta(turno.Id)!;
        Assert.Equal("2.0", ultima.Version);
        Assert.Equal(f.Consentimientos.DocumentoVigente(Castracion)!.Id, ultima.DocumentoId);
        Assert.Equal("Firmado", f.Consentimientos.Estado(turno));
        Assert.True(f.Consentimientos.EsValido(turno.Id));
    }

    [Theory]
    [InlineData("recepcion@vitalia.uy")]
    [InlineData("ferreira@vitalia.uy")]
    [InlineData("acosta@vitalia.uy")]
    public void Solo_el_administrador_publica_versiones(string correo)
    {
        var f = new Fabrica();
        var anterior = f.Consentimientos.DocumentoVigente(Castracion)!;

        var resultado = f.Consentimientos.PublicarVersion(Castracion, "2.0", "Texto nuevo.", Persona(f, correo));

        Assert.False(resultado.Exito);
        var vigente = f.Consentimientos.DocumentoVigente(Castracion)!;
        Assert.Equal(anterior.Id, vigente.Id);
        Assert.Equal("1.0", vigente.Version);
    }

    [Fact]
    public void Publicar_version_sin_sesion_falla()
    {
        var f = new Fabrica();
        var anterior = f.Consentimientos.DocumentoVigente(Castracion)!;

        var resultado = f.Consentimientos.PublicarVersion(Castracion, "2.0", "Texto nuevo.", null);

        Assert.False(resultado.Exito);
        Assert.Equal(anterior.Id, f.Consentimientos.DocumentoVigente(Castracion)!.Id);
    }

    [Fact]
    public void Publicar_la_misma_version_vigente_falla_y_no_invalida_firmas()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);
        var anterior = f.Consentimientos.DocumentoVigente(Castracion)!;

        var resultado = f.Consentimientos.PublicarVersion(Castracion, "1.0", "Otro texto.", Persona(f, "admin@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Equal(anterior.Id, f.Consentimientos.DocumentoVigente(Castracion)!.Id);
        Assert.Equal("Firmado", f.Consentimientos.Estado(turno));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Publicar_version_vacia_falla(string version)
    {
        // Inferencia: la especificación no lo dice de forma explícita (ver ambigüedades).
        var f = new Fabrica();
        var anterior = f.Consentimientos.DocumentoVigente(Castracion)!;

        var resultado = f.Consentimientos.PublicarVersion(Castracion, version, "Texto nuevo.", Persona(f, "admin@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Equal(anterior.Id, f.Consentimientos.DocumentoVigente(Castracion)!.Id);
    }

    // ---------- K-08: pendientes del cliente ----------

    [Fact]
    public void Pendientes_incluye_el_turno_sin_firmar_y_lo_quita_al_firmar()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);

        Assert.Contains(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);

        Assert.True(f.Consentimientos.Firmar(turno.Id, FirmaValida(), cliente).Exito);

        Assert.DoesNotContain(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
    }

    [Fact]
    public void Pendientes_incluye_el_turno_rechazado()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out var cliente, out _);
        Assert.True(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);

        Assert.Contains(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
    }

    [Fact]
    public void Pendientes_de_otro_cliente_no_incluye_el_turno_ajeno()
    {
        var f = new Fabrica();
        var turno = ReservarCastracion(f, out _, out _);

        Assert.DoesNotContain(f.Consentimientos.PendientesDe(101), t => t.Id == turno.Id);
    }
}
