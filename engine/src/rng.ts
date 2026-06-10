// PRNG mulberry32: deterministico, stato = intero 32 bit. Nessun Math.random/Date.
export interface PassoRng {
  val: number; // 0 <= val < 1
  stato: number;
}

export function prossimo(stato: number): PassoRng {
  let a = (stato + 0x6d2b79f5) | 0;
  let t = Math.imul(a ^ (a >>> 15), 1 | a);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  const val = ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  return { val, stato: a };
}

export function mescola<T>(arr: readonly T[], stato: number): { arr: T[]; stato: number } {
  const out = arr.slice();
  let s = stato;
  for (let i = out.length - 1; i > 0; i--) {
    const p = prossimo(s);
    s = p.stato;
    const j = Math.floor(p.val * (i + 1));
    const tmp = out[i];
    out[i] = out[j];
    out[j] = tmp;
  }
  return { arr: out, stato: s };
}
