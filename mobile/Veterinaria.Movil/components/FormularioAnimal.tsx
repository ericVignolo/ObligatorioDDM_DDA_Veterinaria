import { useState } from 'react';
import { Text, View } from 'react-native';
import { especies, sexos, validar } from '../services/animalService';
import type { DatosAnimal } from '../types/Animal';
import type { Errores, Resultado } from '../types/Cuenta';
import { Boton } from './Boton';
import { Campo } from './Campo';
import { Casilla } from './Casilla';
import { Selector } from './Selector';
import { SelectorFoto } from './SelectorFoto';
import { Tarjeta } from './Tarjeta';
import { Nota, Parrafo, Subtitulo } from './Textos';

type Props = {
  inicial: DatosAnimal;
  textoBoton: string;
  // Guarda y devuelve el resultado; si falla, el mensaje se muestra arriba del botón (por ejemplo, microchip repetido).
  onGuardar: (datos: DatosAnimal) => Resultado;
};

// Formulario de alta y edición de un animal o grupo (B-007, CU-CLI-01). Mismas reglas que AnimalFormulario de Blazor.
export function FormularioAnimal({ inicial, textoBoton, onGuardar }: Props) {
  const [datos, setDatos] = useState<DatosAnimal>(inicial);
  const [errores, setErrores] = useState<Errores<DatosAnimal>>({});
  const [mensaje, setMensaje] = useState('');

  const cambiar = <K extends keyof DatosAnimal>(campo: K, valor: DatosAnimal[K]) => {
    setDatos((d) => ({ ...d, [campo]: valor }));
    if (errores[campo]) setErrores((e) => ({ ...e, [campo]: undefined }));
    setMensaje('');
  };

  const guardar = () => {
    const e = validar(datos);
    setErrores(e);
    setMensaje('');
    if (Object.keys(e).length > 0) {
      setMensaje('Revisá los campos marcados.');
      return;
    }
    const r = onGuardar(datos);
    if (!r.exito) setMensaje(r.mensaje);
  };

  return (
    <>
      <Tarjeta>
        <Subtitulo>
          Foto <Text className="font-nunito text-base text-texto-suave dark:text-texto-suave-oscuro">(opcional)</Text>
        </Subtitulo>
        <SelectorFoto foto={datos.foto} nombre={datos.nombre.trim()} onCambio={(uri) => cambiar('foto', uri)} />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Datos básicos</Subtitulo>
        <Campo etiqueta="Nombre" value={datos.nombre} onChangeText={(v) => cambiar('nombre', v)} error={errores.nombre} autoCapitalize="words" maxLength={60} />
        <Selector etiqueta="Especie" valor={datos.especie} opciones={especies} onCambio={(v) => cambiar('especie', v)} error={errores.especie} />
        <Campo etiqueta="Raza" opcional value={datos.raza} onChangeText={(v) => cambiar('raza', v)} error={errores.raza} autoCapitalize="sentences" maxLength={60} />
        <Selector etiqueta="Sexo" valor={datos.sexo} opciones={sexos} onCambio={(v) => cambiar('sexo', v)} error={errores.sexo} />
        <Casilla etiqueta="Es un grupo de animales" marcada={datos.esGrupo} onCambio={(v) => cambiar('esGrupo', v)}>
          <Parrafo>Es un grupo de animales</Parrafo>
          <Nota>Por ejemplo, una majada o un rodeo que se atiende en conjunto.</Nota>
        </Casilla>
        {datos.esGrupo ? (
          <Campo
            etiqueta="Cantidad de animales"
            value={datos.cantidadGrupo}
            onChangeText={(v) => cambiar('cantidadGrupo', v.replace(/\D/g, ''))}
            error={errores.cantidadGrupo}
            keyboardType="number-pad"
            maxLength={5}
            ayuda="Entre 2 y 10.000."
          />
        ) : (
          <Campo
            etiqueta="Fecha de nacimiento"
            opcional
            value={datos.fechaNacimiento}
            onChangeText={(v) => cambiar('fechaNacimiento', v)}
            error={errores.fechaNacimiento}
            placeholder="DD/MM/AAAA"
            keyboardType="numbers-and-punctuation"
            maxLength={10}
            ayuda="Si no la sabés exacta, podés dejarla vacía."
          />
        )}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Identificación</Subtitulo>
        {datos.esGrupo ? (
          <Nota>Un grupo no lleva microchip individual. Podés indicar la caravana o señal del grupo.</Nota>
        ) : (
          <Campo
            etiqueta="Microchip"
            opcional
            value={datos.microchip}
            onChangeText={(v) => cambiar('microchip', v.replace(/\D/g, ''))}
            error={errores.microchip}
            keyboardType="number-pad"
            maxLength={15}
            ayuda="15 números. Lo encontrás en el carné o te lo lee la clínica."
          />
        )}
        <Campo
          etiqueta="Caravana"
          opcional
          value={datos.caravana}
          onChangeText={(v) => cambiar('caravana', v)}
          error={errores.caravana}
          autoCapitalize="characters"
          maxLength={20}
          ayuda="Letras, números o guiones. Ejemplo: UY-OV-2041."
        />
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Para la clínica</Subtitulo>
        <Campo
          etiqueta="Señas particulares"
          opcional
          value={datos.senasParticulares}
          onChangeText={(v) => cambiar('senasParticulares', v)}
          error={errores.senasParticulares}
          multiline
          maxLength={300}
          ayuda="Pelaje, marcas o cualquier dato que ayude a reconocerlo."
        />
        <Campo
          etiqueta="Antecedentes"
          opcional
          value={datos.antecedentes}
          onChangeText={(v) => cambiar('antecedentes', v)}
          error={errores.antecedentes}
          multiline
          maxLength={500}
          ayuda="Enfermedades, cirugías o alergias que conozcas. El veterinario los revisa en la consulta."
        />
      </Tarjeta>

      {mensaje ? (
        <View accessibilityLiveRegion="polite" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
          <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{mensaje}</Text>
        </View>
      ) : null}
      <Boton texto={textoBoton} icono="save-outline" onPress={guardar} />
    </>
  );
}
