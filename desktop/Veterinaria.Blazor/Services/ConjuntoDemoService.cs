using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Conjunto grande de datos de demostración (G-009, D-DEM-03): 30 clientes, 50 animales y 200 consultas históricas
// firmadas, con casos de caballo y oveja. Es reproducible (sin azar: cada dato sale de su posición) y se carga solo
// cuando alguien llama a Cargar(); cómo se activa (ajuste, botón o siempre) lo decide Diego. No reemplaza a Ana ni a Pedro:
// se suma a ellos. Todos los clientes entran con la contraseña de demostración del personal. Datos ficticios.
public class ConjuntoDemoService
{
    public const int ClientesTotales = 30;
    public const int AnimalesTotales = 50;
    public const int ConsultasTotales = 200;

    private readonly CuentaService _cuentas;
    private readonly AnimalService _animales;
    private readonly ConsultaService _consultas;

    public bool Cargado { get; private set; }

    public ConjuntoDemoService(CuentaService cuentas, AnimalService animales, ConsultaService consultas)
    {
        _cuentas = cuentas;
        _animales = animales;
        _consultas = consultas;
    }

    private static readonly string[] Nombres =
    {
        "Lucía", "Martín", "Valentina", "Santiago", "Camila", "Matías", "Sofía", "Nicolás", "Florencia", "Federico",
        "Agustina", "Joaquín", "Micaela", "Sebastián", "Julieta", "Gonzalo", "Rocío", "Facundo", "Carolina", "Maximiliano",
        "Paula", "Diego", "Natalia", "Emiliano", "Victoria", "Ignacio", "Daniela", "Rodrigo"
    };

    private static readonly string[] Apellidos =
    {
        "Techera", "Silva", "Pereira", "Rodríguez", "Fernández", "Cabrera", "Martínez", "Olivera", "Suárez", "Barboza",
        "Núñez", "Acosta", "Correa", "Píriz", "Machado", "Larrosa", "Benítez", "Ramos", "Viera", "Sosa",
        "Cardozo", "Ferreira", "Alonso", "Rivero", "Vázquez", "Duarte", "Zeballos", "Castro"
    };

    // Localidades de Colonia y Soriano; los clientes con campo (caballos, ovejas, vacas) van a las rurales.
    private static readonly (string Localidad, string Departamento)[] Lugares =
    {
        ("Nueva Palmira", "Colonia"), ("Carmelo", "Colonia"), ("Colonia del Sacramento", "Colonia"), ("Dolores", "Soriano"),
        ("Rosario", "Colonia"), ("Mercedes", "Soriano"), ("Tarariras", "Colonia"), ("Juan Lacaze", "Colonia")
    };

    // (Especie, Raza, Sexo, Nombre, grupo): 46 animales nuevos. Los 4 de Ana y Pedro completan los 50.
    private static readonly (string Especie, string Raza, string Sexo, string Nombre, int Grupo)[] Animales =
    {
        ("Perro", "Labrador", "Macho", "Bruno", 0), ("Perro", "Mestizo", "Hembra", "Luna", 0), ("Perro", "Border collie", "Macho", "Pampa", 0),
        ("Perro", "Caniche", "Hembra", "Nina", 0), ("Perro", "Mestizo", "Macho", "Lobo", 0), ("Perro", "Cimarrón uruguayo", "Hembra", "Tola", 0),
        ("Perro", "Golden retriever", "Macho", "Simón", 0), ("Perro", "Mestizo", "Hembra", "Canela", 0), ("Perro", "Ovejero alemán", "Macho", "Rex", 0),
        ("Perro", "Salchicha", "Hembra", "Olivia", 0), ("Perro", "Mestizo", "Macho", "Chiquito", 0), ("Perro", "Bulldog francés", "Macho", "Tano", 0),
        ("Perro", "Mestizo", "Hembra", "Frida", 0), ("Perro", "Boyero de Berna", "Macho", "Oso", 0), ("Perro", "Beagle", "Hembra", "Lola", 0),
        ("Perro", "Mestizo", "Macho", "Gaucho", 0), ("Perro", "Cocker", "Hembra", "Miel", 0), ("Perro", "Pastor belga", "Macho", "Zeus", 0),
        ("Gato", "Común europeo", "Hembra", "Pelusa", 0), ("Gato", "Siamés", "Macho", "Tigre", 0), ("Gato", "Común europeo", "Macho", "Mimo", 0),
        ("Gato", "Angora", "Hembra", "Bruja", 0), ("Gato", "Común europeo", "Hembra", "Violeta", 0), ("Gato", "Persa", "Macho", "Garfield", 0),
        ("Gato", "Común europeo", "Macho", "Michi", 0), ("Gato", "Común europeo", "Hembra", "Cleo", 0), ("Gato", "Bengalí", "Macho", "Simba", 0),
        ("Gato", "Común europeo", "Hembra", "Sombra", 0), ("Gato", "Común europeo", "Macho", "Felipe", 0), ("Gato", "Común europeo", "Hembra", "Chispa", 0),
        ("Caballo", "Criollo", "Macho", "Zaino", 0), ("Caballo", "Criollo", "Hembra", "Malacara", 0), ("Caballo", "Pura sangre", "Macho", "Relámpago", 0),
        ("Caballo", "Criollo", "Hembra", "Tordilla", 0), ("Caballo", "Cuarto de milla", "Macho", "Colorado", 0), ("Caballo", "Criollo", "Macho", "Overo", 0),
        ("Oveja", "Corriedale", "Hembra", "Majada Los Ceibos", 60), ("Oveja", "Merino", "Hembra", "Majada San Pedro", 120),
        ("Oveja", "Texel", "Hembra", "Majada La Aurora", 45), ("Oveja", "Ideal", "Hembra", "Majada El Tala", 80),
        ("Vaca", "Aberdeen Angus", "Hembra", "Rodeo San José", 30), ("Vaca", "Holando", "Hembra", "Tambo Las Acacias", 55),
        ("Cabra", "Anglo nubian", "Hembra", "Cabras del Arroyo", 12), ("Conejo", "Cabeza de león", "Macho", "Copito", 0),
        ("Ave", "Cotorra", "Macho", "Paquito", 0), ("Cerdo", "Pietrain", "Hembra", "Chancha Rosa", 0)
    };

