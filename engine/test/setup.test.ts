import { test, expect } from "vitest";
import { iniziaPartita } from "../src/setup.js";
import type { ConfigPartita } from "../src/stato.js";
import type { DefCarta } from "../src/azioni.js";

const config: ConfigPartita = {
  hpIniziali: 30, manoIniziale: 2, limiteMano: 7,
  primoNonPescaT1: true, penalitaMazzoVuoto: "perdita_immediata",
};

function carteFinte(ids: string[]): Map<string, DefCarta> {
  return new Map(ids.map((id) => [id, { defId: id, tipo: "Creatura" }]));
}

test("distribuisce mano iniziale e imposta stato turno 1", () => {
  const carte = carteFinte(["A", "B", "C", "D", "E"]);
  const mazzi = [["A", "B", "C", "D", "E"], ["A", "B", "C", "D", "E"]];
  const { stato, eventi } = iniziaPartita({ seed: 1, config, mazzi, carte });

  expect(stato.giocatori).toHaveLength(2);
  expect(stato.giocatori[0].mano).toHaveLength(2);
  expect(stato.giocatori[0].mazzo).toHaveLength(3);
  expect(stato.turnoDi).toBe(0);
  expect(stato.numeroTurno).toBe(1);
  expect(stato.fase).toBe("untap");
  expect(stato.giocatori[0].hp).toBe(30);
  expect(eventi[0]).toEqual({ t: "partitaIniziata", giocatori: [0, 1], primo: 0 });
});

test("iid tutti univoci tra i giocatori", () => {
  const carte = carteFinte(["A", "B", "C"]);
  const mazzi = [["A", "B", "C"], ["A", "B", "C"]];
  const { stato } = iniziaPartita({ seed: 7, config, mazzi, carte });
  const tutte = stato.giocatori.flatMap((g) => [...g.mazzo, ...g.mano]);
  const iids = new Set(tutte.map((c) => c.iid));
  expect(iids.size).toBe(tutte.length);
});

test("deterministico per seed", () => {
  const carte = carteFinte(["A", "B", "C", "D", "E"]);
  const mazzi = [["A", "B", "C", "D", "E"], ["A", "B", "C", "D", "E"]];
  const r1 = iniziaPartita({ seed: 99, config, mazzi, carte });
  const r2 = iniziaPartita({ seed: 99, config, mazzi, carte });
  expect(r1.stato.giocatori[0].mano.map((c) => c.defId))
    .toEqual(r2.stato.giocatori[0].mano.map((c) => c.defId));
});
