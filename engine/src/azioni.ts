import type { ConfigPartita } from "./stato.js";

// Tipo carte minimale locale: l'engine usa solo questi campi (disaccoppiato dal parser).
export interface DefCarta {
  defId: string;
  tipo: string;
}

export type DefinizioniCarte = Map<string, DefCarta>;
export type DeckList = string[]; // lista di defId

export type Azione =
  | { t: "iniziaPartita"; seed: number; config: ConfigPartita; mazzi: DeckList[]; carte: DefinizioniCarte }
  | { t: "avanzaFase" }
  | { t: "scarta"; iids: string[] };
