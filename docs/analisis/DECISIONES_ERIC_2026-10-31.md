# Decisiones de Eric — 2026-10-31

Estado: decididas y documentadas. No implementadas. Cada módulo las incorpora a su especificación cuando se aborde.

## Fase del proyecto (DT-ORG-04)

- Desde el 2026-10-31 el proyecto está en **fase de construcción (desarrollo)**, sección A. "Producción" se reserva para la publicación a usuarios reales (puertas de salida de `Implementacion_Politicas.txt`).
- La documentación se actualiza o se crea a medida que se desarrolla; no se reescribe en bloque.
- Eric aprobó el cambio de regla de `AGENTS.md` (puerta H0 por recorrido, DT-ORG-03) y el merge del PR #1.

## Delegado en Diego

- Base de datos, versiones de MySQL, proveedor de EF Core y Plan B de alcance.
- **Pendiente prioritario:** revisar DT-BD-02. Pomelo 9.0.0 es la última versión publicada y solo soporta EF Core 9, cuyo soporte termina el 2026-11-10; el proyecto Blazor usa .NET 10. Alternativas con EF Core 10: `MySql.EntityFrameworkCore` (Oracle, oficial) o `Microting.EntityFrameworkCore.MySql` (fork de Pomelo). El `SELECT … FOR UPDATE` no cambia. Diego decide antes de la sección C.

## Cobro

- Al pagar, el Cliente elige **pagar en línea** o **pagar en Recepción** (para quien no tiene tarjeta).
- CU "Cobrar consulta": Recepción registra el cobro presencial y emite el comprobante (RF-REC-05, RN-32 a RN-34).
- Pago en línea con Mercado Pago (RF-CLI-12): se define con el profesor antes de implementar.

## Internación

- **Recepción:** carga los datos administrativos del ingreso.
- **Veterinario:** ingreso clínico con box y plan terapéutico (RF-VET-09), evolución y alta (RF-VET-11, RN-25).

## Avisos de refuerzo ("alertas sanitarias")

- Son los avisos de refuerzo de vacunas y desparasitaciones (RF-CLI-07).
- Canales: bandeja de notificaciones de la app y correo.
- Anticipación: entre 3 y 5 días antes del vencimiento. Por defecto 5 días, configurable por el Administrador (RF-ADM-10).

## Parámetros de asistencia (por defecto, configurables por el Administrador, RF-ADM-13)

| Parámetro | Valor por defecto |
|---|---|
| Zona de cobertura | 30 km desde la clínica |
| Guardia | Un veterinario de guardia por turno, cargado por el Administrador |
| Espera de aceptación de un rojo antes de escalar a Recepción (RN-55) | 10 minutos |
| Tarifa de visita (RN-58) | Base fija + costo por km + adicional por animal extra; importes a definir |
| Horario de la clínica para verde, amarillo y naranja (RN-48) | Lun–vie 9–19, sáb 9–13 |

## Identidad y textos legales

- La clínica es ficticia: nombre, razón social y contacto inventados (RF-VIS-10).
- Privacidad, Términos y Cookies se escriben completos y versionados (RN-43).

## Proveedores (RNF-SEG-13)

- **Mapa:** Google Maps (uso gratuito, exige registrar tarjeta) u OpenStreetMap (gratis, sin tarjeta). Se decide al llegar al mapa.
- **Pagos:** Mercado Pago, pendiente de acordar con el profesor.
- **CAPTCHA:** no se usa salvo que haga falta. El formulario de contacto (RF-VIS-09) se protege con campo oculto (honeypot) y límite de envíos.
- **Notificaciones:** campana en escritorio y en móvil, push en el celular y correo (RF-CLI-10, RF-CLI-07).

## Conservación y microchip

- Historia clínica: nunca se borra (RN-14). Parámetro "años de conservación" para cuando exista un plazo legal real (RNF-LEG-04).
- Microchip: 15 dígitos (ISO 11784/11785), único (RN-41), validación configurable (RNF-LEG-05).
