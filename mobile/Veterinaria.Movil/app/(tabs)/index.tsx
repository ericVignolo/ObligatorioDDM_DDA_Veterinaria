import { Redirect, useRouter } from 'expo-router';
import { View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Fila } from '../../components/Fila';
import { Pantalla } from '../../components/Pantalla';
import { TarjetaPrestacion } from '../../components/TarjetaPrestacion';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { useSesion } from '../../context/SesionContext';
import { llamar, obtenerClinica } from '../../services/clinicaService';
import { obtenerPublicas } from '../../services/prestacionService';

// Inicio del Visitante y del Cliente (RF-VIS-01, RF-VIS-06). El Veterinario empieza en su agenda.
export default function Inicio() {
  const router = useRouter();
  const { usuario } = useSesion();
  const clinica = obtenerClinica();

  if (usuario?.rol === 'Veterinario') return <Redirect href="/agenda" />;

  const destacados = obtenerPublicas().filter((p) => p.reservableEnLinea).slice(0, 3);
  const reservar = (id: number) =>
    usuario ? router.push({ pathname: '/servicios/[id]', params: { id: String(id) } }) : router.push({ pathname: '/ingresar', params: { servicio: String(id) } });

  return (
    <Pantalla>
      <View className="gap-1">
        <Titulo>{usuario ? `Hola, ${usuario.nombre.split(' ')[0]}` : `Bienvenido a ${clinica.nombre}`}</Titulo>
        <Parrafo suave>{usuario ? '¿Qué necesitás hoy?' : clinica.lema}</Parrafo>
      </View>

      {/* HU-VIS-06: la guardia siempre a un toque, sin sesión. */}
      <View className="gap-2 rounded-tarjeta bg-peligro-suave p-4 dark:bg-peligro-suave-oscuro">
        <Subtitulo>¿Es una urgencia?</Subtitulo>
        <Parrafo>Si tu animal está en peligro, llamá a la guardia: {clinica.telefonoGuardia}</Parrafo>
        <Boton texto="Llamar a la guardia" icono="call-outline" variante="peligro" onPress={() => llamar(clinica.telefonoGuardia)} />
        <Boton texto="Cómo saber si es urgente" variante="secundario" onPress={() => router.push('/urgencias?desde=inicio')} />
      </View>

      {usuario ? (
        <Tarjeta>
          <Fila icono="paw-outline" texto="Mis animales" detalle="Fichas, historia y carnet" onPress={() => router.push('/animales')} />
          <Fila icono="calendar-outline" texto="Mis turnos" detalle="Próximos e historial" onPress={() => router.push('/turnos')} />
          <Fila icono="list-outline" texto="Servicios" detalle="Precios y reserva" onPress={() => router.push('/servicios')} />
        </Tarjeta>
      ) : (
        <Parrafo>{clinica.presentacion}</Parrafo>
      )}

      <Subtitulo>Servicios destacados</Subtitulo>
      {destacados.map((p) => (
        <TarjetaPrestacion
          key={p.id}
          prestacion={p}
          onVerDetalle={(id) => router.push({ pathname: '/servicios/[id]', params: { id: String(id) } })}
          onReservar={reservar}
        />
      ))}
      <Boton texto="Ver todos los servicios" variante="secundario" onPress={() => router.push('/servicios')} />

      <Tarjeta>
        <Fila icono="people-outline" texto="Profesionales" detalle="Especialidades y horarios" onPress={() => router.push('/profesionales')} />
        <Fila icono="business-outline" texto="La clínica" detalle={`${clinica.direccion}, ${clinica.ciudad}`} onPress={() => router.push('/clinica')} />
      </Tarjeta>

      <View className="gap-1">
        <Nota>{clinica.horario}</Nota>
        <View className="flex-row gap-4">
          <Fila icono="document-text-outline" texto="Privacidad" onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo: 'privacidad' } })} />
          <Fila icono="document-text-outline" texto="Términos" onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo: 'terminos' } })} />
        </View>
      </View>
    </Pantalla>
  );
}
