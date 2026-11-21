import type { DocumentoLegal } from '../types/DocumentoLegal';

// Textos generados desde Privacidad.razor y Terminos.razor de Blazor (versión 1.0), con los datos de la clínica ficticia.
// Si cambia el texto en Blazor, hay que regenerarlo acá. En la sección C los da la API (RF-VIS-10, RN-43).
// La política de cookies aplica solo al sitio web.
export const documentosLegales: DocumentoLegal[] = [
  {
    "codigo": "privacidad",
    "titulo": "Política de Privacidad",
    "version": "1.0",
    "vigenteDesde": "2026-11-02",
    "secciones": [
      {
        "titulo": "1. Quién es responsable de tus datos",
        "bloques": [
          {
            "tipo": "p",
            "texto": "El responsable de la base de datos es VITALIA Servicios Veterinarios S.A.S. (VITALIA), con domicilio en Calle 18 de Julio 1234, Nueva Palmira, Colonia, Uruguay. Podés escribirnos a contacto@vitalia.uy o llamar al +598 4544 0000."
          },
          {
            "tipo": "p",
            "texto": "Tratamos los datos personales conforme a la Ley N.º 18.331 de Protección de Datos Personales y sus normas complementarias."
          }
        ]
      },
      {
        "titulo": "2. Qué datos recolectamos",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Datos de tu cuenta: nombre, apellido, correo electrónico, contraseña (guardada cifrada), teléfono, documento de identidad y dirección del hogar."
          },
          {
            "tipo": "li",
            "texto": "Datos de tus animales: nombre, especie, raza, sexo, fecha de nacimiento, microchip y fotos que decidas cargar."
          },
          {
            "tipo": "li",
            "texto": "Historia clínica: consultas, diagnósticos, tratamientos, vacunas, estudios y consentimientos firmados."
          },
          {
            "tipo": "li",
            "texto": "Turnos y pagos: reservas, asistencias, cargos, cobros y comprobantes. No guardamos los datos completos de tu tarjeta."
          },
          {
            "tipo": "li",
            "texto": "Ubicación: solo cuando pedís asistencia a domicilio y lo aceptás en ese momento."
          },
          {
            "tipo": "li",
            "texto": "Datos técnicos: registros de acceso y seguridad necesarios para proteger la cuenta."
          },
          {
            "tipo": "p",
            "texto": "No pedimos datos que no necesitamos para la finalidad indicada. Los campos opcionales se marcan como tales."
          }
        ]
      },
      {
        "titulo": "3. Para qué los usamos",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Crear y proteger tu cuenta, y verificar tu correo."
          },
          {
            "tipo": "li",
            "texto": "Reservar, modificar y cancelar turnos, y enviarte recordatorios."
          },
          {
            "tipo": "li",
            "texto": "Atender a tus animales y llevar su historia clínica."
          },
          {
            "tipo": "li",
            "texto": "Avisarte de refuerzos de vacunas y desparasitaciones."
          },
          {
            "tipo": "li",
            "texto": "Coordinar y seguir una asistencia a domicilio."
          },
          {
            "tipo": "li",
            "texto": "Emitir cargos, registrar pagos y entregar comprobantes."
          },
          {
            "tipo": "li",
            "texto": "Cumplir obligaciones legales y atender tus consultas."
          },
          {
            "tipo": "p",
            "texto": "No usamos tus datos para publicidad de terceros ni los vendemos."
          }
        ]
      },
      {
        "titulo": "4. Con qué base los tratamos",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Tratamos tus datos con tu consentimiento, que se pide por separado para cada finalidad, y cuando son necesarios para prestarte el servicio que solicitaste o para cumplir una obligación legal. Las opciones voluntarias nunca aparecen marcadas de antemano y podés cambiarlas desde tu perfil."
          }
        ]
      },
      {
        "titulo": "5. Ubicación en la asistencia a domicilio",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Antes de compartir tu ubicación te informamos para qué se usa y quién la ve: el veterinario asignado y Recepción, solo para llegar a donde está el animal. El seguimiento termina cuando finaliza la asistencia. La ubicación del veterinario en camino se comparte con vos con el mismo límite."
          }
        ]
      },
      {
        "titulo": "6. Con quién los compartimos",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Solo con quienes necesitamos para prestar el servicio, y con los datos mínimos:"
          },
          {
            "tipo": "li",
            "texto": "proveedor de correo electrónico, para enviarte avisos y comprobantes;"
          },
          {
            "tipo": "li",
            "texto": "proveedor de notificaciones del celular, para los avisos de la aplicación;"
          },
          {
            "tipo": "li",
            "texto": "proveedor de pagos en línea, si elegís pagar por ese medio;"
          },
          {
            "tipo": "li",
            "texto": "proveedor de mapas, cuando usás la asistencia a domicilio."
          },
          {
            "tipo": "p",
            "texto": "Antes de incorporar cada proveedor actualizamos esta política con su nombre y los datos que recibe. Nunca enviamos información clínica a herramientas de análisis."
          }
        ]
      },
      {
        "titulo": "7. Cuánto tiempo los guardamos",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Historia clínica: no se borra, porque es un registro médico. Si eliminás tu cuenta, se conserva separada de tus datos personales durante el plazo que exija la normativa veterinaria."
          },
          {
            "tipo": "li",
            "texto": "Cuenta: mientras esté activa. Si la eliminás, borramos tus datos personales salvo los que debamos conservar por ley."
          },
          {
            "tipo": "li",
            "texto": "Pagos y comprobantes: durante el plazo que exija la normativa fiscal."
          },
          {
            "tipo": "li",
            "texto": "Ubicación de una asistencia: solo mientras dura la asistencia y el registro mínimo de su resultado."
          }
        ]
      },
      {
        "titulo": "8. Tus derechos",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Podés pedir acceso, rectificación, actualización, inclusión, supresión y oposición sobre tus datos, y descargar una copia. Escribinos a contacto@vitalia.uy desde el correo de tu cuenta. Respondemos en los plazos que fija la ley. Si no quedás conforme, podés presentar un reclamo ante la Unidad Reguladora y de Control de Datos Personales (URCDP)."
          }
        ]
      },
      {
        "titulo": "9. Seguridad",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Usamos conexión cifrada, contraseñas protegidas, accesos según el rol de cada integrante del equipo y registro de quién consulta la historia clínica. Si ocurre un incidente que afecte tus datos, te avisamos y actuamos según la normativa."
          }
        ]
      },
      {
        "titulo": "10. Cambios en esta política",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Cada versión tiene número y fecha. Si un cambio afecta lo que aceptaste, te lo informamos y, cuando corresponda, te pedimos una nueva aceptación. Guardamos el historial de versiones aceptadas."
          }
        ]
      }
    ]
  },
  {
    "codigo": "terminos",
    "titulo": "Términos y Condiciones",
    "version": "1.0",
    "vigenteDesde": "2026-11-02",
    "secciones": [
      {
        "titulo": "1. Objeto",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Estos términos regulan el uso del sitio y la aplicación de VITALIA, prestados por VITALIA Servicios Veterinarios S.A.S., para consultar servicios, reservar turnos, ver la historia clínica de tus animales, pedir asistencia y pagar prestaciones. Al crear una cuenta los aceptás. Podés navegar la información pública sin cuenta."
          }
        ]
      },
      {
        "titulo": "2. Tu cuenta",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Tenés que ser mayor de edad y dar datos reales."
          },
          {
            "tipo": "li",
            "texto": "La cuenta es personal. Cuidá tu contraseña y avisanos si creés que alguien más la usa."
          },
          {
            "tipo": "li",
            "texto": "Para reservar tenés que verificar tu correo."
          },
          {
            "tipo": "li",
            "texto": "Solo podés gestionar los animales de los que sos responsable."
          }
        ]
      },
      {
        "titulo": "3. Turnos",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Podés reservar con al menos 2 horas de anticipación y hasta 60 días hacia adelante."
          },
          {
            "tipo": "li",
            "texto": "Podés cancelar sin costo hasta 24 horas antes. Después, la cancelación se hace por Recepción."
          },
          {
            "tipo": "li",
            "texto": "Si no te presentás, el turno queda como ausente. Con 3 ausencias en 12 meses, las próximas reservas se hacen por Recepción."
          },
          {
            "tipo": "li",
            "texto": "Las cirugías pueden requerir una seña. Sin la seña, el turno queda pendiente y se cancela a las 24 horas."
          },
          {
            "tipo": "li",
            "texto": "El horario puede demorarse si se atiende una urgencia."
          }
        ]
      },
      {
        "titulo": "4. Precios y pagos",
        "bloques": [
          {
            "tipo": "li",
            "texto": "Los precios publicados son de referencia («desde»). El importe final depende del animal y de la atención realizada."
          },
          {
            "tipo": "li",
            "texto": "Los precios están en pesos uruguayos."
          },
          {
            "tipo": "li",
            "texto": "Podés pagar en línea, cuando esté disponible, o en Recepción."
          },
          {
            "tipo": "li",
            "texto": "Las correcciones de un cobro se hacen con nota de crédito."
          }
        ]
      },
      {
        "titulo": "5. Asistencia y urgencias",
        "bloques": [
          {
            "tipo": "li",
            "texto": "La asistencia a domicilio cubre hasta 30 km desde la clínica y tiene un costo según distancia y cantidad de animales."
          },
          {
            "tipo": "li",
            "texto": "El nivel de urgencia lo define el equipo y puede cambiar después de la evaluación."
          },
          {
            "tipo": "li",
            "texto": "Pedir asistencia no garantiza un horario de llegada. Si la vida del animal corre peligro, llamá a la guardia: +598 99 000 000."
          },
          {
            "tipo": "li",
            "texto": "La información del sitio es orientativa y no reemplaza la consulta con un veterinario."
          }
        ]
      },
      {
        "titulo": "6. Consentimientos clínicos",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Algunos procedimientos requieren un consentimiento informado específico. Se firma aparte, para cada procedimiento, y no se reemplaza por la aceptación de estos términos."
          }
        ]
      },
      {
        "titulo": "7. Uso correcto",
        "bloques": [
          {
            "tipo": "p",
            "texto": "No podés usar el servicio para fines ilegales, intentar acceder a datos de otras personas o animales, ni afectar su funcionamiento. Podemos suspender una cuenta que incumpla estos términos, avisándote el motivo."
          }
        ]
      },
      {
        "titulo": "8. Responsabilidad",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Trabajamos para que el servicio esté disponible y la información sea correcta, pero puede haber interrupciones. La atención veterinaria se rige por las normas de la profesión. No respondemos por el mal uso de la cuenta por parte de terceros cuando no se nos avisó."
          }
        ]
      },
      {
        "titulo": "9. Propiedad intelectual",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Los textos, imágenes y diseño del sitio pertenecen a VITALIA Servicios Veterinarios S.A.S. o se usan con autorización."
          }
        ]
      },
      {
        "titulo": "10. Cambios y ley aplicable",
        "bloques": [
          {
            "tipo": "p",
            "texto": "Cada versión de estos términos tiene número y fecha. Si un cambio es importante, te lo informamos antes de que rija. Estos términos se rigen por las leyes de la República Oriental del Uruguay."
          }
        ]
      },
      {
        "titulo": "11. Contacto",
        "bloques": [
          {
            "tipo": "p",
            "texto": "VITALIA Servicios Veterinarios S.A.S. · Calle 18 de Julio 1234, Nueva Palmira, Colonia · contacto@vitalia.uy · +598 4544 0000"
          }
        ]
      }
    ]
  }
];