    // Motivos y diagnósticos por tipo de atención, con su tratamiento.
    private static readonly (string Motivo, string Diagnostico, string Tipo, string Tratamiento)[] Pequenos =
    {
        ("Control anual y vacunación", "Paciente sano.", "Definitivo", "Vacuna aplicada. Control en un año."),
        ("Vómitos desde hace dos días", "Gastroenteritis leve.", "Presuntivo", "Dieta blanda 3 días y protector gástrico."),
        ("Picazón y rascado", "Dermatitis alérgica.", "Presuntivo", "Antihistamínico y baño medicado semanal."),
        ("Cojera del miembro posterior", "Esguince leve.", "Presuntivo", "Reposo 7 días y antiinflamatorio."),
        ("Desparasitación", "Sin parásitos visibles.", "Definitivo", "Antiparasitario interno según peso."),
        ("Otitis recurrente", "Otitis externa.", "Definitivo", "Limpieza y gotas óticas 10 días."),
        ("Pérdida de apetito", "Cuadro digestivo inespecífico.", "Presuntivo", "Observación y dieta digestiva."),
        ("Control de peso", "Sobrepeso leve.", "Definitivo", "Ajuste de ración y ejercicio diario."),
        ("Herida por pelea", "Laceración superficial.", "Definitivo", "Limpieza, sutura y antibiótico 5 días."),
        ("Control posoperatorio", "Evolución favorable.", "Definitivo", "Retiro de puntos y alta.")
    };

    private static readonly (string Motivo, string Diagnostico, string Tipo, string Tratamiento)[] Campo =
    {
        ("Revisión sanitaria de rodeo", "Rodeo en buen estado.", "Definitivo", "Refuerzo de vacunas del plan anual."),
        ("Baja de producción", "Deficiencia mineral.", "Presuntivo", "Suplementación mineral por 30 días."),
        ("Cojeras en el lote", "Pietín leve.", "Presuntivo", "Baño de pezuñas y antibiótico."),
        ("Parásitos gastrointestinales", "Carga parasitaria moderada.", "Definitivo", "Antiparasitario por peso del lote."),
        ("Preparación para esquila", "Lote apto.", "Definitivo", "Control de pezuñas y desparasitación."),
        ("Partos y recría", "Controles de parto sin complicaciones.", "Definitivo", "Seguimiento de crías 15 días.")
    };

    private static readonly (string Motivo, string Diagnostico, string Tipo, string Tratamiento)[] Equinos =
    {
        ("Control de herraje y aplomos", "Aplomos correctos.", "Definitivo", "Herraje cada 45 días."),
        ("Cólico leve", "Cólico espasmódico.", "Presuntivo", "Espasmolítico y control de dieta."),
        ("Claudicación", "Tendinitis leve.", "Presuntivo", "Reposo en box y frío local."),
        ("Vacunación y desparasitación", "Paciente sano.", "Definitivo", "Vacuna antitetánica y antiparasitario."),
        ("Revisión dental", "Puntas de esmalte.", "Definitivo", "Limado dental."),
        ("Herida en miembro", "Herida superficial.", "Definitivo", "Limpieza, vendaje y antibiótico.")
    };

