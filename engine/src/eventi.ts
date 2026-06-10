import type { Fase } from "./stato.js";

export type Evento =
  | { t: "partitaIniziata"; giocatori: number[]; primo: number }
  | { t: "turnoIniziato"; giocatore: number; numeroTurno: number }
  | { t: "faseEntrata"; fase: Fase }
  | { t: "cartaStappata"; iid: string }
  | { t: "cartaPescata"; giocatore: number; iid: string }
  | { t: "mazzoVuoto"; giocatore: number }
  | { t: "manaAzzerato"; giocatore: number }
  | { t: "richiestaScarto"; giocatore: number; quantita: number }
  | { t: "cartaScartata"; giocatore: number; iid: string }
  | { t: "turnoPassato"; da: number; a: number }
  | { t: "partitaFinita"; vincitore?: number; motivo: string };
