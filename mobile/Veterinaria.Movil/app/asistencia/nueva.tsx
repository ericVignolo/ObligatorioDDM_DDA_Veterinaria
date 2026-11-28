import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Campo } from '../../components/Campo';
import { Casilla } from '../../components/Casilla';
import { LlamarGuardia } from '../../components/LlamarGuardia';
import { Opcion } from '../../components/Opcion';
import { Pantalla } from '../../components/Pantalla';
import { Selector } from '../../components/Selector';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { especies, obtenerDe as animalesDe } from '../../services/animalService';
import {
  calcularCosto,
  COBERTURA_KM,
  crear,
  datosVacios,
  LARGOS,
  MODALIDADES,
  NIVELES,
  pideAceptarCosto,
  TARIFA_BASE,
  TARIFA_POR_ANIMAL_EXTRA,
  TARIFA_POR_KM,
  validar,
} from '../../services/asistenciaService';
import { obtenerPorId as obtenerCuenta } from '../../services/cuentaService';
import { descartarIntencion } from '../../services/intencionService';
import { formatearPrecio } from '../../services/prestacionService';
import type { DatosSolicitud } from '../../types/Asistencia';
import type { Errores } from '../../types/Cuenta';

// Pedir orientación o asistencia (B-013, RF-CLI-21, 22 y 25, CU-CLI-19). Un solo formulario en el orden del caso de uso:
// modalidad, animales, síntomas y gravedad, destino, costo y confirmación. La ubicación por GPS llega en B-014.
export default function NuevaAsistencia() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const params = useLocalSearchParams<{ animal?: string }>();
  // Desde la ficha, el animal llega elegido si es del cliente (RN-18).
  const [datos, setDatos] = useState<DatosSolicitud>(() => {
    if (clienteId === null) return datosVacios('');
    const vacios = datosVacios(obtenerCuenta(clienteId)?.telefono ?? '');
    const elegido = animalesDe(clienteId).find((a) => a.id === Number(params.animal));
    return elegido ? { ...vacios, animalId: elegido.id } : vacios;
  });
  const [errores, setErrores] = useState<Errores<DatosSolicitud>>({});
  const [mensaje, setMensaje] = useState('');
  const [enviada, setEnviada] = useState(false);

  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="medkit-outline" titulo="Ingresá como cliente para pedir asistencia" texto="Si es grave, llamá a la guardia desde Urgencias." accion={{ texto: 'Ir a urgencias', onPress: () => router.replace('/urgencias') }} />
      </Pantalla>
    );
  }

  const animales = animalesDe(clienteId);
  const animal = animales.find((a) => a.id === datos.animalId);
  const pideCantidad = datos.animalId === 0 || animal?.esGrupo === true;
  const cantidad = pideCantidad ? Number(datos.cantidad) || 1 : 1;

  // Si cambia algo que mueve el costo, hay que volver a aceptarlo.
  const cambiar = (cambios: Partial<DatosSolicitud>) => {
    const mueveCosto = ['modalidad', 'nivel', 'cantidad', 'animalId'].some((c) => c in cambios);
    setDatos((d) => ({ ...d, ...cambios, ...(mueveCosto ? { aceptaCosto: false } : {}) }));
    setErrores((e) => {
      const copia = { ...e };
      (Object.keys(cambios) as (keyof DatosSolicitud)[]).forEach((c) => delete copia[c]);
      return copia;
    });
    setMensaje('');
  };

  const enviar = () => {
    if (enviada) return;
    const e = validar(datos, clienteId);
    setErrores(e);
    if (Object.keys(e).length > 0) return setMensaje(`Faltan o hay que corregir ${Object.keys(e).length} datos. Revisá los marcados.`);
    const r = crear(datos, clienteId);
    if (!r.exito || !r.id) return setMensaje(r.mensaje);
    setEnviada(true);
    descartarIntencion();
    avisar(r.mensaje, 'exito');
    router.replace({ pathname: '/asistencia/[id]', params: { id: String(r.id), nueva: '1' } });
  };

  const error = (texto?: string) => (texto ? <Text className="font-nunito-semi text-sm text-peligro dark:text-peligro-oscuro">{texto}</Text> : null);

  return (
    <Pantalla>
      <Titulo>Pedir asistencia</Titulo>
      <Nota>Esto no es un diagnóstico. Pedirla no garantiza un horario de llegada.</Nota>

      <Tarjeta>
        <Subtitulo>1. Qué necesitás</Subtitulo>
        <View accessibilityRole="radiogroup" accessibilityLabel="Modalidad" className="gap-2">
          {MODALIDADES.map((m) => (
            <Opcion key={m.nombre} titulo={m.nombre} detalle={m.detalle} elegida={datos.modalidad === m.nombre} onPress={() => cambiar({ modalidad: m.nombre })} />
          ))}
        </View>
        {error(errores.modalidad)}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>2. Para quién</Subtitulo>
        <View accessibilityRole="radiogroup" accessibilityLabel="Animal" className="gap-2">
          {animales.map((a) => (
            <Opcion
              key={a.id}
              titulo={a.nombre}
              detalle={a.esGrupo ? `${a.especie} · grupo de ${a.cantidadGrupo}` : a.especie}
              elegida={datos.animalId === a.id}
              onPress={() => cambiar({ animalId: a.id, especie: '', cantidad: '1' })}
            />
          ))}
          <Opcion titulo="Sin registrar o grupo" detalle="Indicá la especie y cuántos son" elegida={datos.animalId === 0} onPress={() => cambiar({ animalId: 0, cantidad: '1' })} />
        </View>
        {error(errores.animalId)}
        {datos.animalId === 0 ? <Selector etiqueta="Especie" valor={datos.especie} opciones={especies} onCambio={(especie) => cambiar({ especie })} error={errores.especie} /> : null}
        {pideCantidad ? (
          <Campo etiqueta="Cuántos animales están afectados" value={datos.cantidad} onChangeText={(cantidad) => cambiar({ cantidad })} error={errores.cantidad} keyboardType="number-pad" maxLength={4} />
        ) : null}
        <Campo etiqueta="Peso o tamaño aproximado" opcional value={datos.pesoAproximado} onChangeText={(pesoAproximado) => cambiar({ pesoAproximado })} error={errores.pesoAproximado} maxLength={LARGOS.pesoAproximado} />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>3. Qué pasa</Subtitulo>
        <Campo etiqueta="Síntomas" value={datos.sintomas} onChangeText={(sintomas) => cambiar({ sintomas })} error={errores.sintomas} maxLength={LARGOS.sintomas} multiline />
        <Campo etiqueta="Desde cuándo" ayuda="Por ejemplo: «hace una hora» o «desde ayer»." value={datos.inicioSintomas} onChangeText={(inicioSintomas) => cambiar({ inicioSintomas })} error={errores.inicioSintomas} maxLength={LARGOS.inicioSintomas} />
        <Campo etiqueta="Riesgos para acercarse o manipularlo" opcional ayuda="Muerde, está suelto, portón con candado…" value={datos.riesgos} onChangeText={(riesgos) => cambiar({ riesgos })} error={errores.riesgos} maxLength={LARGOS.riesgos} multiline />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>4. Qué tan grave lo ves</Subtitulo>
        <View accessibilityRole="radiogroup" accessibilityLabel="Gravedad percibida" className="gap-2">
          {NIVELES.map((n) => (
            <Opcion key={n.nombre} titulo={`${n.simbolo} ${n.nombre}`} detalle={n.descripcion} elegida={datos.nivel === n.nombre} onPress={() => cambiar({ nivel: n.nombre })} />
          ))}
        </View>
        {error(errores.nivel)}
        {/* D-ASI-01: el cuestionario se agrega cuando lo apruebe un veterinario. */}
        <Nota>Se usa el nivel que elijas. Un veterinario puede cambiarlo cuando evalúe el caso. Todavía no hay preguntas de orientación aprobadas.</Nota>
      </Tarjeta>

      {datos.nivel === 'Rojo' ? <LlamarGuardia texto="Si hay riesgo para la vida, llamá ahora y después, si querés, enviá la solicitud." /> : null}

      <Tarjeta>
        <Subtitulo>5. Dónde y cómo contactarte</Subtitulo>
        {datos.modalidad === 'Visita' ? (
          <>
            <Campo etiqueta="Dirección de la visita" value={datos.direccion} onChangeText={(direccion) => cambiar({ direccion })} error={errores.direccion} maxLength={LARGOS.direccion} autoComplete="street-address" />
            <Campo etiqueta="Referencias para llegar" opcional value={datos.referencias} onChangeText={(referencias) => cambiar({ referencias })} error={errores.referencias} maxLength={LARGOS.referencias} />
            <Nota>
              La dirección se usa solo para coordinar esta visita; la ven Recepción y el veterinario asignado. Cubrimos hasta {COBERTURA_KM} km: Recepción lo confirma.
            </Nota>
          </>
        ) : datos.modalidad ? (
          <Parrafo suave>Para orientación y traslado no hace falta ninguna ubicación.</Parrafo>
        ) : null}
        <Campo etiqueta="Teléfono de contacto" value={datos.telefonoContacto} onChangeText={(telefonoContacto) => cambiar({ telefonoContacto })} error={errores.telefonoContacto} maxLength={LARGOS.telefono} keyboardType="phone-pad" />
      </Tarjeta>

      {datos.modalidad ? (
        <Tarjeta>
          <Subtitulo>6. Costo</Subtitulo>
          {datos.modalidad === 'Visita' ? (
            <>
              <Parrafo>
                Desde {formatearPrecio(calcularCosto('Visita', 0, cantidad))}, más {formatearPrecio(TARIFA_POR_KM)} por km desde la clínica.
              </Parrafo>
              <Nota>
                Criterio: base de {formatearPrecio(TARIFA_BASE)} + {formatearPrecio(TARIFA_POR_ANIMAL_EXTRA)} por cada animal adicional + la distancia, que confirma Recepción. Se paga en Recepción.
              </Nota>
              {pideAceptarCosto(datos.modalidad, datos.nivel) ? (
                <Casilla marcada={datos.aceptaCosto} onCambio={(aceptaCosto) => cambiar({ aceptaCosto })} etiqueta="Acepto este criterio de costo" error={errores.aceptaCosto}>
                  <Parrafo>Acepto este criterio de costo.</Parrafo>
                </Casilla>
              ) : datos.nivel === 'Rojo' ? (
                <Nota>En un caso rojo no te pedimos aceptar el costo antes de enviar, para no demorar la atención. Una deuda pendiente tampoco lo bloquea.</Nota>
              ) : null}
            </>
          ) : (
            <Parrafo>La orientación no tiene costo.</Parrafo>
          )}
        </Tarjeta>
      ) : null}

      {mensaje ? (
        <View accessibilityRole="alert" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
          <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{mensaje}</Text>
        </View>
      ) : null}

      <Boton texto="Enviar solicitud" icono="send-outline" onPress={enviar} deshabilitado={enviada} />
    </Pantalla>
  );
}
