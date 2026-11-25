import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Boton } from '../components/Boton';
import { Campo } from '../components/Campo';
import { Pantalla } from '../components/Pantalla';
import { Selector } from '../components/Selector';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../components/Textos';
import { Vacio } from '../components/Vacio';
import { useAviso } from '../context/AvisoContext';
import { useSesion } from '../context/SesionContext';
import { actualizarPerfil, cambiarContrasena, departamentos, obtenerPorId, usuarioDe } from '../services/cuentaService';
import type { DatosPerfil, Errores } from '../types/Cuenta';
import { normalizarTelefono } from '../utils/validacion';

// «Mi cuenta» del Cliente (A-009, B-006): datos editables y cambio de contraseña.
// El correo se cambia desde escritorio (D-PER-04) y el documento solo lo cambia Recepción (RN-42).
export default function MiCuenta() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario, actualizar, salir } = useSesion();
  const cuenta = usuario && usuario.rol === 'Cliente' ? obtenerPorId(usuario.id) : undefined;

  const [datos, setDatos] = useState<DatosPerfil>({
    nombre: cuenta?.nombre ?? '',
    apellido: cuenta?.apellido ?? '',
    telefono: cuenta?.telefono ?? '',
    calle: cuenta?.calle ?? '',
    numeroPuerta: cuenta?.numeroPuerta ?? '',
    apartamento: cuenta?.apartamento ?? '',
    localidad: cuenta?.localidad ?? '',
    departamento: cuenta?.departamento ?? '',
    referencias: cuenta?.referencias ?? '',
  });
  const [errores, setErrores] = useState<Errores<DatosPerfil>>({});
  const [mensaje, setMensaje] = useState('');

  const [actual, setActual] = useState('');
  const [nueva, setNueva] = useState('');
  const [repetir, setRepetir] = useState('');
  const [erroresClave, setErroresClave] = useState<{ actual?: string; nueva?: string; repetir?: string }>({});
  const [mensajeClave, setMensajeClave] = useState('');

  if (!usuario || !cuenta) {
    return (
      <Pantalla>
        <Vacio icono="person-circle-outline" titulo="Ingresá para ver tu cuenta" accion={{ texto: 'Ingresar', onPress: () => router.replace('/ingresar') }} />
      </Pantalla>
    );
  }

  const cambiar = (campo: keyof DatosPerfil, valor: string) => {
    setDatos((d) => ({ ...d, [campo]: valor }));
    if (errores[campo]) setErrores((e) => ({ ...e, [campo]: undefined }));
  };

  const guardar = () => {
    const e: Errores<DatosPerfil> = {};
    if (!datos.nombre.trim()) e.nombre = 'Ingresá tu nombre.';
    else if (datos.nombre.trim().length > 60) e.nombre = 'El nombre puede tener hasta 60 caracteres.';
    if (!datos.apellido.trim()) e.apellido = 'Ingresá tu apellido.';
    else if (datos.apellido.trim().length > 60) e.apellido = 'El apellido puede tener hasta 60 caracteres.';
    if (!datos.telefono.trim()) e.telefono = 'Ingresá un teléfono.';
    else if (normalizarTelefono(datos.telefono) === null) e.telefono = 'El teléfono no es válido. Ejemplo: 099 123 456 o +598 99 123 456.';
    if (!datos.calle.trim()) e.calle = 'Ingresá la calle.';
    else if (datos.calle.trim().length > 100) e.calle = 'La calle puede tener hasta 100 caracteres.';
    if (!datos.numeroPuerta.trim()) e.numeroPuerta = 'Ingresá el número de puerta.';
    else if (datos.numeroPuerta.trim().length > 10) e.numeroPuerta = 'El número puede tener hasta 10 caracteres.';
    if (datos.apartamento.trim().length > 10) e.apartamento = 'El apartamento puede tener hasta 10 caracteres.';
    if (!datos.localidad.trim()) e.localidad = 'Ingresá la localidad.';
    else if (datos.localidad.trim().length > 60) e.localidad = 'La localidad puede tener hasta 60 caracteres.';
    if (!datos.departamento) e.departamento = 'Elegí el departamento.';
    if (datos.referencias.trim().length > 200) e.referencias = 'Las referencias pueden tener hasta 200 caracteres.';
    setErrores(e);
    setMensaje('');
    if (Object.keys(e).length > 0) return;

    const r = actualizarPerfil(cuenta.id, datos);
    if (!r.exito) {
      setMensaje(r.mensaje);
      return;
    }
    actualizar(usuarioDe(obtenerPorId(cuenta.id) ?? cuenta));
    avisar(r.mensaje, 'exito');
  };

  const cambiarClave = () => {
    const e: typeof erroresClave = {};
    if (!actual) e.actual = 'Ingresá tu contraseña actual.';
    if (!nueva) e.nueva = 'Ingresá una contraseña.';
    else if (nueva.length < 8 || nueva.length > 64) e.nueva = 'La contraseña debe tener entre 8 y 64 caracteres.';
    if (!repetir) e.repetir = 'Repetí la contraseña.';
    else if (repetir !== nueva) e.repetir = 'Las contraseñas no coinciden.';
    setErroresClave(e);
    setMensajeClave('');
    if (Object.keys(e).length > 0) return;

    const r = cambiarContrasena(cuenta.id, actual, nueva, repetir);
    if (!r.exito) {
      setMensajeClave(r.mensaje);
      return;
    }
    // Como en Blazor (D-PER-05): se cierra la sesión y se vuelve a ingresar con la contraseña nueva.
    salir();
    avisar(r.mensaje, 'exito');
    router.replace('/ingresar');
  };

  const error = (texto: string) => (
    <View accessibilityLiveRegion="polite" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
      <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{texto}</Text>
    </View>
  );

  return (
    <Pantalla>
      <Titulo>Mi cuenta</Titulo>

      <Tarjeta>
        <Subtitulo>Datos que no se cambian acá</Subtitulo>
        <Parrafo>Correo: {cuenta.correo}</Parrafo>
        {cuenta.numeroDocumento ? (
          <Parrafo>
            Documento: {cuenta.tipoDocumento} {cuenta.numeroDocumento}
          </Parrafo>
        ) : null}
        <Nota>El correo se cambia desde la versión de escritorio. El documento solo lo cambia Recepción.</Nota>
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Tus datos</Subtitulo>
        <Campo etiqueta="Nombre" value={datos.nombre} onChangeText={(v) => cambiar('nombre', v)} error={errores.nombre} autoCapitalize="words" />
        <Campo etiqueta="Apellido" value={datos.apellido} onChangeText={(v) => cambiar('apellido', v)} error={errores.apellido} autoCapitalize="words" />
        <Campo etiqueta="Teléfono" value={datos.telefono} onChangeText={(v) => cambiar('telefono', v)} error={errores.telefono} keyboardType="phone-pad" />
        <Campo etiqueta="Calle" value={datos.calle} onChangeText={(v) => cambiar('calle', v)} error={errores.calle} autoCapitalize="words" />
        <Campo etiqueta="Número de puerta" value={datos.numeroPuerta} onChangeText={(v) => cambiar('numeroPuerta', v)} error={errores.numeroPuerta} />
        <Campo etiqueta="Apartamento" opcional value={datos.apartamento} onChangeText={(v) => cambiar('apartamento', v)} error={errores.apartamento} />
        <Campo etiqueta="Localidad" value={datos.localidad} onChangeText={(v) => cambiar('localidad', v)} error={errores.localidad} autoCapitalize="words" />
        <Selector etiqueta="Departamento" valor={datos.departamento} opciones={departamentos} onCambio={(v) => cambiar('departamento', v)} error={errores.departamento} />
        <Campo etiqueta="Referencias" opcional value={datos.referencias} onChangeText={(v) => cambiar('referencias', v)} error={errores.referencias} multiline />
        {mensaje ? error(mensaje) : null}
        <Boton texto="Guardar cambios" icono="save-outline" onPress={guardar} />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Cambiar contraseña</Subtitulo>
        <Campo etiqueta="Contraseña actual" value={actual} onChangeText={setActual} error={erroresClave.actual} secureTextEntry autoCapitalize="none" autoComplete="current-password" />
        {/* Si no la recuerda, se usa la recuperación por código con el correo de la cuenta (D-REG-03). */}
        <Pressable accessibilityRole="link" onPress={() => router.push('/recuperar?desde=cuenta')} className="min-h-12 justify-center self-start active:opacity-70">
          <Text className="font-nunito-semi text-base text-primario underline dark:text-primario-oscuro">¿No recordás tu contraseña actual?</Text>
        </Pressable>
        <Campo etiqueta="Contraseña nueva" value={nueva} onChangeText={setNueva} error={erroresClave.nueva} secureTextEntry autoCapitalize="none" autoComplete="new-password" ayuda="Entre 8 y 64 caracteres." />
        <Campo etiqueta="Repetir contraseña" value={repetir} onChangeText={setRepetir} error={erroresClave.repetir} secureTextEntry autoCapitalize="none" autoComplete="new-password" />
        {mensajeClave ? error(mensajeClave) : null}
        <Boton texto="Cambiar contraseña" icono="key-outline" variante="secundario" onPress={cambiarClave} />
        <Nota>Al cambiarla se cierra la sesión y volvés a ingresar con la contraseña nueva.</Nota>
      </Tarjeta>
    </Pantalla>
  );
}
