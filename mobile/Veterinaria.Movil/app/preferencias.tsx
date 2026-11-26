import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { Fila } from '../components/Fila';
import { Opcion } from '../components/Opcion';
import { Pantalla } from '../components/Pantalla';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../components/Textos';
import { Vacio } from '../components/Vacio';
import { useAviso } from '../context/AvisoContext';
import { useSesion } from '../context/SesionContext';
import { cambiarPreferencia, canalDe, canalesDe, eleccionDe, tipos } from '../services/comunicacionesService';
import { respuestasDeCliente } from '../services/consentimientoService';
import { obtenerPorId as obtenerCuenta } from '../services/cuentaService';
import { obtenerPorCodigo } from '../services/legalService';
import { obtenerPorId as obtenerPrestacion } from '../services/prestacionService';
import type { TipoComunicacion } from '../types/Comunicacion';
import { diaCorto, hora } from '../utils/fechas';

// Preferencias y privacidad del Cliente (B-012, RF-CLI-18, RF-CLI-20, CU-CLI-17). Separa lo que se puede cambiar,
// las aceptaciones obligatorias y los consentimientos clínicos (RN-43). Exportar o eliminar datos no está en la app.
export default function Preferencias() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const [, setCambios] = useState(0);
  const cuenta = usuario?.rol === 'Cliente' ? obtenerCuenta(usuario.id) : undefined;

  // Al volver de un consentimiento se vuelve a leer el historial.
  useFocusEffect(useCallback(() => setCambios((n) => n + 1), []));

  if (!cuenta) {
    return (
      <Pantalla>
        <Vacio icono="options-outline" titulo="Ingresá como cliente para ver tus preferencias" accion={{ texto: 'Ir al inicio', onPress: () => router.navigate('/') }} />
      </Pantalla>
    );
  }

  const grupo = (tipo: TipoComunicacion) => {
    const eleccion = eleccionDe(cuenta.id, tipo.nombre);
    return (
      <Tarjeta key={tipo.nombre}>
        <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">{tipo.nombre}</Text>
        <Nota>{tipo.descripcion}</Nota>
        <View accessibilityRole="radiogroup" accessibilityLabel={`Canal para ${tipo.nombre}`} className="gap-2">
          {canalesDe(tipo).map((canal) => (
            <Opcion
              key={canal}
              titulo={canal}
              elegida={canalDe(cuenta.id, tipo) === canal}
              onPress={() => {
                if (canalDe(cuenta.id, tipo) === canal) return;
                const r = cambiarPreferencia(cuenta.id, tipo.nombre, canal);
                avisar(r.mensaje, r.exito ? 'exito' : 'error');
                setCambios((n) => n + 1);
              }}
            />
          ))}
        </View>
        <Nota>{eleccion ? `Lo cambiaste el ${diaCorto(eleccion.fecha)} a las ${hora(eleccion.fecha)} desde la app.` : 'Es el valor inicial: todavía no lo cambiaste.'}</Nota>
      </Tarjeta>
    );
  };

  const aceptado = (codigo: 'terminos' | 'privacidad', version: string) => {
    const vigente = obtenerPorCodigo(codigo);
    const detalle = version
      ? `Aceptaste la versión ${version} el ${diaCorto(cuenta.fechaAceptacion)}${vigente && vigente.version !== version ? ` · la vigente es ${vigente.version}` : ''}`
      : `Versión vigente ${vigente?.version} · cuenta de demostración sin aceptación registrada`;
    return <Fila icono="document-text-outline" texto={vigente?.titulo ?? codigo} detalle={detalle} onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo } })} />;
  };

  const consentimientos = respuestasDeCliente(cuenta.id);

  return (
    <Pantalla>
      <Titulo>Preferencias y privacidad</Titulo>

      <Subtitulo>Avisos opcionales</Subtitulo>
      <Parrafo suave>Podés cambiarlos o apagarlos cuando quieras.</Parrafo>
      {tipos.filter((t) => !t.esencial).map(grupo)}

      <Subtitulo>Mensajes del servicio</Subtitulo>
      <Parrafo suave>Son parte de la atención: elegís por dónde te llegan.</Parrafo>
      {tipos.filter((t) => t.esencial).map(grupo)}

      <Tarjeta>
        <Subtitulo>Aceptaciones obligatorias</Subtitulo>
        <Nota>Se aceptan al crear la cuenta y no se cambian desde las preferencias.</Nota>
        {aceptado('terminos', cuenta.versionTerminosAceptada)}
        {aceptado('privacidad', cuenta.versionPrivacidadAceptada)}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Consentimientos clínicos</Subtitulo>
        {consentimientos.length === 0 ? (
          <Parrafo suave>Todavía no respondiste ninguno. Cuando un turno lo pida, lo vas a ver en Turnos y en Avisos.</Parrafo>
        ) : (
          consentimientos.map((c) => (
            <Fila
              key={c.id}
              icono={c.respuesta === 'Aceptado' ? 'checkmark-circle-outline' : 'close-circle-outline'}
              texto={`${obtenerPrestacion(c.prestacionId)?.nombre ?? 'Procedimiento'} · ${c.respuesta}`}
              detalle={`Turno n.º ${c.turnoId} · versión ${c.version} · ${diaCorto(c.fecha)} ${hora(c.fecha)}`}
              onPress={() => router.push({ pathname: '/consentimiento/[turnoId]', params: { turnoId: String(c.turnoId) } })}
            />
          ))
        )}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Privacidad en la app</Subtitulo>
        <Parrafo>La app no usa publicidad, analítica ni rastreadores, así que no hay nada opcional que aceptar para usarla.</Parrafo>
        <Nota>Descargar tus datos o pedir que se eliminen todavía no está en la app: se hace en «Mi cuenta» del sitio web.</Nota>
      </Tarjeta>

      <Nota>Demostración: las preferencias se pierden al cerrar la app y no se envían mensajes.</Nota>
    </Pantalla>
  );
}
