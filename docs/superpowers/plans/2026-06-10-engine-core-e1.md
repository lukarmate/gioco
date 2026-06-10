# Engine E1 — Core Loop + Eventi — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Costruire lo scheletro runtime puro del gioco — `applica(stato, azione) → {stato, eventi}` — che fa girare turni/fasi/pesca/scarto/deckout per 2–4 giocatori ed emette uno stream di eventi, senza giocare carte né combattimento.

**Architecture:** Modulo TypeScript `engine/` puro e deterministico. Lo stato è immutabile; ogni azione produce un nuovo stato + lista eventi. La casualità usa un PRNG seedato il cui stato vive nello stato di partita (no `Math.random`/`Date`). La logica è divisa: `rng` (casualità), `setup` (init), `fasi` (logica per-fase), `engine` (dispatch + validazione azioni).

**Tech Stack:** TypeScript, Node 24, vitest, tsx. Modulo isolato (nessuna dipendenza dall'app o dal parser `motore/`; usa un tipo carte minimale locale `DefCarta`).

---

## File Structure

```
engine/
├─ package.json          vitest + tsx + @types/node
├─ tsconfig.json
├─ src/
│  ├─ stato.ts           StatoPartita, Giocatore, CartaIstanza, Fase, ConfigPartita, ManaPool, manaVuoto()
│  ├─ eventi.ts          union Evento
│  ├─ azioni.ts          union Azione + DeckList, DefCarta, DefinizioniCarte
│  ├─ rng.ts             prossimo(stato), mescola(arr, stato)
│  ├─ setup.ts           iniziaPartita(args) → {stato, eventi}
│  ├─ fasi.ts            ORDINE_FASI, eseguiEntrataFase(stato, fase) → {stato, eventi}
│  ├─ engine.ts          applica(stato, azione) → Risultato
│  └─ carte-db.ts        caricaCarte(jsonText) → Map<defId, DefCarta>
└─ test/  (rng, setup, fasi, engine, carte-db)
```

---

## Task 1: Scaffold modulo `engine/`

**Files:**
- Create: `engine/package.json`
- Create: `engine/tsconfig.json`
- Create: `engine/test/smoke.test.ts`

- [ ] **Step 1: Crea `engine/package.json`**

```json
{
  "name": "gioco-engine",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "test": "vitest run",
    "test:watch": "vitest",
    "typecheck": "tsc --noEmit"
  },
  "devDependencies": {
    "@types/node": "^22.0.0",
    "typescript": "^5.6.0",
    "vitest": "^2.1.0",
    "tsx": "^4.19.0"
  }
}
```

- [ ] **Step 2: Crea `engine/tsconfig.json`**

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "noEmit": true,
    "types": ["vitest/globals", "node"]
  },
  "include": ["src", "test"]
}
```

- [ ] **Step 3: Crea `engine/test/smoke.test.ts`**

```ts
import { test, expect } from "vitest";

test("toolchain engine funziona", () => {
  expect(1 + 1).toBe(2);
});
```

- [ ] **Step 4: Installa e lancia**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npm install && npm test`
Expected: 1 test PASS. Se `npm install` fallisce per rete/sandbox → report BLOCKED con l'errore esatto.

- [ ] **Step 5: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
echo "engine/node_modules/" >> .gitignore
git add engine/package.json engine/package-lock.json engine/tsconfig.json engine/test/smoke.test.ts .gitignore
git commit -m "chore(engine): scaffold modulo runtime (ts + vitest)"
```

---

## Task 2: RNG seedabile (`rng.ts`)

**Files:**
- Create: `engine/src/rng.ts`
- Create: `engine/test/rng.test.ts`

- [ ] **Step 1: Scrivi i test (FAIL)**

```ts
import { test, expect } from "vitest";
import { prossimo, mescola } from "../src/rng.js";

test("stesso seed -> stessa sequenza", () => {
  const a = prossimo(123);
  const b = prossimo(123);
  expect(a.val).toBe(b.val);
  expect(a.stato).toBe(b.stato);
});

test("avanza lo stato (val successivi diversi)", () => {
  const a = prossimo(123);
  const b = prossimo(a.stato);
  expect(a.val).not.toBe(b.val);
  expect(a.val).toBeGreaterThanOrEqual(0);
  expect(a.val).toBeLessThan(1);
});

