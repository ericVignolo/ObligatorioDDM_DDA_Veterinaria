// SHA-256 en TypeScript, sin dependencias, para la huella del consentimiento (K-04) en la demostración.
// En la sección C la huella la calcula la API.
const primos = (n: number) => {
  const lista: number[] = [];
  for (let c = 2; lista.length < n; c++) if (lista.every((p) => c % p !== 0)) lista.push(c);
  return lista;
};
// Constantes del estándar: parte decimal de las raíces cuadradas (H) y cúbicas (K) de los primeros primos.
const fraccion = (x: number) => ((x - Math.floor(x)) * 2 ** 32) >>> 0;
const H = primos(8).map((p) => fraccion(Math.sqrt(p)));
const K = primos(64).map((p) => fraccion(Math.cbrt(p)));
const rotar = (x: number, n: number) => (x >>> n) | (x << (32 - n));

function utf8(texto: string): number[] {
  const bytes: number[] = [];
  for (const caracter of texto) {
    const c = caracter.codePointAt(0) as number;
    if (c < 0x80) bytes.push(c);
    else if (c < 0x800) bytes.push(0xc0 | (c >> 6), 0x80 | (c & 63));
    else if (c < 0x10000) bytes.push(0xe0 | (c >> 12), 0x80 | ((c >> 6) & 63), 0x80 | (c & 63));
    else bytes.push(0xf0 | (c >> 18), 0x80 | ((c >> 12) & 63), 0x80 | ((c >> 6) & 63), 0x80 | (c & 63));
  }
  return bytes;
}

// Devuelve 64 caracteres hexadecimales en mayúsculas, como Convert.ToHexString de .NET.
export function sha256(texto: string): string {
  const bytes = utf8(texto);
  const bits = bytes.length * 8;
  bytes.push(0x80);
  while (bytes.length % 64 !== 56) bytes.push(0);
  const alto = Math.floor(bits / 2 ** 32);
  for (const parte of [alto, bits >>> 0]) bytes.push((parte >>> 24) & 255, (parte >>> 16) & 255, (parte >>> 8) & 255, parte & 255);

  const h = [...H];
  for (let inicio = 0; inicio < bytes.length; inicio += 64) {
    const w: number[] = [];
    for (let i = 0; i < 64; i++) {
      if (i < 16) {
        const j = inicio + i * 4;
        w[i] = ((bytes[j] << 24) | (bytes[j + 1] << 16) | (bytes[j + 2] << 8) | bytes[j + 3]) >>> 0;
      } else {
        const s0 = rotar(w[i - 15], 7) ^ rotar(w[i - 15], 18) ^ (w[i - 15] >>> 3);
        const s1 = rotar(w[i - 2], 17) ^ rotar(w[i - 2], 19) ^ (w[i - 2] >>> 10);
        w[i] = (w[i - 16] + s0 + w[i - 7] + s1) >>> 0;
      }
    }
    let [a, b, c, d, e, f, g, k] = h;
    for (let i = 0; i < 64; i++) {
      const t1 = (k + (rotar(e, 6) ^ rotar(e, 11) ^ rotar(e, 25)) + ((e & f) ^ (~e & g)) + K[i] + w[i]) >>> 0;
      const t2 = ((rotar(a, 2) ^ rotar(a, 13) ^ rotar(a, 22)) + ((a & b) ^ (a & c) ^ (b & c))) >>> 0;
      k = g;
      g = f;
      f = e;
      e = (d + t1) >>> 0;
      d = c;
      c = b;
      b = a;
      a = (t1 + t2) >>> 0;
    }
    [a, b, c, d, e, f, g, k].forEach((v, i) => (h[i] = (h[i] + v) >>> 0));
  }
  return h.map((v) => v.toString(16).padStart(8, '0')).join('').toUpperCase();
}
