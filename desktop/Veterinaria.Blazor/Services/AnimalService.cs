using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Animales de demostración en memoria (sección A). En la sección C se reemplaza por AnimalApiService
// (GET/POST/PUT /api/v1/animales); la API valida la responsabilidad (RN-18) y el microchip único (RN-41).
public class AnimalService
{
    private readonly List<Animal> _animales = new()
    {
        // Animales de los clientes de demostración (Ana Rodríguez = 100, Pedro Gómez = 101).
        // Toby también sirve para probar que no se puede repetir su microchip (RN-41).
        new() { Id = 1, ResponsableId = 100, Nombre = "Toby", Especie = "Perro", Raza = "Mestizo", Sexo = "Macho", Microchip = "858000000000001", FechaAlta = new DateTime(2026, 10, 1) },
        new() { Id = 2, ResponsableId = 100, Nombre = "Mishi", Especie = "Gato", Raza = "Común europeo", Sexo = "Hembra", FechaAlta = new DateTime(2026, 10, 1) },
        new() { Id = 3, ResponsableId = 101, Nombre = "Rodeo La Esperanza", Especie = "Vaca", Raza = "Hereford", Sexo = "Hembra", EsGrupo = true, CantidadGrupo = 25, Caravana = "UY-BV-0815", FechaAlta = new DateTime(2026, 10, 1) },
        new() { Id = 4, ResponsableId = 101, Nombre = "Canela", Especie = "Caballo", Raza = "Criollo", Sexo = "Hembra", FechaAlta = new DateTime(2026, 10, 1) }
    };

    private int _ultimoId = 4;

    // Para el personal: cualquier animal, sin filtrar por responsable (RF-REC-03).
    public Animal? ObtenerParaPersonal(int id)
    {
        return _animales.FirstOrDefault(a => a.Id == id);
    }

    public List<string> Especies { get; } = new()
    {
        "Perro", "Gato", "Caballo", "Oveja", "Vaca", "Cerdo", "Cabra", "Ave", "Conejo", "Reptil", "Otra"
    };

    public List<string> Sexos { get; } = new() { "Macho", "Hembra", "Sin determinar" };

    // RN-18: solo los animales del responsable.
    public List<Animal> ObtenerDe(int cuentaId)
    {
        return _animales
            .Where(a => a.ResponsableId == cuentaId)
            .OrderBy(a => a.Nombre)
            .ToList();
    }

    // Devuelve null si no existe o si no es de ese responsable, sin distinguir los casos (RN-18).
    public Animal? ObtenerPorId(int id, int cuentaId)
    {
        return _animales.FirstOrDefault(a => a.Id == id && a.ResponsableId == cuentaId);
    }

    public ResultadoOperacion Crear(AnimalFormulario datos, int cuentaId)
    {
        if (MicrochipEnUso(datos.Microchip, idActual: 0))
        {
            return MicrochipRepetido();
        }

        _ultimoId++;
        var animal = new Animal { Id = _ultimoId, ResponsableId = cuentaId, FechaAlta = DateTime.Now };
        CopiarDatos(datos, animal);
        _animales.Add(animal);

        return new ResultadoOperacion { Exito = true, Mensaje = $"{animal.Nombre} quedó registrado." };
    }

    public ResultadoOperacion Editar(int id, AnimalFormulario datos, int cuentaId)
    {
        // CU-CLI-01 A4: si ya no es responsable, se rechaza.
        var animal = ObtenerPorId(id, cuentaId);
        if (animal is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos ese animal entre los tuyos." };
        }

        if (MicrochipEnUso(datos.Microchip, idActual: id))
        {
            return MicrochipRepetido();
        }

        CopiarDatos(datos, animal);
        return new ResultadoOperacion { Exito = true, Mensaje = "Los cambios quedaron guardados." };
    }

    // Prepara el formulario de edición con los datos actuales.
    public AnimalFormulario ParaEditar(Animal animal)
    {
        return new AnimalFormulario
        {
            Nombre = animal.Nombre,
            Especie = animal.Especie,
            Raza = animal.Raza,
            Sexo = animal.Sexo,
            FechaNacimiento = animal.FechaNacimiento,
            Microchip = animal.Microchip,
            Caravana = animal.Caravana,
            EsGrupo = animal.EsGrupo,
            CantidadGrupo = animal.CantidadGrupo,
            SenasParticulares = animal.SenasParticulares,
            Antecedentes = animal.Antecedentes
        };
    }

    // A-013: ejemplos sintéticos de caballo y oveja para probar la sección A.
    public void CargarEjemplos(int cuentaId)
    {
        Crear(new AnimalFormulario
        {
            Nombre = "Lucero", Especie = "Caballo", Raza = "Criollo", Sexo = "Macho",
            FechaNacimiento = new DateOnly(2018, 9, 12), Microchip = "858000000123456",
            SenasParticulares = "Tordillo, estrella blanca en la frente.",
            Antecedentes = "Rehabilitación de tendón en 2025."
        }, cuentaId);

        Crear(new AnimalFormulario
        {
            Nombre = "Majada del potrero norte", Especie = "Oveja", Raza = "Corriedale", Sexo = "Hembra",
            Caravana = "UY-OV-2041", EsGrupo = true, CantidadGrupo = 40,
            SenasParticulares = "Señal en oreja izquierda."
        }, cuentaId);
    }

    private bool MicrochipEnUso(string microchip, int idActual)
    {
        return !string.IsNullOrWhiteSpace(microchip)
            && _animales.Any(a => a.Microchip == microchip.Trim() && a.Id != idActual);
    }

    // CU-CLI-01 A1: no se revela de quién es el otro animal.
    private static ResultadoOperacion MicrochipRepetido()
    {
        return new ResultadoOperacion { Exito = false, Mensaje = "Ese microchip ya figura en el sistema. Consultá en la clínica para revisarlo." };
    }

    private static void CopiarDatos(AnimalFormulario datos, Animal animal)
    {
        animal.Nombre = datos.Nombre.Trim();
        animal.Especie = datos.Especie;
        animal.Raza = datos.Raza.Trim();
        animal.Sexo = datos.Sexo;
        animal.FechaNacimiento = datos.FechaNacimiento;
        animal.Microchip = datos.EsGrupo ? "" : datos.Microchip.Trim();
        animal.Caravana = datos.Caravana.Trim();
        animal.EsGrupo = datos.EsGrupo;
        animal.CantidadGrupo = datos.EsGrupo ? datos.CantidadGrupo : null;
        animal.SenasParticulares = datos.SenasParticulares.Trim();
        animal.Antecedentes = datos.Antecedentes.Trim();
    }
}
