using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Sesión de demostración (A-008, A-010). Dura lo que dura la pestaña del navegador.
// Puede ser de un Cliente o de alguien del personal. En la sección C la reemplaza
// la autenticación con tokens de la API (RNF-SEG-03), que también controla los permisos por rol.
public class SesionService
{
    private readonly CuentaService _cuentas;
    private readonly PersonalService _personal;

    public SesionService(CuentaService cuentas, PersonalService personal)
    {
        _cuentas = cuentas;
        _personal = personal;
    }

    // Cliente que ingresó (null si no hay o si ingresó el personal).
    public Cuenta? CuentaActual { get; private set; }

    // Persona del personal que ingresó (null si no hay o si ingresó un Cliente).
    public Personal? PersonalActual { get; private set; }

    // Sesión de Cliente: es lo que piden las páginas de clientes (animales, reservas, turnos).
    public bool Iniciada => CuentaActual is not null;

    public bool EsPersonal => PersonalActual is not null;

    public bool HaySesion => Iniciada || EsPersonal;

    public bool TieneRol(string rol)
    {
        return PersonalActual?.Rol == rol;
    }

    // Avisa a la barra superior para que se actualice al ingresar o salir.
    public event Action? Cambio;

    public ResultadoOperacion Ingresar(InicioSesion datos)
    {
        // Primero el personal: sus cuentas no son de clientes.
        var persona = _personal.ValidarCredenciales(datos.Correo, datos.Contrasena);
        if (persona is not null)
        {
            CuentaActual = null;
            PersonalActual = persona;
            Cambio?.Invoke();
            return new ResultadoOperacion { Exito = true, Mensaje = $"Hola, {persona.Nombre}." };
        }

        var cuenta = _cuentas.ValidarCredenciales(datos.Correo, datos.Contrasena);
        if (cuenta is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El correo o la contraseña no son correctos." };
        }

        // RN-38: sin verificar el correo no se puede usar la cuenta.
        if (!cuenta.Verificada)
        {
            _cuentas.MarcarPendiente(cuenta.Correo);
            return new ResultadoOperacion { Exito = false, Mensaje = "Todavía no verificaste tu correo." };
        }

        PersonalActual = null;
        CuentaActual = cuenta;
        Cambio?.Invoke();
        return new ResultadoOperacion { Exito = true, Mensaje = $"Hola, {cuenta.Nombre}." };
    }

    // Después de verificar el correo se entra directamente, sin volver a pedir la contraseña.
    public void IngresarDespuesDeVerificar(Cuenta cuenta)
    {
        if (!cuenta.Verificada)
        {
            return;
        }

        PersonalActual = null;
        CuentaActual = cuenta;
        Cambio?.Invoke();
    }

    // Después de editar «Mi cuenta» (A-009), para que la barra muestre el nombre nuevo.
    public void AvisarCambio()
    {
        Cambio?.Invoke();
    }

    public void Salir()
    {
        CuentaActual = null;
        PersonalActual = null;
        Cambio?.Invoke();
    }
}
