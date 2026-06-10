import type { StatoPartita } from "./stato.js";
import { manaVuoto } from "./stato.js";
import type { Evento } from "./eventi.js";
import type { Azione } from "./azioni.js";
import { iniziaPartita } from "./setup.js";
import { ORDINE_FASI, eseguiEntrataFase } from "./fasi.js";

export type Risultato =
  | { ok: true; stato: StatoPartita; eventi: Evento[] }
  | { ok: false; errore: string };

export function applica(stato: StatoPartita | undefined, azione: Azione): Risultato {
  if (azione.t === "iniziaPartita") {
    if (azione.mazzi.length < 2 || azione.mazzi.length > 4) {
      return { ok: false, errore: "servono da 2 a 4 mazzi" };
    }
    const r = iniziaPartita(azione);
    return { ok: true, stato: r.stato, eventi: r.eventi };
  }

  if (!stato) return { ok: false, errore: "partita non iniziata" };
  if (stato.finita) return { ok: false, errore: "partita finita" };

  switch (azione.t) {
    case "avanzaFase":
      return avanzaFase(stato);
    case "scarta":
      return scarta(stato, azione.iids);
    default:
      return { ok: false, errore: "azione sconosciuta" };
  }
}

function daScartare(stato: StatoPartita): number {
  const g = stato.giocatori[stato.turnoDi];
  return Math.max(0, g.mano.length - stato.config.limiteMano);
}

function avanzaFase(stato: StatoPartita): Risultato {
  if (stato.fase === "end") {
    if (daScartare(stato) > 0) {
      return { ok: false, errore: "devi scartare prima di passare il turno" };
    }
    return fineTurno(stato);
  }

  const idx = ORDINE_FASI.indexOf(stato.fase);
  const prossima = ORDINE_FASI[idx + 1];
  const r = eseguiEntrataFase({ ...stato, fase: prossima }, prossima);
  const eventi = [...r.eventi];

  if (prossima === "end" && daScartare(r.stato) > 0) {
    eventi.push({ t: "richiestaScarto", giocatore: r.stato.turnoDi, quantita: daScartare(r.stato) });
  }
  return { ok: true, stato: r.stato, eventi };
}

function fineTurno(stato: StatoPartita): Risultato {
  const att = stato.turnoDi;
  const n = stato.giocatori.length;
  const prossimo = (att + 1) % n;

  const giocatori = stato.giocatori.map((g, i) =>
    i === att ? { ...g, manaDisponibile: manaVuoto() } : g
  );

  const statoPassato: StatoPartita = {
    ...stato,
    giocatori,
    turnoDi: prossimo,
    numeroTurno: stato.numeroTurno + 1,
    fase: "untap",
  };

  const eventi: Evento[] = [
    { t: "manaAzzerato", giocatore: att },
    { t: "turnoPassato", da: att, a: prossimo },
    { t: "turnoIniziato", giocatore: prossimo, numeroTurno: statoPassato.numeroTurno },
  ];

  const r = eseguiEntrataFase(statoPassato, "untap");
  return { ok: true, stato: r.stato, eventi: [...eventi, ...r.eventi] };
}

function scarta(stato: StatoPartita, iids: string[]): Risultato {
  if (stato.fase !== "end") return { ok: false, errore: "scarto solo a fine turno" };
  const richiesti = daScartare(stato);
  if (richiesti === 0) return { ok: false, errore: "nessuno scarto richiesto" };
  if (iids.length !== richiesti) {
    return { ok: false, errore: `devi scartare esattamente ${richiesti} carte` };
  }
  const att = stato.turnoDi;
  const g = stato.giocatori[att];
  const inMano = new Set(g.mano.map((c) => c.iid));
  if (!iids.every((id) => inMano.has(id))) {
    return { ok: false, errore: "iid non in mano" };
  }
  const daRimuovere = new Set(iids);
  const scartate = g.mano.filter((c) => daRimuovere.has(c.iid));
  const mano = g.mano.filter((c) => !daRimuovere.has(c.iid));
  const cimitero = [...g.cimitero, ...scartate];
  const giocatori = stato.giocatori.map((gg, i) => (i === att ? { ...gg, mano, cimitero } : gg));
  const eventi: Evento[] = scartate.map((c) => ({ t: "cartaScartata" as const, giocatore: att, iid: c.iid }));
  return { ok: true, stato: { ...stato, giocatori }, eventi };
}
