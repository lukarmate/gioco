import type { StatoPartita, Giocatore, CartaIstanza, ConfigPartita } from "./stato.js";
import { manaVuoto } from "./stato.js";
import type { DefinizioniCarte, DeckList } from "./azioni.js";
import type { Evento } from "./eventi.js";
import { mescola } from "./rng.js";

export interface ArgsInizia {
  seed: number;
  config: ConfigPartita;
  mazzi: DeckList[];
  carte: DefinizioniCarte;
}

export function iniziaPartita(args: ArgsInizia): { stato: StatoPartita; eventi: Evento[] } {
  let rng = args.seed | 0;
  let contatore = 0;
  const giocatori: Giocatore[] = [];

  for (let id = 0; id < args.mazzi.length; id++) {
    const istanze: CartaIstanza[] = args.mazzi[id].map((defId) => ({
      iid: `c${contatore++}`,
      defId,
      proprietario: id,
      tappata: false,
    }));
    const m = mescola(istanze, rng);
    rng = m.stato;
    const mano = m.arr.slice(0, args.config.manoIniziale);
    const mazzo = m.arr.slice(args.config.manoIniziale);
    giocatori.push({
      id, hp: args.config.hpIniziali,
      mazzo, mano, campo: [], cimitero: [], esilio: [],
      manaDisponibile: manaVuoto(), morti: 0,
    });
  }

  const stato: StatoPartita = {
    config: args.config, rng, giocatori,
    turnoDi: 0, numeroTurno: 1, fase: "untap",
    primoGiocatore: 0, finita: false,
  };

  const eventi: Evento[] = [
    { t: "partitaIniziata", giocatori: giocatori.map((g) => g.id), primo: 0 },
    { t: "turnoIniziato", giocatore: 0, numeroTurno: 1 },
    { t: "faseEntrata", fase: "untap" },
  ];

  return { stato, eventi };
}
