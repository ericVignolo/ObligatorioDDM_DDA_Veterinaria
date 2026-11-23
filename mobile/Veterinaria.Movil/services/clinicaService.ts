import { Linking } from 'react-native';
import { clinica } from '../data/clinica';

export function obtenerClinica() {
  return clinica;
}

// Igual que EnlaceTelefono de Blazor: "+598 4544 0000" → "tel:+59845440000".
export function llamar(telefono: string) {
  return Linking.openURL('tel:' + telefono.replace(/\s/g, ''));
}

export function escribir(correo: string) {
  return Linking.openURL('mailto:' + correo);
}
