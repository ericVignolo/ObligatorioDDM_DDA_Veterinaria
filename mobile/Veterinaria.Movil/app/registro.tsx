import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Boton } from '../components/Boton';
import { Campo } from '../components/Campo';
import { Casilla } from '../components/Casilla';
import { Pantalla } from '../components/Pantalla';
import { Selector } from '../components/Selector';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../components/Textos';
import { useAviso } from '../context/AvisoContext';
import { departamentos, registrar } from '../services/cuentaService';
import type { DatosRegistro, Errores } from '../types/Cuenta';
import { correoValido, documentoValido, normalizarTelefono } from '../utils/validacion';

// Cédula uruguaya, pasaporte u otro documento extranjero (D-REG-01): los dos últimos comparten el formato alfanumérico.
const TIPOS_DOCUMENTO = ['CI', 'Pasaporte', 'Otro'];

const inicial: DatosRegistro = {
  nombre: '',
  apellido: '',
  correo: '',
  telefono: '',
  tipoDocumento: 'CI',
  numeroDocumento: '',
  calle: '',
  numeroPuerta: '',
  apartamento: '',
  localidad: '',
  departamento: '',
  referencias: '',
  contrasena: '',
  repetirContrasena: '',
  aceptaTerminos: false, // sin marcar de antemano (RN-43)
  aceptaPrivacidad: false,
};

// Mismos mensajes que las anotaciones de RegistroCliente.cs de Blazor.
function validar(d: DatosRegistro): Errores<DatosRegistro> {
  const e: Errores<DatosRegistro> = {};
  if (!d.nombre.trim()) e.nombre = 'Ingresá tu nombre.';
  else if (d.nombre.trim().length > 60) e.nombre = 'El nombre puede tener hasta 60 caracteres.';
  if (!d.apellido.trim()) e.apellido = 'Ingresá tu apellido.';
  else if (d.apellido.trim().length > 60) e.apellido = 'El apellido puede tener hasta 60 caracteres.';
  if (!d.correo.trim()) e.correo = 'Ingresá tu correo electrónico.';
  else if (!correoValido(d.correo)) e.correo = 'El correo no tiene un formato válido.';
  if (!d.telefono.trim()) e.telefono = 'Ingresá un teléfono.';
  else if (normalizarTelefono(d.telefono) === null) e.telefono = 'El teléfono no es válido. Ejemplo: 099 123 456 o +598 99 123 456.';
  if (!d.numeroDocumento.trim()) e.numeroDocumento = 'Ingresá el número de documento.';
  else if (!documentoValido(d.tipoDocumento, d.numeroDocumento)) {
    e.numeroDocumento =
      d.tipoDocumento === 'CI'
        ? 'La cédula no es válida. Revisá los números y el dígito verificador.'
        : 'El documento debe tener entre 5 y 20 letras o números.';
  }
  if (!d.calle.trim()) e.calle = 'Ingresá la calle.';
  else if (d.calle.trim().length > 100) e.calle = 'La calle puede tener hasta 100 caracteres.';
  if (!d.numeroPuerta.trim()) e.numeroPuerta = 'Ingresá el número de puerta.';
  else if (d.numeroPuerta.trim().length > 10) e.numeroPuerta = 'El número puede tener hasta 10 caracteres.';
  if (d.apartamento.trim().length > 10) e.apartamento = 'El apartamento puede tener hasta 10 caracteres.';
  if (!d.localidad.trim()) e.localidad = 'Ingresá la localidad.';
  else if (d.localidad.trim().length > 60) e.localidad = 'La localidad puede tener hasta 60 caracteres.';
  if (!d.departamento) e.departamento = 'Elegí el departamento.';
  if (d.referencias.trim().length > 200) e.referencias = 'Las referencias pueden tener hasta 200 caracteres.';
  if (!d.contrasena) e.contrasena = 'Ingresá una contraseña.';
  else if (d.contrasena.length < 8 || d.contrasena.length > 64) e.contrasena = 'La contraseña debe tener entre 8 y 64 caracteres.';
  if (!d.repetirContrasena) e.repetirContrasena = 'Repetí la contraseña.';
  else if (d.repetirContrasena !== d.contrasena) e.repetirContrasena = 'Las contraseñas no coinciden.';
  if (!d.aceptaTerminos) e.aceptaTerminos = 'Para crear la cuenta tenés que aceptar los Términos y Condiciones.';
  if (!d.aceptaPrivacidad) e.aceptaPrivacidad = 'Para crear la cuenta tenés que aceptar la Política de Privacidad.';
  return e;
}