    // Carga el conjunto una sola vez; si ya estaba cargado no hace nada.
    public ResultadoOperacion Cargar()
    {
        if (Cargado)
        {
            return new ResultadoOperacion { Exito = true, Mensaje = "El conjunto completo ya estaba cargado." };
        }

        var clientes = CrearClientes();
        var animales = CrearAnimales(clientes);
        CrearConsultas(animales);
        Cargado = true;

        return new ResultadoOperacion
        {
            Exito = true,
            Mensaje = $"Conjunto completo cargado: {ClientesTotales} clientes, {AnimalesTotales} animales y {ConsultasTotales} consultas históricas."
        };
    }

    // 28 clientes nuevos (Id 200 a 227) más Ana y Pedro hacen 30.
    private List<Cuenta> CrearClientes()
    {
        var lista = new List<Cuenta>();
        for (var i = 0; i < Nombres.Length; i++)
        {
            var lugar = Lugares[i % Lugares.Length];
            var numero = (i + 1).ToString("00");
            lista.Add(_cuentas.AgregarClienteDemo(new Cuenta
            {
                Id = 200 + i,
                Nombre = Nombres[i],
                Apellido = Apellidos[i],
                Correo = $"cliente{numero}.demo@vitalia.uy",
                Telefono = $"+598 99 {100 + i * 7:000} {200 + i * 13:000}",
                Calle = "Calle " + (i + 10),
                NumeroPuerta = (100 + i * 3).ToString(),
                Localidad = lugar.Localidad,
                Departamento = lugar.Departamento,
                FechaCreacion = new DateTime(2026, 9, 1).AddDays(i)
            }));
        }

        return lista;
    }

    // 46 animales nuevos repartidos entre los 28 clientes nuevos; los de campo van a quien tiene campo.
    private List<Animal> CrearAnimales(List<Cuenta> clientes)
    {
        var lista = new List<Animal>();
        for (var i = 0; i < Animales.Length; i++)
        {
            var (especie, raza, sexo, nombre, grupo) = Animales[i];
            var responsable = clientes[(i * 5 + 3) % clientes.Count];
            var animal = new Animal
            {
                ResponsableId = responsable.Id,
                Nombre = nombre,
                Especie = especie,
                Raza = raza,
                Sexo = sexo,
                FechaAlta = new DateTime(2026, 9, 15).AddDays(i),
                FechaNacimiento = grupo == 0 ? new DateOnly(2016 + i % 9, 1 + i % 12, 1 + i % 27) : null
            };

            if (grupo > 0)
            {
                animal.EsGrupo = true;
                animal.CantidadGrupo = grupo;
                animal.Caravana = $"UY-{(especie == "Oveja" ? "OV" : especie == "Vaca" ? "BV" : "CP")}-{3000 + i}";
            }
            else if (especie is "Perro" or "Gato")
            {
                animal.Microchip = $"858000000{100000 + i:000000}";
            }

            lista.Add(_animales.AgregarDemo(animal));
        }

        return lista;
    }

    // 198 consultas nuevas más las 2 de Toby y Canela hacen 200. El veterinario depende de la especie.
    private void CrearConsultas(List<Animal> animales)
    {
        const int Nuevas = ConsultasTotales - 2;
        var alergicos = new[] { "Penicilina", "Meloxicam", "Sulfas" };

        for (var k = 0; k < Nuevas; k++)
        {
            var animal = animales[k % animales.Count];
            var (profesionalId, autorId, autor, tabla) = animal.Especie switch
            {
                "Caballo" => (3, 4, "Dra. Valentina Acosta", Equinos),
                "Oveja" or "Vaca" or "Cabra" or "Cerdo" => (2, 3, "Dr. Martín Silveira", Campo),
                _ => (1, 2, "Dra. Lucía Ferreira", Pequenos)
            };

            var caso = tabla[(k / animales.Count + k) % tabla.Length];
            var haceDias = 40 + k * 7 % 360;
            var peso = animal.Especie switch
            {
                "Perro" => 8m + k % 25,
                "Gato" => 3m + k % 5,
                "Caballo" => 380m + k % 80,
                "Conejo" => 2m,
                "Ave" => 0.3m,
                _ => 45m + k % 400
            };
            var alergias = k % 23 == 0 ? alergicos[k / 23 % alergicos.Length] : "";

            _consultas.AgregarHistoriaDemo(animal.Id, profesionalId, autorId, autor, haceDias,
                caso.Motivo, caso.Diagnostico, caso.Tipo, caso.Tratamiento, peso, alergias);
        }
    }
}
