import { test, expect } from "vitest";
import { applica } from "../src/engine.js";
import type { Azione } from "../src/azioni.js";
import type { DefCarta } from "../src/azioni.js";
import type { ConfigPartita, StatoPartita } from "../src/stato.js";

const config: ConfigPartita = {
  hpIniziali: 30, manoIniziale: 2, limiteMano: 7,
  primoNonPescaT1: true, penalitaMazzoVuoto: "perdita_immediata",
};
const ids = ["A", "B", "C", "D", "E", "F", "G", "H"];
const carte = new Map<string, DefCarta>(ids.map((id) => [id, { defId: id, tipo: "Creatura" }]));

function gioca(seed: number): StatoPartita {
  const azioni: Azione[] = [
    { t: "iniziaPartita", seed, config, mazzi: [[...ids], [...ids]], carte },
    { t: "avanzaFase" }, { t: "avanzaFase" }, { t: "avanzaFase" },
    { t: "avanzaFase" }, { t: "avanzaFase" }, { t: "avanzaFase" }, { t: "avanzaFase" },
  ];
  let stato: StatoPartita | undefined;
  for (const a of azioni) {
    const r = applica(stato, a);
    if (!r.ok) throw new Error(r.errore);
    stato = r.stato;
  }
  return stato!;
}

test("stesso seed + stesse azioni => stato finale identico", () => {
  const a = gioca(2024);
  const b = gioca(2024);
  expect(JSON.stringify(a)).toBe(JSON.stringify(b));
});

test("seed diversi => mani iniziali (probabilmente) diverse", () => {
  const a = gioca(1);
  const b = gioca(2);
  expect(JSON.stringify(a.giocatori[0].mazzo)).not.toBe(JSON.stringify(b.giocatori[0].mazzo));
});
