import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { EstadoRefuerzo } from '../../components/EstadoRefuerzo';
import { FotoAnimal } from '../../components/FotoAnimal';
import { Pantalla } from '../../components/Pantalla';
import { Proximamente } from '../../components/Proximamente';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { alertasDe, DIAS_AVISO_REFUERZO, servicioPara, textoAlerta } from '../../services/vacunaService';
import { diaCorto } from '../../utils/fechas';

// Avisos del Cliente (B-010, RF-CLI-07): alertas sanitarias de refuerzos próximos o vencidos de sus animales.
// Las notificaciones push y del Veterinario se completan en B-017; el correo lo manda la API (A-028).
export default function Avisos() {
  const router = useRouter();
  const { usuario } = useSesion();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const [alertas, setAlertas] = useState<ReturnType<typeof alertasDe>>([]);

  useFocusEffect(
    useCallback(() => {
      setAlertas(clienteId === null ? [] : alertasDe(clienteId));
    }, [clienteId]),
  );

  if (usuario?.rol === 'Veterinario') return <Proximamente icono="notifications-outline" titulo="Avisos" tarea="B-017" />;
  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="notifications-outline" titulo="Ingresá para ver tus avisos" accion={{ texto: 'Ingresar', onPress: () => router.navigate('/ingresar') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Avisos</Titulo>
      <Nota>Refuerzos de vacunas y antiparasitarios de tus animales: vencidos o en los próximos {DIAS_AVISO_REFUERZO} días.</Nota>

      {alertas.length === 0 ? (
        <Vacio icono="shield-checkmark-outline" titulo="Todo al día" texto="Ningún animal tiene refuerzos vencidos ni próximos." />
      ) : (
        alertas.map(({ animal, item }) => (
          <Tarjeta key={`${animal.id}-${item.aplicacion.id}`}>
            <View className="flex-row items-center gap-3">
              <FotoAnimal uri={animal.foto} nombre={animal.nombre} decorativa />
              <View className="flex-1 gap-1">
                <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">
                  {animal.nombre} · {item.aplicacion.producto}
                </Text>
                <EstadoRefuerzo estado={item.estado} />
              </View>
            </View>
            <Parrafo>{textoAlerta(item, diaCorto(item.aplicacion.fechaRefuerzo))}</Parrafo>
            <View className="flex-row gap-2">
              <View className="flex-1">
                <Boton texto="Ver carné" variante="secundario" onPress={() => router.push(`/animales/${animal.id}`)} />
              </View>
              <View className="flex-1">
                <Boton
                  texto="Reservar"
                  icono="calendar-outline"
                  onPress={() => router.push({ pathname: '/reservar', params: { animal: String(animal.id), servicio: String(servicioPara(item.aplicacion.tipo)) } })}
                />
              </View>
            </View>
          </Tarjeta>
        ))
      )}
      <Nota>Con la API también te llegan por correo, según lo que elijas en tus preferencias de comunicación.</Nota>
    </Pantalla>
  );
}
