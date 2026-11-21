using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Agenda y turnos (CU-CLI-02, A-014 a A-018, RF-REC-01 a 04, P-RC-02). La cuenta sale siempre del token.
public interface ITurnoService
{
    // ---------- Cliente ----------
    Task<Resultado<DisponibilidadDto>> DisponibilidadClienteAsync(int cuentaId, int profesionalId, int prestacionId, int animalId, DateOnly fecha);
    Task<Resultado<TurnoReadDto>> ReservarAsync(int cuentaId, ReservaCreateDto datos);
    Task<PaginaDto<TurnoReadDto>> MisTurnosAsync(int cuentaId, bool proximos, int page, int pageSize);
    Task<Resultado<TurnoReadDto>> ObtenerMioAsync(int cuentaId, int turnoId);
    Task<Resultado<TurnoReadDto>> CancelarAsync(int cuentaId, int turnoId, string motivo);
    Task<Resultado<TurnoReadDto>> ReprogramarAsync(int cuentaId, int turnoId, DateTime nuevoInicio);

    // ---------- Recepción ----------
    Task<PaginaDto<ClienteBusquedaDto>> BuscarClientesAsync(string texto, int page, int pageSize);
    Task<Resultado<DisponibilidadDto>> DisponibilidadMostradorAsync(int clienteId, int profesionalId, int prestacionId, int animalId, DateOnly fecha);
    Task<List<TurnoReadDto>> AgendaDelDiaAsync(DateOnly fecha, int? profesionalId, string? estado);
    Task<Resultado<TurnoReadDto>> ReservarPorMostradorAsync(int personaId, string rol, ReservaMostradorDto datos);
    Task<Resultado<TurnoReadDto>> RegistrarLlegadaAsync(int turnoId);
    Task<Resultado<TurnoReadDto>> RegistrarAusenciaAsync(int turnoId);
    Task<Resultado<TurnoReadDto>> CancelarPorRecepcionAsync(int turnoId, string motivo);

    // ---------- Veterinario ----------
    Task<Resultado<List<TurnoReadDto>>> AgendaDelVeterinarioAsync(int cuentaId, DateOnly fecha);
}
