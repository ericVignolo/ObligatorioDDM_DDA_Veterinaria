// Cuenta de Cliente de demostración (mismos campos que Models/Cuenta.cs de Blazor).
// En la sección C la crea y guarda la API; la app nunca guarda la contraseña ni los códigos.
export type Cuenta = {
  id: number;
  nombre: string;
  apellido: string;
  correo: string;
  telefono: string;
  tipoDocumento: string;
  numeroDocumento: string;
  calle: string;
  numeroPuerta: string;
  apartamento: string;
  localidad: string;
  departamento: string;
  referencias: string;

  // SOLO DEMOSTRACIÓN: en memoria y en claro. La API guarda solo el derivado (RNF-SEG-01).
  contrasena: string;
  verificada: boolean;
  fechaCreacion: number;

  // Aceptaciones obligatorias con su versión (RN-43, RNF-LEG-03).
  versionTerminosAceptada: string;
  versionPrivacidadAceptada: string;
  fechaAceptacion: number;

  // Verificación por correo (D-REG-02).
  codigo: string;
  codigoVence: number;
  intentosRestantes: number;
  reenvios: number[];

  // Recuperación de contraseña (D-REG-03): mismos límites que la verificación.
  codigoRecuperacion: string;
  codigoRecuperacionVence: number;
  intentosRecuperacion: number;
  pedidosRecuperacion: number[];
};

// Datos del formulario de registro (D-REG-01, D-REG-04).
export type DatosRegistro = {
  nombre: string;
  apellido: string;
  correo: string;
  telefono: string;
  tipoDocumento: string;
  numeroDocumento: string;
  calle: string;
  numeroPuerta: string;
  apartamento: string;
  localidad: string;
  departamento: string;
  referencias: string;
  contrasena: string;
  repetirContrasena: string;
  aceptaTerminos: boolean;
  aceptaPrivacidad: boolean;
};

// Datos editables de «Mi cuenta». El correo se cambia aparte y el documento, en Recepción (RN-42).
export type DatosPerfil = Pick<
  DatosRegistro,
  'nombre' | 'apellido' | 'telefono' | 'calle' | 'numeroPuerta' | 'apartamento' | 'localidad' | 'departamento' | 'referencias'
>;

export type Resultado = { exito: boolean; mensaje: string };

export type Errores<T> = Partial<Record<keyof T, string>>;