test("mescola: deterministico e permutazione valida", () => {
  const arr = [1, 2, 3, 4, 5];
  const r1 = mescola(arr, 42);
  const r2 = mescola(arr, 42);
  expect(r1.arr).toEqual(r2.arr);
  expect(r1.arr.slice().sort()).toEqual([1, 2, 3, 4, 5]); // stessi elementi
  expect(arr).toEqual([1, 2, 3, 4, 5]); // input non mutato
});
```

- [ ] **Step 2: Run test (FAIL)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/rng.test.ts`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementa `engine/src/rng.ts`**

```ts
// PRNG mulberry32: deterministico, stato = intero 32 bit. Nessun Math.random/Date.
export interface PassoRng {
  val: number; // 0 <= val < 1
  stato: number;
}

export function prossimo(stato: number): PassoRng {
  let a = (stato + 0x6d2b79f5) | 0;
  let t = Math.imul(a ^ (a >>> 15), 1 | a);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  const val = ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  return { val, stato: a };
}

export function mescola<T>(arr: readonly T[], stato: number): { arr: T[]; stato: number } {
  const out = arr.slice();
  let s = stato;
  for (let i = out.length - 1; i > 0; i--) {
    const p = prossimo(s);
    s = p.stato;
    const j = Math.floor(p.val * (i + 1));
    const tmp = out[i];
    out[i] = out[j];
    out[j] = tmp;
  }
  return { arr: out, stato: s };
}
```

- [ ] **Step 4: Run test (PASS)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/rng.test.ts`
Expected: 3 test PASS.

- [ ] **Step 5: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/rng.ts engine/test/rng.test.ts
git commit -m "feat(engine): PRNG seedabile (mulberry32) + mescola deterministico"
```

---

## Task 3: Tipi stato / eventi / azioni

**Files:**
- Create: `engine/src/stato.ts`
- Create: `engine/src/eventi.ts`
- Create: `engine/src/azioni.ts`

- [ ] **Step 1: Crea `engine/src/stato.ts`**

```ts
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
```

- [ ] **Step 2: Crea `engine/src/eventi.ts`**

```ts
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
```

- [ ] **Step 3: Crea `engine/src/azioni.ts`**

```ts
import type { ConfigPartita } from "./stato.js";

// Tipo carte minimale locale: l'engine usa solo questi campi (disaccoppiato dal parser).
export interface DefCarta {
  defId: string;
  tipo: string;
}

export type DefinizioniCarte = Map<string, DefCarta>;
export type DeckList = string[]; // lista di defId

export type Azione =
  | { t: "iniziaPartita"; seed: number; config: ConfigPartita; mazzi: DeckList[]; carte: DefinizioniCarte }
  | { t: "avanzaFase" }
  | { t: "scarta"; iids: string[] };
```

- [ ] **Step 4: Verifica compilazione**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx tsc --noEmit`
Expected: nessun errore.

- [ ] **Step 5: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/stato.ts engine/src/eventi.ts engine/src/azioni.ts
git commit -m "feat(engine): tipi stato, eventi, azioni"
```

---

## Task 4: Setup partita (`setup.ts`)

**Files:**
- Create: `engine/src/setup.ts`
- Create: `engine/test/setup.test.ts`

- [ ] **Step 1: Scrivi i test (FAIL)**

```ts
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
  expect(stato.giocatori[0].mazzo).toHaveLength(3); // 5 - 2
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
```

- [ ] **Step 2: Run test (FAIL)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/setup.test.ts`
Expected: FAIL (iniziaPartita non trovato).

- [ ] **Step 3: Implementa `engine/src/setup.ts`**

```ts
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
```

- [ ] **Step 4: Run test (PASS)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/setup.test.ts`
Expected: 3 test PASS.

- [ ] **Step 5: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/setup.ts engine/test/setup.test.ts
git commit -m "feat(engine): iniziaPartita (shuffle seedato + distribuzione mano)"
```

---

## Task 5: Logica per-fase (`fasi.ts`)

Logica eseguita all'**ingresso** di una fase. L'orchestrazione (ordine, fine-turno) è in `engine.ts` (Task 6). `eseguiEntrataFase` NON cambia `turnoDi`/`numeroTurno` — solo la logica interna della fase sul giocatore attivo.

**Files:**
- Create: `engine/src/fasi.ts`
- Create: `engine/test/fasi.test.ts`

- [ ] **Step 1: Scrivi i test (FAIL)**

```ts
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
  expect(r.stato.giocatori[0].mano.length).toBe(manoPrima); // niente pesca
  expect(r.eventi.some((e) => e.t === "cartaPescata")).toBe(false);
});

