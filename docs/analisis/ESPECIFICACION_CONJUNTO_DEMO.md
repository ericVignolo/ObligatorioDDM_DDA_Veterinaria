# Especificación del conjunto grande de datos de demostración (G-009)

Clasificación: requerimiento funcional de demostración (D-DEM-03 de `ESPECIFICACION_RECORRIDOS_DEMO.md`). Los datos son ficticios y reproducibles. La advertencia sobre los límites de los prototipos está en la sección 3.1 de `CONTEXTO_BASE.md`.

## Alcance
`ConjuntoDemoService` (escritorio, sección A) suma al conjunto de partida (Ana, Pedro, 4 animales, 2 consultas) lo necesario para llegar a **30 clientes, 50 animales y 200 consultas históricas firmadas**, con casos de caballo y oveja. No cambia ninguna regla de negocio. En la sección C lo reemplaza el seed de la base de datos.

## Reglas
| Id | Regla |
|---|---|
| CD-01 | Mientras no se llame a `Cargar()`, la demostración queda exactamente como antes (2 clientes, 4 animales, 2 consultas). |
| CD-02 | Después de `Cargar()` hay 30 cuentas, 50 animales y 200 consultas firmadas (`ClientesTotales`, `AnimalesTotales`, `ConsultasTotales`). |
| CD-03 | `Cargar()` es idempotente: la segunda llamada no duplica nada y devuelve éxito. |
| CD-04 | Hay al menos 5 caballos y al menos 3 majadas de ovejas; cada oveja es un grupo (`EsGrupo`, más de 1 animal) con caravana. |
| CD-05 | Ids, correos y microchips son únicos; cada microchip tiene 15 dígitos; cada animal apunta a un responsable existente; todos los clientes están verificados. |
| CD-06 | Cada consulta histórica está firmada, es anterior a hoy, tiene motivo y diagnóstico, y su profesional atiende la especie del animal (Ferreira: pequeños; Silveira: vacas, ovejas, cabras, cerdos; Acosta: caballos). |
| CD-07 | Los clientes nuevos (`cliente01.demo@vitalia.uy` a `cliente28.demo@vitalia.uy`) ingresan con la contraseña de demostración del personal. |
| CD-08 | Cada animal nuevo tiene al menos una consulta en su historia; los animales de Ana y Pedro no cambian. |

## Datos
- 28 clientes nuevos (Id 200 a 227) en Colonia y Soriano.
- 46 animales nuevos: 18 perros, 12 gatos, 6 caballos, 4 majadas de ovejas, 2 rodeos de vacas, 1 grupo de cabras, 1 conejo, 1 ave y 1 cerdo.
- 198 consultas nuevas con 10 motivos de pequeños animales, 6 de campo y 6 de equinos; algunas con alergia registrada (Penicilina, Meloxicam, Sulfas) para el aviso de RN-17.
- Reproducible: no usa azar; cada dato sale de su posición.

## Decisión abierta para Diego (D-DEM-04)
Cómo se activa el conjunto. Eric (2026-11-17) dejó la decisión a Diego. Opciones:
1. **Ajuste** `Demo:ConjuntoCompleto` en `appsettings`: apagado, nada cambia; para la presentación se prende. (Recomendada por Claude: no toca pruebas.)
2. **Siempre cargado** como datos de partida: la agenda y las listas se ven llenas, pero hay que reajustar varias pruebas.
3. **Botón «Cargar conjunto completo»** en `/demo`: se elige en el momento, sin tocar el arranque.

Hasta que se decida, el servicio está registrado en `Program.cs` pero nadie lo llama.

## Pruebas
`ConjuntoDemoTests` (CD-01 a CD-08).
