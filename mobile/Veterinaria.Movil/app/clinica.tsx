import { useRouter } from 'expo-router';
import { Boton } from '../components/Boton';
import { Fila } from '../components/Fila';
import { Pantalla } from '../components/Pantalla';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../components/Textos';
import { escribir, llamar, obtenerClinica } from '../services/clinicaService';

// Mismo contenido que LaClinica.razor (RF-VIS-03).
export default function LaClinica() {
  const router = useRouter();
  const clinica = obtenerClinica();

  return (
    <Pantalla>
      <Titulo>{clinica.nombre}</Titulo>
      <Parrafo suave>{clinica.presentacion}</Parrafo>

      <Tarjeta>
        <Subtitulo>Contacto</Subtitulo>
        <Fila icono="call-outline" texto={clinica.telefono} detalle="Teléfono" onPress={() => llamar(clinica.telefono)} />
        <Fila icono="alert-circle-outline" texto={clinica.telefonoGuardia} detalle="Guardia, 24 horas" onPress={() => llamar(clinica.telefonoGuardia)} />
        <Fila icono="mail-outline" texto={clinica.correo} detalle="Correo" onPress={() => escribir(clinica.correo)} />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Horario</Subtitulo>
        <Parrafo>{clinica.horario}</Parrafo>
        <Nota>Las urgencias graves se atienden en todo horario por la guardia.</Nota>
        <Boton texto="Ver urgencias" variante="secundario" onPress={() => router.push('/urgencias')} />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Ubicación</Subtitulo>
        <Parrafo>{clinica.direccion}</Parrafo>
        <Parrafo>{clinica.ciudad}</Parrafo>
        {/* HU-VIS-03: sin mapa, la dirección en texto siempre queda visible. */}
        <Nota>El mapa se agrega más adelante.</Nota>
        <Parrafo>{clinica.cobertura}</Parrafo>
      </Tarjeta>

      <Nota>{clinica.razonSocial} Clínica ficticia del obligatorio.</Nota>
    </Pantalla>
  );
}
