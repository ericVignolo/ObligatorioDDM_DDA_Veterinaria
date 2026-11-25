import { animalesDemo } from '../data/animalesDemo';
import type { Animal, DatosAnimal } from '../types/Animal';
import type { Errores, Resultado } from '../types/Cuenta';

// Animales de demostración en memoria (sección B): se pierden al cerrar la app. Misma lógica que AnimalService de Blazor.
// En la sección C se reemplaza por GET/POST/PUT /api/v1/animales; la API valida la responsabilidad (RN-18)
// y el microchip único (RN-41).
const animales: Animal[] = animalesDemo.map((a) => ({ ...a }));
let ultimoId = Math.max(...animales.map((a) => a.id));

export const especies = ['Perro', 'Gato', 'Caballo', 'Oveja', 'Vaca', 'Cerdo', 'Cabra', 'Ave', 'Conejo', 'Reptil', 'Otra'];
export const sexos = ['Macho', 'Hembra', 'Sin determinar'];

export const datosVacios = (): DatosAnimal => ({
  nombre: '',
  especie: '',
  raza: '',
  sexo: '',
  fechaNacimiento: '',
  microchip: '',
  caravana: '',
  esGrupo: false,
  cantidadGrupo: '',
  senasParticulares: '',
  antecedentes: '',
});

// RN-18: solo los animales del responsable, por nombre.
export function obtenerDe(cuentaId: number): Animal[] {
  return animales.filter((a) => a.responsableId === cuentaId).sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));
}

// Devuelve undefined si no existe o si no es de ese responsable, sin distinguir los casos (RN-18).
export function obtenerPorId(id: number, cuentaId: number): Animal | undefined {
  return animales.find((a) => a.id === id && a.responsableId === cuentaId);
}

// Convierte DD/MM/AAAA en AAAA-MM-DD. Devuelve null si la fecha no existe (por ejemplo, 31/02).
export function leerFecha(texto: string): string | null {
  const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(texto.trim());
  if (!m) return null;
  const [dia, mes, anio] = [Number(m[1]), Number(m[2]), Number(m[3])];
  const fecha = new Date(anio, mes - 1, dia);
  if (fecha.getFullYear() !== anio || fecha.getMonth() !== mes - 1 || fecha.getDate() !== dia) return null;
  return `${anio}-${String(mes).padStart(2, '0')}-${String(dia).padStart(2, '0')}`;
}

// AAAA-MM-DD → DD/MM/AAAA.
export function mostrarFecha(iso: string | null): string {
  if (!iso) return '';
  const [anio, mes, dia] = iso.split('-');
  return `${dia}/${mes}/${anio}`;
}

// Mismas reglas que AnimalFormulario de Blazor (A-013, CU-CLI-01).
export function validar(datos: DatosAnimal): Errores<DatosAnimal> {
  const e: Errores<DatosAnimal> = {};
  const nombre = datos.nombre.trim();
  if (!nombre) e.nombre = 'Ingresá el nombre.';
  else if (nombre.length > 60) e.nombre = 'El nombre puede tener hasta 60 caracteres.';
  if (!datos.especie) e.especie = 'Elegí la especie.';
  if (datos.raza.trim().length > 60) e.raza = 'La raza puede tener hasta 60 caracteres.';
  if (!datos.sexo) e.sexo = 'Elegí el sexo.';

  if (datos.esGrupo) {
    const cantidad = datos.cantidadGrupo.trim();
    if (!cantidad) e.cantidadGrupo = 'Indicá cuántos animales tiene el grupo.';
    else if (!/^\d+$/.test(cantidad) || Number(cantidad) < 2 || Number(cantidad) > 10000)
      e.cantidadGrupo = 'Un grupo tiene entre 2 y 10.000 animales.';
  } else {
    if (datos.fechaNacimiento.trim()) {
      const iso = leerFecha(datos.fechaNacimiento);
      const hoy = new Date();
      const hoyIso = `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}-${String(hoy.getDate()).padStart(2, '0')}`;
      if (!iso) e.fechaNacimiento = 'La fecha no es válida. Escribila como DD/MM/AAAA.';
      else if (iso > hoyIso) e.fechaNacimiento = 'La fecha de nacimiento no puede ser futura.';
    }
    // Microchip: 15 dígitos (ISO 11784/11785), opcional.
    if (datos.microchip.trim() && !/^\d{15}$/.test(datos.microchip.trim())) e.microchip = 'El microchip tiene 15 números.';
  }

  if (datos.caravana.trim() && !/^[A-Za-z0-9-]{1,20}$/.test(datos.caravana.trim()))
    e.caravana = 'La caravana puede tener hasta 20 letras, números o guiones.';
  if (datos.senasParticulares.trim().length > 300) e.senasParticulares = 'Las señas particulares pueden tener hasta 300 caracteres.';
  if (datos.antecedentes.trim().length > 500) e.antecedentes = 'Los antecedentes pueden tener hasta 500 caracteres.';
  return e;
}