test("pesca: giocatore non-primo pesca 1", () => {
  const { stato } = iniziaPartita({ seed: 1, config, mazzi, carte });
  stato.turnoDi = 1; // secondo giocatore
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
  stato.giocatori[1].mazzo = []; // mazzo vuoto
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
```

- [ ] **Step 2: Run test (FAIL)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/fasi.test.ts`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementa `engine/src/fasi.ts`**

```ts
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
      // upkeep, main1, combat, main2, end: nessuna logica automatica in E1
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
  // regola: il primo giocatore non pesca al turno 1
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
```

- [ ] **Step 4: Run test (PASS)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/fasi.test.ts`
Expected: 6 test PASS.

- [ ] **Step 5: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/fasi.ts engine/test/fasi.test.ts
git commit -m "feat(engine): logica per-fase (untap, pesca, deckout)"
```

---

## Task 6: Reducer principale (`engine.ts`)

Orchestrazione: dispatch azioni, avanzamento fasi, fine-turno (reset mana + passa turno), gate scarto-a-7, validazioni.

**Files:**
- Create: `engine/src/engine.ts`
- Create: `engine/test/engine.test.ts`

- [ ] **Step 1: Scrivi i test (FAIL)**

```ts
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
  // porta fino a end (6 avanzamenti: untap->...->end)
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
  // gonfia la mano del giocatore attivo oltre il limite
  const extra = stato.giocatori[0].mazzo.slice();
  stato = { ...stato, giocatori: stato.giocatori.map((g, i) => i === 0 ? { ...g, mano: [...g.mano, ...extra], mazzo: [] } : g) };
  for (let k = 0; k < 6; k++) { // fino a end
    const r = applica(stato, { t: "avanzaFase" });
    if (!r.ok) return; stato = r.stato;
  }
  expect(stato.fase).toBe("end");
  const bloccato = applica(stato, { t: "avanzaFase" });
  expect(bloccato.ok).toBe(false); // deve scartare prima
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
```

- [ ] **Step 2: Run test (FAIL)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/engine.test.ts`
Expected: FAIL (applica non trovato).

- [ ] **Step 3: Implementa `engine/src/engine.ts`**

```ts
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
  // Da "end" si chiude il turno e si passa al prossimo (se non c'è scarto pendente).
  if (stato.fase === "end") {
    if (daScartare(stato) > 0) {
      return { ok: false, errore: "devi scartare prima di passare il turno" };
    }
    return fineTurno(stato);
  }

  // Altrimenti avanza alla fase successiva ed eseguine l'ingresso.
  const idx = ORDINE_FASI.indexOf(stato.fase);
  const prossima = ORDINE_FASI[idx + 1];
  const r = eseguiEntrataFase({ ...stato, fase: prossima }, prossima);
  const eventi = [...r.eventi];

  // entrando in "end", se la mano supera il limite richiedi lo scarto
  if (prossima === "end" && daScartare(r.stato) > 0) {
    eventi.push({ t: "richiestaScarto", giocatore: r.stato.turnoDi, quantita: daScartare(r.stato) });
  }
  return { ok: true, stato: r.stato, eventi };
}

function fineTurno(stato: StatoPartita): Risultato {
  const att = stato.turnoDi;
  const n = stato.giocatori.length;
  const prossimo = (att + 1) % n;

  // azzera mana del giocatore che chiude
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

  // esegui l'ingresso di untap del nuovo giocatore attivo
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
  const eventi: Evento[] = scartate.map((c) => ({ t: "cartaScartata", giocatore: att, iid: c.iid }));
  return { ok: true, stato: { ...stato, giocatori }, eventi };
}
```

- [ ] **Step 4: Run test (PASS)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/engine.test.ts`
Expected: 7 test PASS.

- [ ] **Step 5: Verifica tsc**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx tsc --noEmit`
Expected: nessun errore.

- [ ] **Step 6: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/engine.ts engine/test/engine.test.ts
git commit -m "feat(engine): reducer applica (cicla fasi, fine-turno, scarto, validazioni)"
```

---

## Task 7: Loader carte (`carte-db.ts`)

Helper che trasforma il JSON del parser (`dist-motore/carte.json`) nella mappa `DefCarta` minimale usata dall'engine. È l'unico punto che tocca dati esterni; il core resta puro.

**Files:**
- Create: `engine/src/carte-db.ts`
- Create: `engine/test/carte-db.test.ts`

- [ ] **Step 1: Scrivi i test (FAIL)**

```ts
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
```

- [ ] **Step 2: Run test (FAIL)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/carte-db.test.ts`
Expected: FAIL (caricaCarte non trovato).

- [ ] **Step 3: Implementa `engine/src/carte-db.ts`**

```ts
import type { DefCarta, DefinizioniCarte } from "./azioni.js";

interface VoceJson {
  id?: string;
  tipo?: string;
}

// Trasforma il testo JSON di dist-motore/carte.json in Map<defId, DefCarta>.
export function caricaCarte(jsonText: string): DefinizioniCarte {
  const voci = JSON.parse(jsonText) as VoceJson[];
  const m: DefinizioniCarte = new Map<string, DefCarta>();
  for (const v of voci) {
    if (!v.id) continue;
    m.set(v.id, { defId: v.id, tipo: v.tipo ?? "" });
  }
  return m;
}
```

- [ ] **Step 4: Run test (PASS)**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run test/carte-db.test.ts`
Expected: 2 test PASS.

- [ ] **Step 5: Verifica integrazione su carte.json reale**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && node --input-type=module -e "import { readFileSync } from 'node:fs'; import { caricaCarte } from './src/carte-db.ts';" 2>/dev/null || npx tsx -e "import { readFileSync } from 'node:fs'; import { caricaCarte } from './src/carte-db.js'; const m = caricaCarte(readFileSync('../dist-motore/carte.json','utf8')); console.log('carte caricate:', m.size);"`
Expected: stampa "carte caricate: 317".

- [ ] **Step 6: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/src/carte-db.ts engine/test/carte-db.test.ts
git commit -m "feat(engine): loader carte.json -> mappa DefCarta"
```

---

## Task 8: Verifica finale + replay

**Files:**
- Create: `engine/test/replay.test.ts`

- [ ] **Step 1: Scrivi il test replay (determinismo end-to-end)**

```ts
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
  // almeno uno dei due giocatori ha ordine mazzo diverso
  expect(JSON.stringify(a.giocatori[0].mazzo)).not.toBe(JSON.stringify(b.giocatori[0].mazzo));
});
```

- [ ] **Step 2: Run tutta la suite + tsc**

Run: `cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco/engine" && npx vitest run && npx tsc --noEmit`
Expected: tutti i test PASS (rng, setup, fasi, engine, carte-db, replay, smoke), tsc pulito.

- [ ] **Step 3: Commit**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git add engine/test/replay.test.ts
git commit -m "test(engine): replay deterministico end-to-end"
```

- [ ] **Step 4: Push branch**

```bash
cd "/Users/lucacaucci/Desktop/Gioco Alessandro/gioco"
git push -u fork feature/engine-core-e1
```
(Push sul fork `lukarmate/gioco`; aprire PR verso `apeizon:main` quando opportuno. NB: `lukarmate` ha solo read su `apeizon/gioco`.)

---

## Self-Review (eseguita)

**Spec coverage:**
- Funzione pura `applica` con Risultato ok/errore → Task 6 ✓
- RNG seedato deterministico → Task 2 ✓
- Stato (zone, mana, 2-4 giocatori, fase) → Task 3 ✓
- iniziaPartita (shuffle, distribuzione, primo) → Task 4 ✓
- Ciclo fasi + untap + pesca + primoNonPescaT1 + deckout → Task 5 ✓
- Fine-turno (mana reset, passa turno, rotazione %N) + scarto-a-7 gate → Task 6 ✓
- Eventi union → Task 3 (eventi.ts), emessi in Task 4/5/6 ✓
- Deckout perdita_immediata + danno_per_turno → Task 5 ✓
- Loader carte.json → DefCarta → Task 7 ✓
- Determinismo/replay → Task 8 ✓
- Azioni illegali → ok:false → Task 6 (test) ✓

**Placeholder scan:** nessun TBD/TODO; tutto il codice è completo.

**Type consistency:** `applica` ritorna `Risultato`; `eseguiEntrataFase` ritorna `RisultatoFase`; `iniziaPartita` ritorna `{stato,eventi}`. `DefCarta {defId,tipo}` usato coerentemente in setup/carte-db/azioni. `daScartare`, `fineTurno`, `scarta` interni a engine.ts. Nomi eventi (`manaAzzerato`, `turnoPassato`, `richiestaScarto.quantita`) coerenti tra eventi.ts e engine.ts.

**Nota numeroTurno:** incrementa a ogni turno-giocatore (non per round). `primoNonPescaT1` scatta solo a `numeroTurno===1` (primo turno del primo giocatore). Coerente col regolamento (solo chi va primo salta la prima pesca).
