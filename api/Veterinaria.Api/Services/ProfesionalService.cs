using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Equipo profesional del área pública (RF-VIS-02, HU-VIS-02).
public class ProfesionalService : IProfesionalService
{
    private static readonly string[] NombresDias = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
    private static readonly Func<Prestacion, bool> ServicioPublico = ReglasCatalogo.EsPublica.Compile();

    private readonly IProfesionalRepository _repository;

    public ProfesionalService(IProfesionalRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginaDto<ProfesionalReadDto>> ListarPublicosAsync(int page, int pageSize)
    {
        var total = await _repository.ContarPublicosAsync();
        var profesionales = await _repository.ObtenerPublicosAsync((page - 1) * pageSize, pageSize);

        return new PaginaDto<ProfesionalReadDto>
        {
            Items = profesionales.Select(p => Completar(new ProfesionalReadDto(), p)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<ProfesionalDetalleDto?> ObtenerPublicoAsync(int id)
    {
        var profesional = await _repository.ObtenerPublicoPorIdAsync(id);
        if (profesional == null)
        {
            return null;
        }

        var detalle = Completar(new ProfesionalDetalleDto(), profesional);
        detalle.Horario = ArmarHorario(profesional.Franjas);
        return detalle;
    }

    // Arma el horario semanal por día, de lunes a domingo; solo los días con jornada.
    public static List<DiaHorarioDto> ArmarHorario(IEnumerable<FranjaHoraria> franjas)
    {
        return franjas
            .GroupBy(f => f.Dia)
            .Where(g => g.Any(f => f.Tipo == "Jornada"))
            .OrderBy(g => ((int)g.Key + 6) % 7) // lunes primero
            .Select(g => new DiaHorarioDto
            {
                Dia = NombresDias[(int)g.Key],
                Jornadas = Tramos(g, "Jornada"),
                Descansos = Tramos(g, "Descanso")
            })
            .ToList();
    }

    private static List<TramoHorarioDto> Tramos(IEnumerable<FranjaHoraria> franjas, string tipo)
    {
        return franjas
            .Where(f => f.Tipo == tipo)
            .OrderBy(f => f.Inicio)
            .Select(f => new TramoHorarioDto { Desde = f.Inicio.ToString(@"hh\:mm"), Hasta = f.Fin.ToString(@"hh\:mm") })
            .ToList();
    }

    // Mapeo explícito entidad → DTO. No copia matrícula, cuenta ni ausencias.
    private static T Completar<T>(T dto, Profesional p) where T : ProfesionalReadDto
    {
        dto.Id = p.Id;
        dto.Nombre = p.Nombre;
        dto.Especialidad = p.Especialidad;
        dto.Descripcion = p.Descripcion;
        dto.Especies = p.Especies
            .Where(pe => pe.Especie != null)
            .Select(pe => pe.Especie!.Nombre)
            .OrderBy(n => n)
            .ToList();
        dto.Servicios = p.Prestaciones
            .Where(pp => pp.Prestacion != null && ServicioPublico(pp.Prestacion))
            .OrderBy(pp => pp.PrestacionId)
            .Select(pp => new ServicioResumenDto { Id = pp.PrestacionId, Nombre = pp.Prestacion!.Nombre })
            .ToList();
        return dto;
    }
}
