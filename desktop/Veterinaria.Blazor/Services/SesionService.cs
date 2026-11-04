using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Sesión de demostración (A-008). Dura lo que dura la pestaña del navegador.
// En la sección C la reemplaza la autenticación con tokens de la API (RNF-SEG-03).
public class SesionService
{
    private readonly CuentaService _cuentas;

    public SesionService(CuentaService cuentas)
    {
        _cuentas = cuentas;
    }

    public Cuenta? CuentaActual { get; private set; }

    public bool Iniciada => CuentaActual is not null;

    // Avisa a la barra superior para que se actualice al ingresar o salir.
    public event Action? Cambio;

    public ResultadoOperacion Ingresar(InicioSesion datos)
    {
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

        CuentaActual = cuenta;
        Cambio?.Invoke();
    }

    public void Salir()
    {
        CuentaActual = null;
        Cambio?.Invoke();
    }
}
