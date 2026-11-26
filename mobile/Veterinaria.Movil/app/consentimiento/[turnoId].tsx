import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Campo } from '../../components/Campo';
import { Casilla } from '../../components/Casilla';
import { EstadoConsentimiento } from '../../components/EstadoConsentimiento';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { obtenerDe as obtenerTurno } from '../../services/agendaService';
import { obtenerPorId as obtenerAnimal } from '../../services/animalService';
import { abierto, documentoVigente, estadoDe, firmar, LARGO_MAXIMO_NOMBRE, rechazar, requiere, respuestasDe } from '../../services/consentimientoService';
import { obtenerPorId as obtenerPrestacion } from '../../services/prestacionService';
import { diaCorto, diaLargo, hora } from '../../utils/fechas';

// Consentimiento informado (B-012, RF-CLI-11, CU-CLI-10), como FirmarConsentimiento.razor: leer, aceptar con casilla
// y nombre completo, no autorizar o retirar lo firmado (K-10). Es independiente de Términos y Privacidad (RN-43).
export default function Consentimiento() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const { turnoId } = useLocalSearchParams<{ turnoId: string }>();
  const [acepta, setAcepta] = useState(false);
  const [nombre, setNombre] = useState('');
  const [errores, setErrores] = useState<{ acepta?: string; nombre?: string }>({});
  const [error, setError] = useState('');
  const [confirmando, setConfirmando] = useState(false);
  const [, setCambios] = useState(0);

  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const turno = clienteId === null ? undefined : obtenerTurno(Number(turnoId), clienteId);
  const documento = turno && requiere(turno) ? documentoVigente(turno.prestacionId) : undefined;

  // RN-18: no se distingue entre «no existe», «no es tuyo» y «no lo necesita».
  if (clienteId === null || !turno || !documento) {
    return (
      <Pantalla>
        <Vacio icono="document-outline" titulo="Consentimiento no disponible" texto="No encontramos ese turno entre los tuyos o no necesita consentimiento." accion={{ texto: 'Ir a mis turnos', onPress: () => router.navigate('/turnos') }} />
      </Pantalla>
    );
  }

  const estado = estadoDe(turno);
  const historial = respuestasDe(turno.id, clienteId);
  const puedeResponder = abierto(turno);

  const terminar = (r: { exito: boolean; mensaje: string }) => {
    setError(r.exito ? '' : r.mensaje);
    if (r.exito) {
      avisar(r.mensaje, 'exito');
      setAcepta(false);
      setNombre('');
      setErrores({});
    }
    setConfirmando(false);
    setCambios((n) => n + 1);
  };

  const enviar = () => {
    const e = {
      acepta: acepta ? undefined : 'Marcá que leíste y aceptás el consentimiento.',
      nombre: nombre.trim() ? undefined : 'Escribí tu nombre completo.',
    };
    setErrores(e);
    setError('');
    if (!e.acepta && !e.nombre) terminar(firmar(turno.id, clienteId, documento.id, acepta, nombre));
  };

  const dato = (etiqueta: string, valor: string) => (
    <View className="flex-row justify-between gap-3 border-b border-borde py-2 dark:border-borde-oscuro">
      <Text className="font-nunito-semi text-base text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
      <Text className="flex-1 text-right font-nunito text-base text-texto dark:text-texto-oscuro">{valor}</Text>
    </View>
  );

  return (
    <Pantalla>
      <Titulo>{documento.titulo}</Titulo>
      <EstadoConsentimiento estado={estado} />

      <Tarjeta>
        {dato('Procedimiento', obtenerPrestacion(turno.prestacionId)?.nombre ?? 'Servicio')}
        {dato('Paciente', obtenerAnimal(turno.animalId, clienteId)?.nombre ?? 'Sin dato')}
        {dato('Turno', `${diaLargo(turno.inicio)}, ${hora(turno.inicio)} (n.º ${turno.id})`)}
        {dato('Versión', `${documento.version}, vigente desde el ${diaCorto(documento.vigenteDesde)}`)}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Texto del consentimiento</Subtitulo>
        <Parrafo>{documento.texto}</Parrafo>
      </Tarjeta>

      {error ? (
        <View accessibilityRole="alert" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
          <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{error}</Text>
        </View>
      ) : null}

      {estado === 'Firmado' ? (
        <>
          <View className="gap-1 rounded-tarjeta bg-exito-suave p-4 dark:bg-exito-suave-oscuro">
            <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">
              Firmado por {historial[0].nombreEscrito} el {diaCorto(historial[0].fecha)} a las {hora(historial[0].fecha)} ({historial[0].canal.toLowerCase()})
            </Text>
            <Nota>Huella: {historial[0].hash}</Nota>
          </View>
          {!puedeResponder ? null : confirmando ? (
            <View className="gap-2 rounded-boton bg-superficie-2 p-3 dark:bg-superficie-2-oscuro">
              <Parrafo>¿Retirar el consentimiento? La cirugía no se podrá hacer. La firma anterior queda en el historial.</Parrafo>
              <Boton texto="Sí, retirar" variante="peligro" onPress={() => terminar(rechazar(turno.id, clienteId))} />
              <Boton texto="No, volver" variante="secundario" onPress={() => setConfirmando(false)} />
            </View>
          ) : (
            <Boton texto="Retirar consentimiento" variante="secundario" onPress={() => setConfirmando(true)} />
          )}
        </>
      ) : puedeResponder ? (
        <>
          {estado === 'Versión nueva' ? <Parrafo>La clínica actualizó el documento: tenés que volver a firmarlo.</Parrafo> : null}
          {estado === 'Rechazado' ? <Parrafo>No autorizaste este procedimiento. Si cambiaste de opinión, podés firmarlo ahora.</Parrafo> : null}
          <Casilla
            marcada={acepta}
            onCambio={(v) => {
              setAcepta(v);
              setErrores((e) => ({ ...e, acepta: undefined }));
            }}
            etiqueta="Leí el texto, pude hacer preguntas y autorizo el procedimiento"
            error={errores.acepta}
          >
            <Parrafo>Leí el texto, pude hacer preguntas y autorizo el procedimiento.</Parrafo>
          </Casilla>
          <Campo
            etiqueta="Tu nombre completo, como figura en tu cuenta"
            value={nombre}
            onChangeText={(v) => {
              setNombre(v);
              setErrores((e) => ({ ...e, nombre: undefined }));
            }}
            error={errores.nombre}
            maxLength={LARGO_MAXIMO_NOMBRE}
            autoComplete="name"
            autoCapitalize="words"
          />
          <Nota>Se guardan la versión, la fecha, el canal y una huella del texto. Es independiente de los Términos y la Política de privacidad. No es una firma electrónica avanzada.</Nota>
          <Boton texto="Firmar consentimiento" icono="create-outline" onPress={enviar} />
          {estado !== 'Rechazado' ? <Boton texto="No autorizo" variante="secundario" onPress={() => terminar(rechazar(turno.id, clienteId))} /> : null}
        </>
      ) : (
        <Parrafo suave>El turno ya empezó o terminó: el consentimiento no se puede cambiar.</Parrafo>
      )}

      {historial.length > 1 ? (
        <Tarjeta>
          <Subtitulo>Historial de respuestas</Subtitulo>
          {historial.map((r) => (
            <Nota key={r.id}>
              {diaCorto(r.fecha)} {hora(r.fecha)} · {r.respuesta} · versión {r.version}
            </Nota>
          ))}
        </Tarjeta>
      ) : null}
    </Pantalla>
  );
}
