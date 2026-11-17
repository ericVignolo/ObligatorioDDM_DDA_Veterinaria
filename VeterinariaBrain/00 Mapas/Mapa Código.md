---
tipo: mapa
tags: [mapa]
generado: true
---
# Mapa Código

Volver a [[Inicio]]. Una sección por aplicación; cada nota lista qué usa, quién la usa y, si existe, su equivalente en las otras aplicaciones.

## Desktop — [[Carpeta desktop]]

### Páginas (46)
- [[Acceso]] · `/acceso`
- [[AdminAuditoria]] · `/panel/admin/auditoria`
- [[AdminDerechos]] · `/panel/admin/derechos`
- [[AdminHorarios]] · `/panel/admin/horarios`
- [[AdminIndicadores]] · `/panel/admin/indicadores`
- [[AdminParametros]] · `/panel/admin/parametros`
- [[AdminPersonal]] · `/panel/admin/personal`
- [[AdminServicios]] · `/panel/admin/servicios`
- [[AgendaRecepcion]] · `/panel/agenda`
- [[AgendaVeterinario]] · `/panel/mi-agenda`
- [[AnimalDetalle]] · `/animales/{Id:int}`
- [[AnimalEditar]] · `/animales/{Id:int}/editar`
- [[AnimalNuevo]] · `/animales/nuevo`
- [[Animales]] · `/animales`
- [[AsistenciaDetalle]] · `/panel/asistencia/{Id:int}`
- [[Asistencias]] · `/panel/asistencia`
- [[AtencionConsulta]] · `/panel/consulta/{TurnoId:int}`
- [[Cobros]] · `/panel/cobros`
- [[Comprobante]] · `/panel/comprobantes/{Id:int}`
- [[Comunicaciones]] · `/panel/comunicaciones`
- [[Cookies]] · `/cookies`
- [[Demostracion]] · `/demo`
- [[FirmarConsentimiento]] · `/consentimiento/{TurnoId:int}`
- [[HistoriaAnimal]] · `/animales/{Id:int}/historia`
- [[HistoriaClinica]] · `/panel/historia/{AnimalId:int}`
- [[Home]] · `/`
- [[Ingresar]] · `/ingresar`
- [[InternacionDetalle]] · `/panel/internacion/{Id:int}`
- [[Internaciones]] · `/panel/internacion`
- [[Inventario]] · `/panel/inventario`
- [[LaClinica]] · `/clinica`
- [[MiCuenta]] · `/cuenta`
- [[PanelInicio]] · `/panel`
- [[PrestacionDetalle]] · `/servicios/{Id:int}`
- [[Privacidad]] · `/privacidad`
- [[ProfesionalDetalle]] · `/profesionales/{Id:int}`
- [[Profesionales]] · `/profesionales`
- [[Recuperar]] · `/recuperar`
- [[Registro]] · `/registro`
- [[Reservar]] · `/reservar`
- [[Restablecer]] · `/restablecer`
- [[Servicios]] · `/servicios`
- [[Terminos]] · `/terminos`
- [[Turnos]] · `/turnos`
- [[Urgencias]] · `/urgencias`
- [[Verificar]] · `/verificar`

### Componentes (22)
- [[AnimalCard]]
- [[AnimalForm]]
- [[Atajo]]
- [[BarraSesion]]
- [[CarnetSanitario]]
- [[EncabezadoLegal]]
- [[EstadoConsentimiento]]
- [[EstadoRefuerzo]]
- [[EstadoTurno]]
- [[MainLayout]]
- [[MisComunicaciones]]
- [[MisDerechos]]
- [[NavMenu]]
- [[NivelAsistencia]]
- [[PasosReserva]]
- [[PieLegal]]
- [[PrestacionCard]]
- [[ProfesionalCard]]
- [[RegistroClinico]]
- [[RequierePersonal]]
- [[RequiereSesion]]
- [[TurnoMostrador]]

