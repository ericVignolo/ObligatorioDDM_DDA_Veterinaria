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

### Servicios (37)
- [[AdministracionService (API)]]
- [[AnimalService (API)]]
- [[CalculoAgenda (API)]]
- [[CarnetService (API)]]
- [[ClinicaService (API)]]
- [[CobroService (API)]]
- [[ConsentimientoService (API)]]
- [[ConsultaService (API)]]
- [[CuentaService (API)]]
- [[EnvioCorreo (API)]]
- [[IAdministracionService (API)]]
- [[IAnimalService (API)]]
- [[ICarnetService (API)]]
- [[IClinicaService (API)]]
- [[ICobroService (API)]]
- [[IConsentimientoService (API)]]
- [[IConsultaService (API)]]
- [[ICuentaService (API)]]
- [[IInventarioService (API)]]
- [[IPrestacionService (API)]]
- [[IProfesionalService (API)]]
- [[ISesionService (API)]]
- [[ITokenService (API)]]
- [[ITurnoService (API)]]
- [[InventarioService (API)]]
- [[Opciones (API)]]
- [[PrestacionService (API)]]
- [[ProfesionalService (API)]]
- [[ReglasCatalogo (API)]]
- [[ReglasClinicas (API)]]
- [[ReglasStock (API)]]
- [[Resultado (API)]]
- [[Seguridad (API)]]
- [[SesionService (API)]]
- [[TokenService (API)]]
- [[TurnoService (API)]]
- [[ValidacionDatos (API)]]

### Modelos (16)
- [[Animal (API)]]
- [[Asistencia (API)]]
- [[Cobro (API)]]
- [[CodigoCuenta (API)]]
- [[Comunicaciones (API)]]
- [[Configuracion (API)]]
- [[Consulta (API)]]
- [[Cuenta (API)]]
- [[DocumentoLegal (API)]]
- [[Internacion (API)]]
- [[Inventario (API)]]
- [[Prestacion (API)]]
- [[Profesional (API)]]
- [[Rol (API)]]
- [[SesionRefresco (API)]]
- [[Turno (API)]]

### Controladores (19)
- [[AdminSaludController (API)]] · `api/v1/admin`
- [[AdministracionController (API)]] · `api/v1/admin`
- [[AnimalesController (API)]] · `api/v1/animales`
- [[ClinicaController (API)]] · `api/v1/clinica`
- [[CobrosController (API)]] · `api/v1/cobros`
- [[CuentasController (API)]] · `api/v1/cuentas`
- [[EspeciesController (API)]] · `api/v1/especies`
- [[InventarioController (API)]] · `api/v1/admin/inventario`
- [[ManejadorConcurrencia (API)]]
- [[Problemas (API)]]
- [[ProfesionalesController (API)]] · `api/v1/profesionales`
- [[RecepcionController (API)]] · `api/v1/recepcion`
- [[SaludController (API)]] · `api/v1`
- [[ServiciosController (API)]] · `api/v1/servicios`
- [[SesionesController (API)]] · `api/v1/sesiones`
- [[StockVeterinarioController (API)]] · `api/v1/veterinario`
- [[TurnosController (API)]] · `api/v1/turnos`
- [[UsuarioActual (API)]]
- [[VeterinarioController (API)]] · `api/v1/veterinario`

### Repositorios (20)
- [[AdministracionRepository (API)]]
- [[AnimalRepository (API)]]
- [[ClinicaRepository (API)]]
- [[CobroRepository (API)]]
- [[ConsultaRepository (API)]]
- [[CuentaRepository (API)]]
- [[IAdministracionRepository (API)]]
- [[IAnimalRepository (API)]]
- [[IClinicaRepository (API)]]
- [[ICobroRepository (API)]]
- [[IConsultaRepository (API)]]
- [[ICuentaRepository (API)]]
- [[IPrestacionRepository (API)]]
- [[IProfesionalRepository (API)]]
- [[ISaludRepository (API)]]
- [[ITurnoRepository (API)]]
- [[PrestacionRepository (API)]]
- [[ProfesionalRepository (API)]]
- [[SaludRepository (API)]]
- [[TurnoRepository (API)]]

### DTOs (11)
- [[AdminDtos (API)]]
- [[AnimalDtos (API)]]
- [[ClinicaReadDto (API)]]
- [[CobroDtos (API)]]
- [[ConsultaDtos (API)]]
- [[CuentaDtos (API)]]
- [[PaginaDto (API)]]
- [[ProfesionalReadDto (API)]]
- [[SaludDtos (API)]]
- [[ServicioReadDto (API)]]
- [[TurnoDtos (API)]]

### Datos (3)
- [[AppDbContext (API)]]
- [[ConfiguracionModulos (API)]]
- [[Transacciones (API)]]

## Mobile — [[Carpeta mobile]]

Todavía sin código.
