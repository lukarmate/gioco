import { test, expect } from "vitest";
import { caricaCarte } from "../src/carte-db.js";

test("trasforma il json carte in mappa DefCarta", () => {
  const json = JSON.stringify([
    { id: "FORGIA", tipo: "Santuario", nome: "Forgia", categoria: "CAT2" },
    { id: "MORDETH", tipo: "Creatura", nome: "Mordeth", categoria: "CAT2" },
  ]);
  const m = caricaCarte(json);
  expect(m.size).toBe(2);
  expect(m.get("FORGIA")).toEqual({ defId: "FORGIA", tipo: "Santuario" });
  expect(m.get("MORDETH")?.tipo).toBe("Creatura");
});

test("ignora voci senza id", () => {
  const json = JSON.stringify([{ tipo: "X" }, { id: "OK", tipo: "Creatura" }]);
  const m = caricaCarte(json);
  expect(m.size).toBe(1);
  expect(m.has("OK")).toBe(true);
});
