import { test, expect } from "vitest";
import { ORDINE_FASI, eseguiEntrataFase } from "../src/fasi.js";
import { iniziaPartita } from "../src/setup.js";
import type { ConfigPartita } from "../src/stato.js";
import type { DefCarta } from "../src/azioni.js";

const config: ConfigPartita = {
  hpIniziali: 30, manoIniziale: 2, limiteMano: 7,
  primoNonPescaT1: true, penalitaMazzoVuoto: "perdita_immediata",
};
const carte = new Map<string, DefCarta>(
  ["A", "B", "C", "D", "E"].map((id) => [id, { defId: id, tipo: "Creatura" }])
);
const mazzi = [["A", "B", "C", "D", "E"], ["A", "B", "C", "D", "E"]];

test("ORDINE_FASI ha le 7 fasi in ordine", () => {
  expect(ORDINE_FASI).toEqual(["untap", "upkeep", "pesca", "main1", "combat", "main2", "end"]);
});

test("untap stappa le carte tappate del giocatore attivo", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  stato.giocatori[0].mazzo[0].tappata = true;
  const r = eseguiEntrataFase(stato, "untap");
  expect(r.stato.giocatori[0].mazzo[0].tappata).toBe(false);
  expect(r.eventi.some((e) => e.t === "cartaStappata")).toBe(true);
});

test("pesca: primo giocatore NON pesca al turno 1", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  const manoPrima = stato.giocatori[0].mano.length;
  const r = eseguiEntrataFase(stato, "pesca");
  expect(r.stato.giocatori[0].mano.length).toBe(manoPrima);
  expect(r.eventi.some((e) => e.t === "cartaPescata")).toBe(false);
});

test("pesca: giocatore non-primo pesca 1", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  stato.turnoDi = 1;
  stato.numeroTurno = 2;
  const manoPrima = stato.giocatori[1].mano.length;
  const mazzoPrima = stato.giocatori[1].mazzo.length;
  const r = eseguiEntrataFase(stato, "pesca");
  expect(r.stato.giocatori[1].mano.length).toBe(manoPrima + 1);
  expect(r.stato.giocatori[1].mazzo.length).toBe(mazzoPrima - 1);
  expect(r.eventi.some((e) => e.t === "cartaPescata")).toBe(true);
});

test("pesca da mazzo vuoto (perdita_immediata) => partitaFinita", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  stato.turnoDi = 1;
  stato.numeroTurno = 2;
  stato.giocatori[1].mazzo = [];
  const r = eseguiEntrataFase(stato, "pesca");
  expect(r.stato.finita).toBe(true);
  expect(r.eventi.some((e) => e.t === "mazzoVuoto")).toBe(true);
  expect(r.eventi.some((e) => e.t === "partitaFinita")).toBe(true);
});

test("upkeep/main: solo faseEntrata", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  const r = eseguiEntrataFase(stato, "main1");
  expect(r.eventi).toEqual([{ t: "faseEntrata", fase: "main1" }]);
});
