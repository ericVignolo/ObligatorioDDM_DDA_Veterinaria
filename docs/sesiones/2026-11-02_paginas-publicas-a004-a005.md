# Sesión 2026-11-02 (2) — Páginas públicas: profesionales, clínica, urgencias y políticas

Participantes: Eric (con Claude). Rama: `eric/a-004-a005-paginas-publicas`, creada desde `eric/a-002-inicio-vitalia` (PR #4 todavía sin mezclar) → PR a `desktop`.

## Contexto al iniciar

- Eric probó la rama del PR #4 (inicio de VITALIA): funciona.
- Se eligió hacer A-004 y A-005 antes del registro.

## Qué se hizo

**A-004 — Profesionales y clínica**
- `Models/Profesional.cs`, `Services/ProfesionalService.cs`: 3 veterinarios ficticios publicados y 1 sin publicar (no debe aparecer). Sin títulos ni afirmaciones indemostrables (RN-45).
- `Components/ProfesionalCard.razor`, `Pages/Profesionales.razor` (`/profesionales`) y `Pages/ProfesionalDetalle.razor` (`/profesionales/{Id}`) con horarios (RF-VIS-02, HU-VIS-02).
- `Pages/LaClinica.razor` (`/clinica`): presentación, contacto, guardia, horario, ubicación en texto, cobertura y responsable (RF-VIS-03, HU-VIS-03).
- `Clinica` suma razón social, presentación y cobertura.

**A-005 — Urgencias y políticas**
- `Pages/Urgencias.razor` (`/urgencias`): botón de guardia primero, sin formularios (HU-VIS-06); señales de alarma orientativas; tabla de niveles rojo/naranja/amarillo/verde según `ESPECIFICACION_ALERTAS_ASISTENCIA.md`; aviso de que no garantiza horario ni reemplaza al veterinario.
- `Models/DocumentoLegal.cs` y `Services/DocumentoLegalService.cs`: versión 1.0 de cada documento, vigente desde el 2026-11-02 (RF-VIS-10, PC-RF-01).
- `Pages/Privacidad.razor`, `Pages/Terminos.razor`, `Pages/Cookies.razor` con textos completos para VITALIA, alineados con la Ley 18.331 y con las decisiones del proyecto (P-RC-02, decisiones del 31/10).
- `Components/EncabezadoLegal.razor`: título, versión, fecha y aviso de clínica ficticia sin revisión jurídica.
- `Components/PieLegal.razor` en `MainLayout`: razón social, dirección, teléfono y enlaces legales en **todas** las páginas.
- Menú: Inicio, Servicios, Profesionales, La clínica, Urgencias. El inicio enlaza a Urgencias y a La clínica.

**Documentación**
- `AGENTS.md`: «Siguiente paso» ahora remite a la última bitácora.

## Decisiones

- Cookies: hoy solo hay cookies necesarias (antifalsificación de ASP.NET Core), por eso **no hay banner**. Si se agrega un mapa externo u otra herramienta no necesaria, se actualiza la política y se agrega el banner (RN-44).
- Los textos legales llevan un aviso visible: clínica ficticia, sin revisión de un abogado.
- Los términos fijan «mayor de edad» para crear una cuenta.

## Cómo probarlo

1. GitHub Desktop → *Fetch origin* → rama `eric/a-004-a005-paginas-publicas` → F5.
2. Revisar el menú nuevo y cada página: Profesionales (3, sin el Dr. Pereira), detalle con horarios, `/profesionales/4` dice «no disponible», La clínica, Urgencias (botón de guardia arriba, tabla de colores).
3. Abajo de cualquier página: pie con los 3 enlaces legales; cada documento muestra «Versión 1.0 · Vigente desde el 2 de noviembre de 2026».

> El código no se compiló desde el entorno de Claude. Si Visual Studio marca un error, se corrige en esta rama.

## Pendientes

- [ ] Eric: probar la rama y avisar errores.
- [ ] Diego: revisar y mezclar primero el PR #4 y después este.
- [ ] Marcar A-002, A-004 y A-005 en `PLAN_TAREAS_PROYECTO.md` al mezclar.
- [ ] Diego: decidir proveedor MySQL de DT-BD-02.
- [ ] Botones Reservar / Consultar en el detalle del servicio.
- [ ] Mapa en La clínica y en el inicio cuando se elija proveedor; actualizar Privacidad y Cookies en ese momento.
- [ ] Siguiente tarea grande: registro de cliente (A-006 a A-008).
- [ ] Borrar la copia vieja del repo en `C:\Users\eric0\source\repos\ObligatorioDDM_DDA_Veterinaria`.
