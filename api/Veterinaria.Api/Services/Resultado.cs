namespace Veterinaria.Api.Services;

// Resultado de una operación del Service: el valor, o el estado HTTP, el código y el mensaje del error.
// El Controller lo convierte en la respuesta (Problem Details si falla).
public class Resultado<T>
{
    public bool Exito { get; set; }
    public T? Valor { get; set; }
    public int Estado { get; set; } = 200;
    public string Codigo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";

    public static Resultado<T> Ok(T valor) => new() { Exito = true, Valor = valor };

    public static Resultado<T> Error(int estado, string codigo, string titulo, string mensaje)
        => new() { Exito = false, Estado = estado, Codigo = codigo, Titulo = titulo, Mensaje = mensaje };
}