// Registro de Cliente (A-006, B-006): siete datos obligatorios (D-REG-01) y dos aceptaciones separadas (D-REG-04).
export default function Registro() {
  const router = useRouter();
  const avisar = useAviso();
  const [datos, setDatos] = useState<DatosRegistro>(inicial);
  const [errores, setErrores] = useState<Errores<DatosRegistro>>({});
  const [mensaje, setMensaje] = useState('');

  const cambiar = <K extends keyof DatosRegistro>(campo: K, valor: DatosRegistro[K]) => {
    setDatos((d) => ({ ...d, [campo]: valor }));
    if (errores[campo]) setErrores((e) => ({ ...e, [campo]: undefined }));
  };

  const enviar = () => {
    const e = validar(datos);
    setErrores(e);
    setMensaje('');
    if (Object.keys(e).length > 0) {
      setMensaje('Revisá los campos marcados.');
      return;
    }
    const r = registrar(datos);
    if (!r.exito) {
      setMensaje(r.mensaje);
      return;
    }
    avisar(r.mensaje, 'exito');
    router.replace('/verificar');
  };

  const enlace = (texto: string, codigo: 'terminos' | 'privacidad') => (
    <Text
      accessibilityRole="link"
      onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo } })}
      className="font-nunito-semi text-primario underline dark:text-primario-oscuro"
    >
      {texto}
    </Text>
  );

  return (
    <Pantalla>
      <Titulo>Crear cuenta</Titulo>
      <Parrafo suave>Todos los datos son obligatorios salvo los marcados como opcionales. Te vamos a mandar un código al correo para verificarlo.</Parrafo>

      <Tarjeta>
        <Subtitulo>Tus datos</Subtitulo>
        <Campo etiqueta="Nombre" value={datos.nombre} onChangeText={(v) => cambiar('nombre', v)} error={errores.nombre} autoComplete="given-name" autoCapitalize="words" />
        <Campo etiqueta="Apellido" value={datos.apellido} onChangeText={(v) => cambiar('apellido', v)} error={errores.apellido} autoComplete="family-name" autoCapitalize="words" />
        <Campo etiqueta="Correo electrónico" value={datos.correo} onChangeText={(v) => cambiar('correo', v)} error={errores.correo} keyboardType="email-address" autoCapitalize="none" autoComplete="email" autoCorrect={false} />
        <Campo etiqueta="Teléfono" value={datos.telefono} onChangeText={(v) => cambiar('telefono', v)} error={errores.telefono} keyboardType="phone-pad" autoComplete="tel" ayuda="Ejemplo: 099 123 456" />
        <Selector
          etiqueta="Tipo de documento"
          valor={datos.tipoDocumento}
          opciones={TIPOS_DOCUMENTO}
          onCambio={(v) => cambiar('tipoDocumento', v)}
        />
        <Campo
          etiqueta="Número de documento"
          value={datos.numeroDocumento}
          onChangeText={(v) => cambiar('numeroDocumento', v)}
          error={errores.numeroDocumento}
          autoCapitalize="characters"
          keyboardType={datos.tipoDocumento === 'CI' ? 'number-pad' : 'default'}
          ayuda={datos.tipoDocumento === 'CI' ? 'Cédula con dígito verificador, con o sin puntos y guion.' : undefined}
        />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Dirección del hogar</Subtitulo>
        <Campo etiqueta="Calle" value={datos.calle} onChangeText={(v) => cambiar('calle', v)} error={errores.calle} autoCapitalize="words" />
        <Campo etiqueta="Número de puerta" value={datos.numeroPuerta} onChangeText={(v) => cambiar('numeroPuerta', v)} error={errores.numeroPuerta} />
        <Campo etiqueta="Apartamento" opcional value={datos.apartamento} onChangeText={(v) => cambiar('apartamento', v)} error={errores.apartamento} />
        <Campo etiqueta="Localidad" value={datos.localidad} onChangeText={(v) => cambiar('localidad', v)} error={errores.localidad} autoCapitalize="words" />
        <Selector etiqueta="Departamento" valor={datos.departamento} opciones={departamentos} onCambio={(v) => cambiar('departamento', v)} error={errores.departamento} />
        <Campo etiqueta="Referencias" opcional value={datos.referencias} onChangeText={(v) => cambiar('referencias', v)} error={errores.referencias} multiline />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Contraseña</Subtitulo>
        <Campo etiqueta="Contraseña" value={datos.contrasena} onChangeText={(v) => cambiar('contrasena', v)} error={errores.contrasena} secureTextEntry autoCapitalize="none" autoComplete="new-password" ayuda="Entre 8 y 64 caracteres." />
        <Campo etiqueta="Repetir contraseña" value={datos.repetirContrasena} onChangeText={(v) => cambiar('repetirContrasena', v)} error={errores.repetirContrasena} secureTextEntry autoCapitalize="none" autoComplete="new-password" />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Aceptaciones obligatorias</Subtitulo>
        <Casilla etiqueta="Acepto los Términos y Condiciones" marcada={datos.aceptaTerminos} onCambio={(v) => cambiar('aceptaTerminos', v)} error={errores.aceptaTerminos}>
          <Text className="font-nunito text-base text-texto dark:text-texto-oscuro">Acepto los {enlace('Términos y Condiciones', 'terminos')} (v1.0)</Text>
        </Casilla>
        <Casilla etiqueta="Acepto la Política de Privacidad" marcada={datos.aceptaPrivacidad} onCambio={(v) => cambiar('aceptaPrivacidad', v)} error={errores.aceptaPrivacidad}>
          <Text className="font-nunito text-base text-texto dark:text-texto-oscuro">Acepto la {enlace('Política de Privacidad', 'privacidad')} (v1.0)</Text>
        </Casilla>
        <Nota>Los textos son de una clínica ficticia y se identifican como borrador académico.</Nota>
      </Tarjeta>

      {mensaje ? (
        <View accessibilityLiveRegion="polite" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
          <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{mensaje}</Text>
        </View>
      ) : null}
      <Boton texto="Crear cuenta" icono="person-add-outline" onPress={enviar} />
      <Pressable accessibilityRole="link" onPress={() => router.back()} className="min-h-12 items-center justify-center">
        <Text className="font-nunito-semi text-base text-primario dark:text-primario-oscuro">Ya tengo cuenta: volver a ingresar</Text>
      </Pressable>
    </Pantalla>
  );
}
