import type { StatoPartita, Fase } from "./stato.js";
import type { Evento } from "./eventi.js";

export const ORDINE_FASI: Fase[] = ["untap", "upkeep", "pesca", "main1", "combat", "main2", "end"];

export interface RisultatoFase {
  stato: StatoPartita;
  eventi: Evento[];
}

// Esegue la logica di INGRESSO di una fase per il giocatore attivo.
// Non modifica turnoDi/numeroTurno (lo fa l'orchestratore in engine.ts).
export function eseguiEntrataFase(stato: StatoPartita, fase: Fase): RisultatoFase {
  const eventi: Evento[] = [{ t: "faseEntrata", fase }];
  switch (fase) {
    case "untap":
      return untap(stato, eventi);
    case "pesca":
      return pesca(stato, eventi);
    default:
      return { stato, eventi };
  }
}

function untap(stato: StatoPartita, eventi: Evento[]): RisultatoFase {
  const att = stato.turnoDi;
  const g = stato.giocatori[att];
  const campo = g.campo.map((c) => {
    if (c.tappata) eventi.push({ t: "cartaStappata", iid: c.iid });
    return c.tappata ? { ...c, tappata: false } : c;
  });
  const mazzo = g.mazzo.map((c) => {
    if (c.tappata) eventi.push({ t: "cartaStappata", iid: c.iid });
    return c.tappata ? { ...c, tappata: false } : c;
  });
  const giocatori = stato.giocatori.map((gg, i) => (i === att ? { ...gg, campo, mazzo } : gg));
  return { stato: { ...stato, giocatori }, eventi };
}

function pesca(stato: StatoPartita, eventi: Evento[]): RisultatoFase {
  const att = stato.turnoDi;
  if (stato.numeroTurno === 1 && att === stato.primoGiocatore && stato.config.primoNonPescaT1) {
    return { stato, eventi };
  }
  const g = stato.giocatori[att];
  if (g.mazzo.length === 0) {
    return deckout(stato, eventi);
  }
  const [cima, ...resto] = g.mazzo;
  const nuovo = { ...g, mazzo: resto, mano: [...g.mano, cima] };
  const giocatori = stato.giocatori.map((gg, i) => (i === att ? nuovo : gg));
  eventi.push({ t: "cartaPescata", giocatore: att, iid: cima.iid });
  return { stato: { ...stato, giocatori }, eventi };
}

function deckout(stato: StatoPartita, eventi: Evento[]): RisultatoFase {
  const att = stato.turnoDi;
  eventi.push({ t: "mazzoVuoto", giocatore: att });

  if (stato.config.penalitaMazzoVuoto === "danno_per_turno") {
    const danno = stato.config.dannoMazzoVuoto ?? 1;
    const g = stato.giocatori[att];
    const hp = g.hp - danno;
    const giocatori = stato.giocatori.map((gg, i) => (i === att ? { ...gg, hp } : gg));
    if (hp <= 0) {
      const vincitore = stato.giocatori.length === 2 ? (att + 1) % 2 : undefined;
      eventi.push({ t: "partitaFinita", vincitore, motivo: "deckout-danno" });
      return { stato: { ...stato, giocatori, finita: true, vincitore }, eventi };
    }
    return { stato: { ...stato, giocatori }, eventi };
  }

  // perdita_immediata
  const vincitore = stato.giocatori.length === 2 ? (att + 1) % 2 : undefined;
  eventi.push({ t: "partitaFinita", vincitore, motivo: "deckout" });
  return { stato: { ...stato, finita: true, vincitore }, eventi };
}
