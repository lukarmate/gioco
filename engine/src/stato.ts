export type Fase = "untap" | "upkeep" | "pesca" | "main1" | "combat" | "main2" | "end";

export interface ConfigPartita {
  hpIniziali: number;
  manoIniziale: number;
  limiteMano: number;
  primoNonPescaT1: boolean;
  penalitaMazzoVuoto: "perdita_immediata" | "danno_per_turno";
  dannoMazzoVuoto?: number; // usato se penalita = danno_per_turno
}

export interface ManaPool {
  nord: number; sud: number; est: number; ovest: number; centro: number; generico: number;
}

export function manaVuoto(): ManaPool {
  return { nord: 0, sud: 0, est: 0, ovest: 0, centro: 0, generico: 0 };
}

export interface CartaIstanza {
  iid: string;
  defId: string;
  proprietario: number;
  tappata: boolean;
}

export interface Giocatore {
  id: number;
  hp: number;
  mazzo: CartaIstanza[];
  mano: CartaIstanza[];
  campo: CartaIstanza[];
  cimitero: CartaIstanza[];
  esilio: CartaIstanza[];
  manaDisponibile: ManaPool;
  morti: number;
}

export interface StatoPartita {
  config: ConfigPartita;
  rng: number;
  giocatori: Giocatore[];
  turnoDi: number;
  numeroTurno: number;
  fase: Fase;
  primoGiocatore: number;
  finita: boolean;
  vincitore?: number;
}