### Servicios (25)
- [[AdministracionService]]
- [[AgendaService]]
- [[AnimalService]]
- [[AsistenciaService]]
- [[AuditoriaService]]
- [[AvisosRefuerzoService]]
- [[BitacoraService]]
- [[ClinicaService]]
- [[CobroService]]
- [[ComunicacionesService]]
- [[ConsentimientoService]]
- [[ConsultaService]]
- [[CuentaService]]
- [[DerechosService]]
- [[DocumentoLegalService]]
- [[IndicadoresService]]
- [[IntencionService]]
- [[InternacionService]]
- [[InventarioService]]
- [[PersonalService]]
- [[PrestacionService]]
- [[ProfesionalService]]
- [[RecorridosService]]
- [[SesionService]]
- [[VacunaService]]

### Modelos (74)
- [[AdmisionFormulario]]
- [[AjusteFormulario]]
- [[AlertaStock]]
- [[AltaFormulario]]
- [[Animal]]
- [[AnimalFormulario]]
- [[AnulacionFormulario]]
- [[AplicacionFormulario]]
- [[AplicacionSanitaria]]
- [[Articulo]]
- [[Ausencia]]
- [[AusenciaFormulario]]
- [[Box]]
- [[CambioContrasenaFormulario]]
- [[CambioCorreoFormulario]]
- [[CambioNivelFormulario]]
- [[Cargo]]
- [[Clinica]]
- [[CobroFormulario]]
- [[CodigoVerificacion]]
- [[Comunicacion]]
- [[Consentimiento]]
- [[ConsentimientoFormulario]]
- [[Consulta]]
- [[ConsultaFormulario]]
- [[ConsumoFormulario]]
- [[Cuenta]]
- [[DocumentoConsentimiento]]
- [[DocumentoLegal]]
- [[Enmienda]]
- [[EnmiendaFormulario]]
- [[EventoAsistencia]]
- [[FirmaConsulta]]
- [[FranjaHoraria]]
- [[Indicadores]]
- [[IngresoClinicoFormulario]]
- [[IngresoLoteFormulario]]
- [[InicioSesion]]
- [[Internacion]]
- [[ItemCarnet]]
- [[LecturaHistoria]]
- [[LoteProducto]]
- [[MotivoFormulario]]
- [[MotivoOpcionalFormulario]]
- [[MovimientoStock]]
- [[NivelUrgencia]]
- [[NotaCredito]]
- [[NovedadFormulario]]
- [[NovedadInternacion]]
- [[NuevaContrasena]]
- [[Pago]]
- [[PerfilFormulario]]
- [[Personal]]
- [[PersonalFormulario]]
- [[PoliticaConservacion]]
- [[PreferenciaComunicacion]]
- [[Prestacion]]
- [[PrestacionFormulario]]
- [[ProductoSanitario]]
- [[Profesional]]
- [[Recorrido]]
- [[RegistroAuditoria]]
- [[RegistroCliente]]
- [[ReservaStock]]
- [[ResultadoOperacion]]
- [[SolicitudAsistencia]]
- [[SolicitudDerecho]]
- [[SolicitudRecuperacion]]
- [[SolicitudTelefonicaFormulario]]
- [[TarifaFormulario]]
- [[TarifaPrestacion]]
- [[TipoComunicacion]]
- [[Turno]]
- [[ValidacionDatos]]

## Api — [[Carpeta api]]

### Modelos (9)
- [[Animal (API)]]
- [[CodigoCuenta (API)]]
- [[Configuracion (API)]]
- [[Cuenta (API)]]
- [[DocumentoLegal (API)]]
- [[Prestacion (API)]]
- [[Profesional (API)]]
- [[Rol (API)]]
- [[Turno (API)]]

### Datos (1)
- [[AppDbContext (API)]]

## Mobile — [[Carpeta mobile]]

Todavía sin código.
