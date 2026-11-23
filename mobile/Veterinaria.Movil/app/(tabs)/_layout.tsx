import { Tabs } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Icono } from '../../components/Icono';
import { useSesion } from '../../context/SesionContext';
import { fuentes, useColor } from '../../tema';

// Pestañas por rol (docs/analisis/MAPA_MOVIL.md). Todas las pantallas existen; cada rol ve solo las suyas.
type Pestana = { nombre: string; titulo: string; icono: string; roles: ('Visitante' | 'Cliente' | 'Veterinario')[] };

const PESTANAS: Pestana[] = [
  { nombre: 'index', titulo: 'Inicio', icono: 'home', roles: ['Visitante', 'Cliente'] },
  { nombre: 'servicios', titulo: 'Servicios', icono: 'list', roles: ['Visitante'] },
  { nombre: 'urgencias', titulo: 'Urgencias', icono: 'alert-circle', roles: ['Visitante'] },
  { nombre: 'ingresar', titulo: 'Ingresar', icono: 'log-in', roles: ['Visitante'] },
  { nombre: 'animales', titulo: 'Animales', icono: 'paw', roles: ['Cliente'] },
  { nombre: 'turnos', titulo: 'Turnos', icono: 'calendar', roles: ['Cliente'] },
  { nombre: 'agenda', titulo: 'Agenda', icono: 'today', roles: ['Veterinario'] },
  { nombre: 'asistencias', titulo: 'Asistencias', icono: 'navigate', roles: ['Veterinario'] },
  { nombre: 'avisos', titulo: 'Avisos', icono: 'notifications', roles: ['Cliente', 'Veterinario'] },
  { nombre: 'perfil', titulo: 'Perfil', icono: 'person-circle', roles: ['Cliente', 'Veterinario'] },
];

export default function LayoutPestanas() {
  const { usuario } = useSesion();
  const color = useColor();
  const insets = useSafeAreaInsets();
  const rol = usuario?.rol === 'Cliente' || usuario?.rol === 'Veterinario' ? usuario.rol : 'Visitante';

  return (
    <Tabs
      screenOptions={{
        headerStyle: { backgroundColor: color('superficie') },
        headerTitleStyle: { fontFamily: fuentes.negrita, color: color('texto') },
        headerShadowVisible: false,
        // Alto propio para que las etiquetas no se corten, más el borde seguro del celular.
        tabBarStyle: {
          backgroundColor: color('superficie'),
          borderTopColor: color('borde'),
          height: 64 + insets.bottom,
          paddingTop: 6,
          paddingBottom: 6 + insets.bottom,
        },
        tabBarActiveTintColor: color('primario'),
        tabBarInactiveTintColor: color('texto-suave'),
        tabBarLabelStyle: { fontFamily: fuentes.semi, fontSize: 12, lineHeight: 16, flexShrink: 0 },
        sceneStyle: { backgroundColor: color('fondo') },
      }}
    >
      {PESTANAS.map((p) => (
        <Tabs.Screen
          key={p.nombre}
          name={p.nombre}
          options={{
            title: p.titulo,
            tabBarLabel: p.titulo,
            // Las pestañas de otros roles se ocultan de la barra pero siguen navegables
            // (por ejemplo, la Cliente abre Urgencias o Servicios desde su inicio).
            tabBarItemStyle: p.roles.includes(rol) ? undefined : { display: 'none' },
            tabBarIcon: ({ focused }) => (
              <Icono nombre={focused ? p.icono : `${p.icono}-outline`} color={focused ? 'primario' : 'texto-suave'} tamano={24} />
            ),
          }}
        />
      ))}
    </Tabs>
  );
}
