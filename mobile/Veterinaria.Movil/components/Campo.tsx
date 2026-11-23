import { useState } from 'react';
import { Text, TextInput, View, type TextInputProps } from 'react-native';
import { useColor } from '../tema';

type Props = TextInputProps & {
  etiqueta: string;
  error?: string;
  ayuda?: string;
  opcional?: boolean;
};

// Campo de formulario (S3): etiqueta siempre visible, foco, error debajo y aviso de opcional.
export function Campo({ etiqueta, error, ayuda, opcional, editable = true, ...props }: Props) {
  const color = useColor();
  const [foco, setFoco] = useState(false);
  const borde = error
    ? 'border-peligro dark:border-peligro-oscuro'
    : foco
      ? 'border-primario dark:border-primario-oscuro border-2'
      : 'border-borde dark:border-borde-oscuro';

  return (
    <View className="gap-1">
      <Text className="font-nunito-semi text-sm text-texto dark:text-texto-oscuro">
        {etiqueta}
        {opcional ? <Text className="font-nunito text-texto-suave dark:text-texto-suave-oscuro"> (opcional)</Text> : null}
      </Text>
      <TextInput
        {...props}
        editable={editable}
        accessibilityLabel={etiqueta}
        accessibilityHint={error ?? ayuda}
        placeholderTextColor={color('texto-suave')}
        onFocus={(e) => {
          setFoco(true);
          props.onFocus?.(e);
        }}
        onBlur={(e) => {
          setFoco(false);
          props.onBlur?.(e);
        }}
        className={`min-h-12 rounded-boton border bg-superficie px-3 font-nunito text-base text-texto dark:bg-superficie-oscuro dark:text-texto-oscuro ${borde} ${editable ? '' : 'opacity-50'}`}
      />
      {error ? (
        <Text className="font-nunito-semi text-sm text-peligro dark:text-peligro-oscuro">{error}</Text>
      ) : ayuda ? (
        <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{ayuda}</Text>
      ) : null}
    </View>
  );
}
