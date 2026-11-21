using System;
using System.Collections.Generic;
using System.Linq;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Caja negra de A-030 (recorridos demostrativos). Fuente: ESPECIFICACION_RECORRIDOS_DEMO.md, RD-01 a RD-08 y RD-10.
// RD-09 es de interfaz y no se prueba aca.
public class RecorridosCajaNegraTests
{
    private static readonly string[] RolesEnOrden =
    {
        RecorridosService.Visitante,
        RecorridosService.Cliente,
        PersonalService.Recepcion,
        PersonalService.Veterinario,
        PersonalService.Administrador
    };

    private static Recorrido Del(Fabrica f, string rol)
    {
        var r = f.Recorridos.DeRol(rol);
        Assert.NotNull(r);
        return r!;
    }

    private static InicioSesion Datos(string correo, string contrasena)
    {
        return new InicioSesion { Correo = correo, Contrasena = contrasena };
    }

    // Cuentas de clientes de demostracion distintas de la del recorrido Cliente (Pedro).
    private static Cuenta OtraCuentaCliente(Fabrica f)
    {
        var correoAna = Del(f, RecorridosService.Cliente).Correo;
        var otra = f.Cuentas.Todas().FirstOrDefault(c => !string.Equals(c.Correo, correoAna, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(otra);
        return otra!;
    }

    // ---------------- RD-01 ----------------

    [Fact]
    public void RD01_Todos_devuelve_exactamente_cinco_recorridos()
    {
        var f = new Fabrica();
        Assert.Equal(5, f.Recorridos.Todos().Count);
    }

    [Fact]
    public void RD01_Todos_respeta_el_orden_Visitante_Cliente_Recepcion_Veterinario_Administrador()
    {
        var f = new Fabrica();
        var roles = f.Recorridos.Todos().Select(r => r.Rol).ToArray();
        Assert.Equal(RolesEnOrden, roles);
    }

    [Fact]
    public void RD01_No_hay_roles_repetidos()
    {
        var f = new Fabrica();
        var roles = f.Recorridos.Todos().Select(r => r.Rol).ToList();
        Assert.Equal(roles.Count, roles.Distinct().Count());
    }

    [Fact]
    public void RD01_Llamar_Todos_dos_veces_da_el_mismo_orden_y_cantidad()
    {
        var f = new Fabrica();
        var a = f.Recorridos.Todos().Select(r => r.Rol).ToList();
        var b = f.Recorridos.Todos().Select(r => r.Rol).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void RD01_Servicios_nuevos_dan_los_mismos_cinco_roles()
    {
        var a = new RecorridosService().Todos().Select(r => r.Rol).ToList();
        var b = new RecorridosService().Todos().Select(r => r.Rol).ToList();
        Assert.Equal(5, a.Count);
        Assert.Equal(a, b);
    }

    // ---------------- RD-02 ----------------

    [Fact]
    public void RD02_Cada_recorrido_tiene_titulo_y_resumen_con_texto()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Titulo), "Titulo vacio en " + r.Rol);
            Assert.False(string.IsNullOrWhiteSpace(r.Resumen), "Resumen vacio en " + r.Rol);
        }
    }

    [Fact]
    public void RD02_Cada_recorrido_tiene_al_menos_cinco_pasos()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            Assert.NotNull(r.Pasos);
            Assert.True(r.Pasos.Count >= 5, "Menos de cinco pasos en " + r.Rol);
        }
    }

    [Fact]
    public void RD02_Cada_paso_tiene_accion_ruta_y_resultado_con_texto()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            for (var i = 0; i < r.Pasos.Count; i++)
            {
                var p = r.Pasos[i];
                Assert.False(string.IsNullOrWhiteSpace(p.Accion), r.Rol + " paso " + (i + 1) + ": accion vacia");
                Assert.False(string.IsNullOrWhiteSpace(p.Ruta), r.Rol + " paso " + (i + 1) + ": ruta vacia");
                Assert.False(string.IsNullOrWhiteSpace(p.Resultado), r.Rol + " paso " + (i + 1) + ": resultado vacio");
            }
        }
    }

    [Fact]
    public void RD02_Ningun_paso_es_nulo()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            Assert.DoesNotContain(r.Pasos, p => p == null);
        }
    }

    // ---------------- RD-03 ----------------

    [Theory]
    [InlineData("Visitante")]
    [InlineData("Cliente")]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    public void RD03_DeRol_devuelve_el_recorrido_del_rol(string rol)
    {
        var f = new Fabrica();
        var r = f.Recorridos.DeRol(rol);
        Assert.NotNull(r);
        Assert.Equal(rol, r!.Rol);
    }

    [Fact]
    public void RD03_DeRol_coincide_con_el_elemento_de_Todos()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            var d = f.Recorridos.DeRol(r.Rol);
            Assert.NotNull(d);
            Assert.Equal(r.Titulo, d!.Titulo);
            Assert.Equal(r.Correo, d.Correo);
            Assert.Equal(r.Pasos.Count, d.Pasos.Count);
        }
    }

    [Fact]
    public void RD03_DeRol_con_las_constantes_publicas_devuelve_recorrido()
    {
        var f = new Fabrica();
        Assert.NotNull(f.Recorridos.DeRol(RecorridosService.Visitante));
        Assert.NotNull(f.Recorridos.DeRol(RecorridosService.Cliente));
        Assert.NotNull(f.Recorridos.DeRol(PersonalService.Recepcion));
        Assert.NotNull(f.Recorridos.DeRol(PersonalService.Veterinario));
        Assert.NotNull(f.Recorridos.DeRol(PersonalService.Administrador));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("Inexistente")]
    [InlineData("Admin")]
    [InlineData("Dueño")]
    [InlineData("Cliente1")]
    [InlineData("Cliente ")]
    [InlineData(" Cliente")]
    public void RD03_DeRol_con_rol_desconocido_devuelve_nulo(string rol)
    {
        var f = new Fabrica();
        Assert.Null(f.Recorridos.DeRol(rol));
    }

    [Fact]
    public void RD03_DeRol_con_rol_nulo_devuelve_nulo_o_no_rompe_con_excepcion_inesperada()
    {
        // La especificacion no dice que pasa con nulo (ver ambiguedades). Lo razonable: nulo, sin explotar.
        var f = new Fabrica();
        Recorrido? r = null;
        var ex = Record.Exception(() => r = f.Recorridos.DeRol(null!));
        Assert.Null(ex);
        Assert.Null(r);
    }

    [Theory]
    [InlineData("cliente", "Cliente")]
    [InlineData("CLIENTE", "Cliente")]
    [InlineData("visitante", "Visitante")]
    [InlineData("administrador", "Administrador")]
    [InlineData("VETERINARIO", "Veterinario")]
    [InlineData("recepción", "Recepción")]
    [InlineData("Recepcion", "Recepción")]
    public void RD03_DeRol_con_otra_capitalizacion_no_rompe_y_si_responde_es_el_rol_correcto(string escrito, string canonico)
    {
        // Ambiguo: puede devolver nulo (exacto) o el recorrido (sin distinguir mayusculas). Nunca otro rol.
        var f = new Fabrica();
        Recorrido? r = null;
        var ex = Record.Exception(() => r = f.Recorridos.DeRol(escrito));
        Assert.Null(ex);
        if (r != null)
        {
            Assert.Equal(canonico, r.Rol);
        }
    }

    [Fact]
    public void RD03_Consultar_un_rol_desconocido_no_altera_los_recorridos()
    {
        var f = new Fabrica();
        var antes = f.Recorridos.Todos().Select(r => r.Rol).ToList();
        f.Recorridos.DeRol("Inexistente");
        var despues = f.Recorridos.Todos().Select(r => r.Rol).ToList();
        Assert.Equal(antes, despues);
    }

    // ---------------- RD-04 ----------------

    [Fact]
    public void RD04_El_Visitante_no_tiene_correo()
    {
        var f = new Fabrica();
        Assert.True(string.IsNullOrWhiteSpace(Del(f, RecorridosService.Visitante).Correo));
    }

    [Fact]
    public void RD04_Los_demas_recorridos_tienen_correo()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos().Where(x => x.Rol != RecorridosService.Visitante))
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Correo), "Sin correo: " + r.Rol);
        }
    }

    [Fact]
    public void RD04_Los_correos_de_los_recorridos_con_cuenta_son_distintos()
    {
        var f = new Fabrica();
        var correos = f.Recorridos.Todos().Where(x => x.Rol != RecorridosService.Visitante)
            .Select(x => x.Correo.Trim().ToLowerInvariant()).ToList();
        Assert.Equal(correos.Count, correos.Distinct().Count());
    }

    [Fact]
    public void RD04_El_correo_del_Cliente_es_una_cuenta_existente()
    {
        var f = new Fabrica();
        var cuenta = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo);
        Assert.NotNull(cuenta);
    }

    [Theory]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    public void RD04_El_correo_del_personal_es_una_cuenta_de_personal_existente_con_su_rol(string rol)
    {
        var f = new Fabrica();
        var correo = Del(f, rol).Correo;
        var personal = f.Personal.ValidarCredenciales(correo, PersonalService.ContrasenaDemo);
        Assert.NotNull(personal);
        Assert.Equal(rol, personal!.Rol);
    }

    [Fact]
    public void RD04_Los_correos_usuales_de_la_demostracion_son_los_de_los_recorridos()
    {
        var f = new Fabrica();
        Assert.Equal("recepcion@vitalia.uy", Del(f, PersonalService.Recepcion).Correo.Trim().ToLowerInvariant());
        Assert.Equal("ferreira@vitalia.uy", Del(f, PersonalService.Veterinario).Correo.Trim().ToLowerInvariant());
        Assert.Equal("admin@vitalia.uy", Del(f, PersonalService.Administrador).Correo.Trim().ToLowerInvariant());
    }

    // ---------------- RD-05 ----------------

    [Fact]
    public void RD05_Todo_recorrido_con_correo_inicia_con_la_contrasena_de_demostracion()
    {
        foreach (var rol in RolesEnOrden.Where(x => x != RecorridosService.Visitante))
        {
            var f = new Fabrica();
            var r = Del(f, rol);
            var res = f.Sesion.Ingresar(Datos(r.Correo, PersonalService.ContrasenaDemo));
            Assert.True(res.Exito, "No pudo ingresar: " + rol);
            Assert.True(f.Sesion.HaySesion, "Sesion no iniciada: " + rol);
        }
    }

    [Theory]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    public void RD05_El_personal_entra_como_personal_con_su_rol(string rol)
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos(Del(f, rol).Correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.True(f.Sesion.EsPersonal);
        Assert.NotNull(f.Sesion.PersonalActual);
        Assert.Equal(rol, f.Sesion.PersonalActual!.Rol);
    }

    [Fact]
    public void RD05_El_Cliente_entra_como_cliente_y_no_como_personal()
    {
        var f = new Fabrica();
        var r = Del(f, RecorridosService.Cliente);
        var res = f.Sesion.Ingresar(Datos(r.Correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.True(f.Sesion.Iniciada);
        Assert.False(f.Sesion.EsPersonal);
        Assert.Null(f.Sesion.PersonalActual);
        Assert.NotNull(f.Sesion.CuentaActual);
        Assert.Equal(f.Cuentas.ObtenerPorCorreo(r.Correo)!.Id, f.Sesion.CuentaActual!.Id);
    }

    [Fact]
    public void RD05_Antes_de_ingresar_no_hay_sesion()
    {
        var f = new Fabrica();
        Assert.False(f.Sesion.Iniciada);
        Assert.False(f.Sesion.EsPersonal);
        Assert.Null(f.Sesion.CuentaActual);
        Assert.Null(f.Sesion.PersonalActual);
    }

    [Fact]
    public void RD05_Salir_deja_la_sesion_cerrada()
    {
        var f = new Fabrica();
        f.Sesion.Ingresar(Datos(Del(f, PersonalService.Recepcion).Correo, PersonalService.ContrasenaDemo));
        f.Sesion.Salir();
        Assert.False(f.Sesion.Iniciada);
        Assert.False(f.Sesion.EsPersonal);
        Assert.Null(f.Sesion.PersonalActual);
        Assert.Null(f.Sesion.CuentaActual);
    }

    [Theory]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    [InlineData("Cliente")]
    public void RD05_Contrasena_vacia_se_rechaza_y_no_inicia_sesion(string rol)
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos(Del(f, rol).Correo, ""));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
        Assert.Null(f.Sesion.PersonalActual);
        Assert.Null(f.Sesion.CuentaActual);
    }

    [Theory]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    [InlineData("Cliente")]
    public void RD05_Contrasena_de_solo_espacios_se_rechaza(string rol)
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos(Del(f, rol).Correo, "   "));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Theory]
    [InlineData("Recepción")]
    [InlineData("Veterinario")]
    [InlineData("Administrador")]
    [InlineData("Cliente")]
    public void RD05_Contrasena_incorrecta_se_rechaza(string rol)
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos(Del(f, rol).Correo, "clave-equivocada"));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void RD05_Contrasena_con_otra_capitalizacion_se_rechaza()
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos(Del(f, PersonalService.Recepcion).Correo, PersonalService.ContrasenaDemo.ToUpperInvariant() + "x"));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void RD05_Correo_inexistente_se_rechaza_aunque_la_contrasena_sea_la_demo()
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos("nadie@vitalia.uy", PersonalService.ContrasenaDemo));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void RD05_Correo_vacio_se_rechaza()
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos("", PersonalService.ContrasenaDemo));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void RD05_Un_rechazo_no_cierra_la_sesion_ya_iniciada_del_personal()
    {
        // Ambiguo (ver ambiguedades): se comprueba solo que el rechazo no deja una sesion ajena.
        var f = new Fabrica();
        f.Sesion.Ingresar(Datos(Del(f, PersonalService.Recepcion).Correo, PersonalService.ContrasenaDemo));
        var res = f.Sesion.Ingresar(Datos(Del(f, PersonalService.Administrador).Correo, "clave-equivocada"));
        Assert.False(res.Exito);
        if (f.Sesion.PersonalActual != null)
        {
            Assert.NotEqual(PersonalService.Administrador, f.Sesion.PersonalActual.Rol);
        }
    }

    // Correo con mayusculas y espacios al ingresar: la especificacion no define la normalizacion.
    // Se verifica que no rompe y que, si acepta, entra como la cuenta correcta.

    [Fact]
    public void RD05_Correo_en_mayusculas_no_rompe_y_si_entra_es_la_cuenta_correcta()
    {
        var f = new Fabrica();
        var r = Del(f, PersonalService.Recepcion);
        ResultadoOperacion? res = null;
        var ex = Record.Exception(() => res = f.Sesion.Ingresar(Datos(r.Correo.ToUpperInvariant(), PersonalService.ContrasenaDemo)));
        Assert.Null(ex);
        Assert.NotNull(res);
        if (res!.Exito)
        {
            Assert.True(f.Sesion.EsPersonal);
            Assert.Equal(PersonalService.Recepcion, f.Sesion.PersonalActual!.Rol);
        }
        else
        {
            Assert.False(f.Sesion.Iniciada);
        }
    }

    [Fact]
    public void RD05_Correo_con_espacios_alrededor_no_rompe_y_si_entra_es_la_cuenta_correcta()
    {
        var f = new Fabrica();
        var r = Del(f, PersonalService.Administrador);
        ResultadoOperacion? res = null;
        var ex = Record.Exception(() => res = f.Sesion.Ingresar(Datos("  " + r.Correo + "  ", PersonalService.ContrasenaDemo)));
        Assert.Null(ex);
        Assert.NotNull(res);
        if (res!.Exito)
        {
            Assert.Equal(PersonalService.Administrador, f.Sesion.PersonalActual!.Rol);
        }
        else
        {
            Assert.False(f.Sesion.Iniciada);
        }
    }

    [Fact]
    public void RD05_Correo_del_cliente_con_mayusculas_y_espacios_no_rompe_y_si_entra_es_ana()
    {
        var f = new Fabrica();
        var r = Del(f, RecorridosService.Cliente);
        var idAna = f.Cuentas.ObtenerPorCorreo(r.Correo)!.Id;
        ResultadoOperacion? res = null;
        var ex = Record.Exception(() => res = f.Sesion.Ingresar(Datos(" " + r.Correo.ToUpperInvariant() + " ", PersonalService.ContrasenaDemo)));
        Assert.Null(ex);
        Assert.NotNull(res);
        if (res!.Exito)
        {
            Assert.False(f.Sesion.EsPersonal);
            Assert.Equal(idAna, f.Sesion.CuentaActual!.Id);
        }
        else
        {
            Assert.False(f.Sesion.Iniciada);
        }
    }

    [Fact]
    public void RD05_Correo_de_solo_espacios_se_rechaza()
    {
        var f = new Fabrica();
        var res = f.Sesion.Ingresar(Datos("   ", PersonalService.ContrasenaDemo));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    // ---------------- RD-06 ----------------

    [Fact]
    public void RD06_Los_clientes_de_demostracion_existen_y_tienen_el_correo_verificado()
    {
        var f = new Fabrica();
        var cuentas = f.Cuentas.Todas();
        Assert.True(cuentas.Count >= 2, "Se esperan al menos Ana y Pedro");
        Assert.All(cuentas, c => Assert.True(c.Verificada, "Sin verificar: " + c.Correo));
    }

    [Fact]
    public void RD06_Ana_ingresa_con_la_contrasena_de_demostracion()
    {
        var f = new Fabrica();
        var correo = Del(f, RecorridosService.Cliente).Correo;
        var res = f.Sesion.Ingresar(Datos(correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.True(f.Cuentas.ObtenerPorCorreo(correo)!.Verificada);
    }

    [Fact]
    public void RD06_Pedro_ingresa_con_la_contrasena_de_demostracion()
    {
        var f = new Fabrica();
        var pedro = OtraCuentaCliente(f);
        var res = f.Sesion.Ingresar(Datos(pedro.Correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.True(f.Sesion.Iniciada);
        Assert.False(f.Sesion.EsPersonal);
        Assert.Equal(pedro.Id, f.Sesion.CuentaActual!.Id);
    }

    [Fact]
    public void RD06_Todas_las_cuentas_de_cliente_de_demostracion_entran_con_la_contrasena_demo()
    {
        foreach (var cuenta in new Fabrica().Cuentas.Todas())
        {
            var f = new Fabrica();
            var res = f.Sesion.Ingresar(Datos(cuenta.Correo, PersonalService.ContrasenaDemo));
            Assert.True(res.Exito, "No entra: " + cuenta.Correo);
            Assert.False(f.Sesion.EsPersonal);
        }
    }

    [Fact]
    public void RD06_Pedro_con_contrasena_incorrecta_se_rechaza()
    {
        var f = new Fabrica();
        var pedro = OtraCuentaCliente(f);
        var res = f.Sesion.Ingresar(Datos(pedro.Correo, "clave-equivocada"));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
        Assert.Null(f.Sesion.CuentaActual);
    }

    [Fact]
    public void RD06_Ana_con_contrasena_incorrecta_se_rechaza_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var correo = Del(f, RecorridosService.Cliente).Correo;
        var cantidad = f.Cuentas.Todas().Count;
        var res = f.Sesion.Ingresar(Datos(correo, "otra"));
        Assert.False(res.Exito);
        Assert.False(f.Sesion.Iniciada);
        Assert.Equal(cantidad, f.Cuentas.Todas().Count);
        Assert.True(f.Cuentas.ObtenerPorCorreo(correo)!.Verificada);
    }

    [Fact]
    public void RD06_Un_intento_fallido_no_impide_ingresar_despues_con_la_clave_correcta()
    {
        var f = new Fabrica();
        var correo = Del(f, RecorridosService.Cliente).Correo;
        Assert.False(f.Sesion.Ingresar(Datos(correo, "mal")).Exito);
        Assert.True(f.Sesion.Ingresar(Datos(correo, PersonalService.ContrasenaDemo)).Exito);
    }

    [Fact]
    public void RD06_Cliente_que_cierra_sesion_y_entra_otro_queda_como_el_nuevo()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var pedro = OtraCuentaCliente(f);

        Assert.True(f.Sesion.Ingresar(Datos(ana.Correo, PersonalService.ContrasenaDemo)).Exito);
        Assert.Equal(ana.Id, f.Sesion.CuentaActual!.Id);

        f.Sesion.Salir();
        Assert.False(f.Sesion.Iniciada);
        Assert.Null(f.Sesion.CuentaActual);

        Assert.True(f.Sesion.Ingresar(Datos(pedro.Correo, PersonalService.ContrasenaDemo)).Exito);
        Assert.True(f.Sesion.Iniciada);
        Assert.Equal(pedro.Id, f.Sesion.CuentaActual!.Id);
        Assert.NotEqual(ana.Id, f.Sesion.CuentaActual.Id);
    }

    [Fact]
    public void RD06_Cliente_que_sale_y_entra_personal_deja_de_ser_cliente()
    {
        var f = new Fabrica();
        f.Sesion.Ingresar(Datos(Del(f, RecorridosService.Cliente).Correo, PersonalService.ContrasenaDemo));
        f.Sesion.Salir();
        var res = f.Sesion.Ingresar(Datos(Del(f, PersonalService.Veterinario).Correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.True(f.Sesion.EsPersonal);
        Assert.Equal(PersonalService.Veterinario, f.Sesion.PersonalActual!.Rol);
    }

    [Fact]
    public void RD06_Personal_que_sale_y_entra_cliente_deja_de_ser_personal()
    {
        var f = new Fabrica();
        f.Sesion.Ingresar(Datos(Del(f, PersonalService.Administrador).Correo, PersonalService.ContrasenaDemo));
        f.Sesion.Salir();
        var res = f.Sesion.Ingresar(Datos(Del(f, RecorridosService.Cliente).Correo, PersonalService.ContrasenaDemo));
        Assert.True(res.Exito);
        Assert.False(f.Sesion.EsPersonal);
        Assert.Null(f.Sesion.PersonalActual);
        Assert.NotNull(f.Sesion.CuentaActual);
    }

    [Fact]
    public void RD06_Salir_sin_haber_ingresado_no_rompe()
    {
        var f = new Fabrica();
        var ex = Record.Exception(() => f.Sesion.Salir());
        Assert.Null(ex);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void RD06_Salir_dos_veces_no_rompe()
    {
        var f = new Fabrica();
        f.Sesion.Ingresar(Datos(Del(f, RecorridosService.Cliente).Correo, PersonalService.ContrasenaDemo));
        f.Sesion.Salir();
        var ex = Record.Exception(() => f.Sesion.Salir());
        Assert.Null(ex);
        Assert.False(f.Sesion.Iniciada);
    }

    // ---------------- RD-07 ----------------

    // Las rutas validas se obtienen por reflexion: los atributos de ruta (@page) de los componentes del ensamblado.
    private static List<string> RutasExistentes()
    {
        var rutas = new List<string>();
        foreach (var tipo in typeof(RecorridosService).Assembly.GetTypes())
        {
            foreach (var atributo in tipo.GetCustomAttributes(false))
            {
                var t = atributo.GetType();
                if (t.Name != "RouteAttribute")
                {
                    continue;
                }
                var propiedad = t.GetProperty("Template");
                var valor = propiedad == null ? null : propiedad.GetValue(atributo) as string;
                if (!string.IsNullOrEmpty(valor))
                {
                    rutas.Add(valor!);
                }
            }
        }
        return rutas;
    }

    [Fact]
    public void RD07_Hay_pantallas_con_ruta_para_comparar()
    {
        Assert.NotEmpty(RutasExistentes());
    }

    [Fact]
    public void RD07_Toda_ruta_de_un_paso_corresponde_a_una_pantalla_existente()
    {
        var existentes = RutasExistentes().Select(x => x.Trim().TrimEnd('/').ToLowerInvariant()).ToHashSet();
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            for (var i = 0; i < r.Pasos.Count; i++)
            {
                var ruta = r.Pasos[i].Ruta.Trim().TrimEnd('/').ToLowerInvariant();
                Assert.True(existentes.Contains(ruta), r.Rol + " paso " + (i + 1) + ": la ruta '" + r.Pasos[i].Ruta + "' no tiene @page");
            }
        }
    }

    [Fact]
    public void RD07_Las_rutas_de_los_pasos_empiezan_con_barra()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            foreach (var p in r.Pasos)
            {
                Assert.StartsWith("/", p.Ruta.Trim());
            }
        }
    }

    [Fact]
    public void RD07_Ninguna_ruta_de_los_pasos_usa_parametros()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            foreach (var p in r.Pasos)
            {
                Assert.DoesNotContain("{", p.Ruta);
                Assert.DoesNotContain("}", p.Ruta);
            }
        }
    }

    [Fact]
    public void RD07_Las_rutas_de_los_pasos_no_tienen_espacios_internos()
    {
        var f = new Fabrica();
        foreach (var r in f.Recorridos.Todos())
        {
            foreach (var p in r.Pasos)
            {
                Assert.DoesNotContain(" ", p.Ruta.Trim());
            }
        }
    }

    // ---------------- RD-08 ----------------

    [Fact]
    public void RD08_Dos_sesiones_nuevas_tienen_la_misma_cantidad_de_cuentas_de_clientes()
    {
        var a = new Fabrica();
        var b = new Fabrica();
        Assert.Equal(a.Cuentas.Todas().Count, b.Cuentas.Todas().Count);
    }

    [Fact]
    public void RD08_Dos_sesiones_nuevas_tienen_los_mismos_correos_de_clientes()
    {
        var a = new Fabrica().Cuentas.Todas().Select(c => c.Correo).OrderBy(x => x).ToList();
        var b = new Fabrica().Cuentas.Todas().Select(c => c.Correo).OrderBy(x => x).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void RD08_Dos_sesiones_nuevas_tienen_los_mismos_recorridos_con_los_mismos_correos()
    {
        var a = new Fabrica().Recorridos.Todos().Select(r => r.Rol + "|" + r.Correo + "|" + r.Pasos.Count).ToList();
        var b = new Fabrica().Recorridos.Todos().Select(r => r.Rol + "|" + r.Correo + "|" + r.Pasos.Count).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void RD08_Dos_sesiones_nuevas_tienen_el_mismo_personal_de_demostracion()
    {
        var a = new Fabrica();
        var b = new Fabrica();
        foreach (var rol in new[] { PersonalService.Recepcion, PersonalService.Veterinario, PersonalService.Administrador })
        {
            var pa = a.Personal.ValidarCredenciales(Del(a, rol).Correo, PersonalService.ContrasenaDemo);
            var pb = b.Personal.ValidarCredenciales(Del(b, rol).Correo, PersonalService.ContrasenaDemo);
            Assert.NotNull(pa);
            Assert.NotNull(pb);
            Assert.Equal(pa!.Id, pb!.Id);
            Assert.Equal(pa.Rol, pb.Rol);
        }
    }

    [Fact]
    public void RD08_Dos_sesiones_nuevas_tienen_los_mismos_animales_por_cuenta()
    {
        var a = new Fabrica();
        var b = new Fabrica();
        var cuentasA = a.Cuentas.Todas().OrderBy(c => c.Correo).ToList();
        var cuentasB = b.Cuentas.Todas().OrderBy(c => c.Correo).ToList();
        Assert.Equal(cuentasA.Count, cuentasB.Count);
        for (var i = 0; i < cuentasA.Count; i++)
        {
            var animalesA = a.Animales.ObtenerDe(cuentasA[i].Id).Select(x => x.Nombre).ToList();
            var animalesB = b.Animales.ObtenerDe(cuentasB[i].Id).Select(x => x.Nombre).ToList();
            Assert.Equal(animalesA, animalesB);
        }
    }

    [Fact]
    public void RD08_Los_animales_de_Ana_son_los_mismos_en_dos_sesiones_nuevas()
    {
        var a = new Fabrica();
        var b = new Fabrica();
        var anaA = a.Cuentas.ObtenerPorCorreo(Del(a, RecorridosService.Cliente).Correo)!;
        var anaB = b.Cuentas.ObtenerPorCorreo(Del(b, RecorridosService.Cliente).Correo)!;
        Assert.Equal(
            a.Animales.ObtenerDe(anaA.Id).Select(x => x.Nombre).ToList(),
            b.Animales.ObtenerDe(anaB.Id).Select(x => x.Nombre).ToList());
    }

    [Fact]
    public void RD08_Lo_que_se_hace_en_una_sesion_no_aparece_en_otra_nueva()
    {
        var a = new Fabrica();
        var cuentasAntes = a.Cuentas.Todas().Count;
        var ana = a.Cuentas.ObtenerPorCorreo(Del(a, RecorridosService.Cliente).Correo)!;
        var animalesAntes = a.Animales.ObtenerDe(ana.Id).Count;

        var nueva = a.ClienteVerificado("nuevo@prueba.uy", "2.345.678-9");
        a.AnimalDe(ana.Id, "Gato", "Garfield");
        a.AnimalDe(nueva.Id, "Perro", "Rex");

        Assert.Equal(cuentasAntes + 1, a.Cuentas.Todas().Count);
        Assert.Equal(animalesAntes + 1, a.Animales.ObtenerDe(ana.Id).Count);

        var b = new Fabrica();
        var anaB = b.Cuentas.ObtenerPorCorreo(Del(b, RecorridosService.Cliente).Correo)!;
        Assert.Equal(cuentasAntes, b.Cuentas.Todas().Count);
        Assert.Equal(animalesAntes, b.Animales.ObtenerDe(anaB.Id).Count);
        Assert.Null(b.Cuentas.ObtenerPorCorreo("nuevo@prueba.uy"));
    }

    [Fact]
    public void RD08_Una_sesion_iniciada_en_una_fabrica_no_afecta_a_otra()
    {
        var a = new Fabrica();
        var b = new Fabrica();
        a.Sesion.Ingresar(Datos(Del(a, PersonalService.Administrador).Correo, PersonalService.ContrasenaDemo));
        Assert.True(a.Sesion.HaySesion);
        Assert.False(b.Sesion.HaySesion);
    }

    // ---------------- RD-10 ----------------

    [Fact]
    public void RD10_Ana_tiene_dos_animales_Toby_y_Mishi()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var nombres = f.Animales.ObtenerDe(ana.Id).Select(a => a.Nombre).ToList();
        Assert.Equal(2, nombres.Count);
        Assert.Contains("Toby", nombres);
        Assert.Contains("Mishi", nombres);
    }

    [Fact]
    public void RD10_Los_animales_de_Ana_vienen_ordenados_por_nombre()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var nombres = f.Animales.ObtenerDe(ana.Id).Select(a => a.Nombre).ToList();
        Assert.Equal(new[] { "Mishi", "Toby" }, nombres.ToArray());
    }

    [Fact]
    public void RD10_Pedro_tiene_dos_animales_y_uno_es_Canela()
    {
        var f = new Fabrica();
        var pedro = OtraCuentaCliente(f);
        var nombres = f.Animales.ObtenerDe(pedro.Id).Select(a => a.Nombre).ToList();
        Assert.Equal(2, nombres.Count);
        Assert.Contains("Canela", nombres);
    }

    [Fact]
    public void RD10_El_rodeo_de_Pedro_es_un_animal_distinto_de_Canela()
    {
        var f = new Fabrica();
        var pedro = OtraCuentaCliente(f);
        var nombres = f.Animales.ObtenerDe(pedro.Id).Select(a => a.Nombre).ToList();
        Assert.Equal(2, nombres.Distinct().Count());
        Assert.Single(nombres, n => n != "Canela");
    }

    [Fact]
    public void RD10_Un_cliente_no_ve_los_animales_del_otro()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var pedro = OtraCuentaCliente(f);

        var deAna = f.Animales.ObtenerDe(ana.Id);
        var dePedro = f.Animales.ObtenerDe(pedro.Id);

        Assert.DoesNotContain(deAna, a => a.Nombre == "Canela");
        Assert.DoesNotContain(dePedro, a => a.Nombre == "Toby");
        Assert.DoesNotContain(dePedro, a => a.Nombre == "Mishi");
        Assert.Empty(deAna.Select(a => a.Nombre).Intersect(dePedro.Select(a => a.Nombre)));
    }

    [Fact]
    public void RD10_Los_ids_de_animales_de_Ana_y_Pedro_no_se_mezclan()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var pedro = OtraCuentaCliente(f);
        var idsAna = f.Animales.ObtenerDe(ana.Id).Select(a => a.Id).ToList();
        var idsPedro = f.Animales.ObtenerDe(pedro.Id).Select(a => a.Id).ToList();
        Assert.Empty(idsAna.Intersect(idsPedro));
    }

    [Fact]
    public void RD10_Una_cuenta_inexistente_no_tiene_animales()
    {
        var f = new Fabrica();
        Assert.Empty(f.Animales.ObtenerDe(int.MaxValue));
        Assert.Empty(f.Animales.ObtenerDe(-1));
    }

    [Fact]
    public void RD10_Agregar_un_animal_a_Ana_no_lo_muestra_a_Pedro()
    {
        var f = new Fabrica();
        var ana = f.Cuentas.ObtenerPorCorreo(Del(f, RecorridosService.Cliente).Correo)!;
        var pedro = OtraCuentaCliente(f);
        var antesPedro = f.Animales.ObtenerDe(pedro.Id).Select(a => a.Nombre).ToList();

        f.AnimalDe(ana.Id, "Gato", "Garfield");

        Assert.Equal(3, f.Animales.ObtenerDe(ana.Id).Count);
        Assert.Equal(antesPedro, f.Animales.ObtenerDe(pedro.Id).Select(a => a.Nombre).ToList());
    }

    [Fact]
    public void RD10_Tras_cerrar_sesion_y_entrar_otro_cliente_la_cuenta_actual_ve_solo_sus_animales()
    {
        var f = new Fabrica();
        var correoAna = Del(f, RecorridosService.Cliente).Correo;
        var pedro = OtraCuentaCliente(f);

        f.Sesion.Ingresar(Datos(correoAna, PersonalService.ContrasenaDemo));
        var nombresAna = f.Animales.ObtenerDe(f.Sesion.CuentaActual!.Id).Select(a => a.Nombre).ToList();
        Assert.Contains("Toby", nombresAna);

        f.Sesion.Salir();
        f.Sesion.Ingresar(Datos(pedro.Correo, PersonalService.ContrasenaDemo));
        var nombresPedro = f.Animales.ObtenerDe(f.Sesion.CuentaActual!.Id).Select(a => a.Nombre).ToList();

        Assert.Contains("Canela", nombresPedro);
        Assert.DoesNotContain("Toby", nombresPedro);
        Assert.DoesNotContain("Mishi", nombresPedro);
    }
}
