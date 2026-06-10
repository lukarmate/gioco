import type { DefCarta, DefinizioniCarte } from "./azioni.js";

interface VoceJson {
  id?: string;
  tipo?: string;
}

// Trasforma il testo JSON di dist-motore/carte.json in Map<defId, DefCarta>.
export function caricaCarte(jsonText: string): DefinizioniCarte {
  const voci = JSON.parse(jsonText) as VoceJson[];
  const m: DefinizioniCarte = new Map<string, DefCarta>();
  for (const v of voci) {
    if (!v.id) continue;
    m.set(v.id, { defId: v.id, tipo: v.tipo ?? "" });
  }
  return m;
}