export function crear(datos: DatosAnimal, cuentaId: number): Resultado & { id?: number } {
  if (microchipEnUso(datos, 0)) return microchipRepetido();
  ultimoId++;
  const animal = { id: ultimoId, responsableId: cuentaId, fechaAlta: Date.now() } as Animal;
  copiarDatos(datos, animal);
  animales.push(animal);
  return { exito: true, mensaje: `${animal.nombre} quedó registrado.`, id: animal.id };
}

export function editar(id: number, datos: DatosAnimal, cuentaId: number): Resultado {
  // CU-CLI-01 A4: si ya no es responsable, se rechaza.
  const animal = obtenerPorId(id, cuentaId);
  if (!animal) return { exito: false, mensaje: 'No encontramos ese animal entre los tuyos.' };
  if (microchipEnUso(datos, id)) return microchipRepetido();
  copiarDatos(datos, animal);
  return { exito: true, mensaje: 'Los cambios quedaron guardados.' };
}

// Prepara el formulario de edición con los datos actuales.
export function paraEditar(animal: Animal): DatosAnimal {
  return {
    nombre: animal.nombre,
    especie: animal.especie,
    raza: animal.raza,
    sexo: animal.sexo,
    fechaNacimiento: mostrarFecha(animal.fechaNacimiento),
    microchip: animal.microchip,
    caravana: animal.caravana,
    esGrupo: animal.esGrupo,
    cantidadGrupo: animal.cantidadGrupo ? String(animal.cantidadGrupo) : '',
    senasParticulares: animal.senasParticulares,
    antecedentes: animal.antecedentes,
  };
}

// A-013: ejemplos sintéticos de caballo y oveja para probar la app sin escribir todo a mano.
export function cargarEjemplos(cuentaId: number): void {
  crear(
    { ...datosVacios(), nombre: 'Lucero', especie: 'Caballo', raza: 'Criollo', sexo: 'Macho', fechaNacimiento: '12/09/2018', microchip: '858000000123456', senasParticulares: 'Tordillo, estrella blanca en la frente.', antecedentes: 'Rehabilitación de tendón en 2025.' },
    cuentaId,
  );
  crear(
    { ...datosVacios(), nombre: 'Majada del potrero norte', especie: 'Oveja', raza: 'Corriedale', sexo: 'Hembra', caravana: 'UY-OV-2041', esGrupo: true, cantidadGrupo: '40', senasParticulares: 'Señal en oreja izquierda.' },
    cuentaId,
  );
}

function microchipEnUso(datos: DatosAnimal, idActual: number): boolean {
  const microchip = datos.esGrupo ? '' : datos.microchip.trim();
  return microchip !== '' && animales.some((a) => a.microchip === microchip && a.id !== idActual);
}

// CU-CLI-01 A1: no se revela de quién es el otro animal.
function microchipRepetido(): Resultado {
  return { exito: false, mensaje: 'Ese microchip ya figura en el sistema. Consultá en la clínica para revisarlo.' };
}

function copiarDatos(datos: DatosAnimal, animal: Animal): void {
  animal.nombre = datos.nombre.trim();
  animal.especie = datos.especie;
  animal.raza = datos.raza.trim();
  animal.sexo = datos.sexo;
  animal.esGrupo = datos.esGrupo;
  // Un grupo no tiene fecha de nacimiento ni microchip individual.
  animal.fechaNacimiento = datos.esGrupo ? null : leerFecha(datos.fechaNacimiento);
  animal.microchip = datos.esGrupo ? '' : datos.microchip.trim();
  animal.caravana = datos.caravana.trim();
  animal.cantidadGrupo = datos.esGrupo ? Number(datos.cantidadGrupo.trim()) : null;
  animal.senasParticulares = datos.senasParticulares.trim();
  animal.antecedentes = datos.antecedentes.trim();
}
