import { test, expect } from "vitest";
import { applica } from "../src/engine.js";
import type { StatoPartita, ConfigPartita } from "../src/stato.js";
import type { DefCarta } from "../src/azioni.js";

const config: ConfigPartita = {
  hpIniziali: 30, manoIniziale: 2, limiteMano: 7,
  primoNonPescaT1: true, penalitaMazzoVuoto: "perdita_immediata",
};
function carte(ids: string[]) {
  return new Map<string, DefCarta>(ids.map((id) => [id, { defId: id, tipo: "Creatura" }]));
}
const ids = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];

function avvia(nGiocatori = 2): StatoPartita {
  const mazzi = Array.from({ length: nGiocatori }, () => [...ids]);
  const r = applica(undefined, { t: "iniziaPartita", seed: 5, config, mazzi, carte: carte(ids) });
  if (!r.ok) throw new Error(r.errore);
  return r.stato;
}

test("iniziaPartita valida 2..4 mazzi", () => {
  const uno = applica(undefined, { t: "iniziaPartita", seed: 1, config, mazzi: [[...ids]], carte: carte(ids) });
  expect(uno.ok).toBe(false);
});

test("avanzaFase cicla le fasi nell'ordine", () => {
  let stato = avvia();
  const fasi: string[] = [];
  for (let k = 0; k < 6; k++) {
    const r = applica(stato, { t: "avanzaFase" });
    expect(r.ok).toBe(true);
    if (!r.ok) return;
    stato = r.stato;
    fasi.push(stato.fase);
  }
  expect(fasi).toEqual(["upkeep", "pesca", "main1", "combat", "main2", "end"]);
});

test("da end passa al prossimo giocatore (2 giocatori) e azzera mana", () => {
  let stato = avvia();
  for (let k = 0; k < 6; k++) {
    const r = applica(stato, { t: "avanzaFase" });
    if (!r.ok) return; stato = r.stato;
  }
  expect(stato.fase).toBe("end");
  const r = applica(stato, { t: "avanzaFase" });
  expect(r.ok).toBe(true);
  if (!r.ok) return;
  expect(r.stato.turnoDi).toBe(1);
  expect(r.stato.numeroTurno).toBe(2);
  expect(r.stato.fase).toBe("untap");
  expect(r.eventi.some((e) => e.t === "turnoPassato")).toBe(true);
  expect(r.eventi.some((e) => e.t === "manaAzzerato")).toBe(true);
});

test("rotazione turni con 3 e 4 giocatori", () => {
  for (const n of [3, 4]) {
    let stato = avvia(n);
    for (let k = 0; k < 7; k++) {
      const r = applica(stato, { t: "avanzaFase" });
      if (!r.ok) return; stato = r.stato;
    }
    expect(stato.turnoDi).toBe(1 % n);
  }
});

test("scarto-a-7: end con mano troppo grande blocca finché non scarti", () => {
  let stato = avvia();
  const extra = stato.giocatori[0].mazzo.slice();
  stato = { ...stato, giocatori: stato.giocatori.map((g, i) => i === 0 ? { ...g, mano: [...g.mano, ...extra], mazzo: [] } : g) };
  for (let k = 0; k < 6; k++) {
    const r = applica(stato, { t: "avanzaFase" });
    if (!r.ok) return; stato = r.stato;
  }
  expect(stato.fase).toBe("end");
  const bloccato = applica(stato, { t: "avanzaFase" });
  expect(bloccato.ok).toBe(false);
  const daScartare = stato.giocatori[0].mano.length - config.limiteMano;
  const iids = stato.giocatori[0].mano.slice(0, daScartare).map((c) => c.iid);
  const sc = applica(stato, { t: "scarta", iids });
  expect(sc.ok).toBe(true);
  if (!sc.ok) return;
  expect(sc.stato.giocatori[0].mano.length).toBe(config.limiteMano);
});

test("azione su partita finita => ok:false", () => {
  let stato = avvia();
  stato = { ...stato, finita: true };
  const r = applica(stato, { t: "avanzaFase" });
  expect(r.ok).toBe(false);
});
