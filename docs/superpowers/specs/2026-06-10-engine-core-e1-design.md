# Spec — Engine E1: Core loop + eventi

**Data:** 2026-06-10
**Slice:** E1 (primo di 7 dell'engine-runtime). Core loop deterministico + stream eventi.
**Stato:** approvato (design), pronto per piano implementazione

---

## 1. Obiettivo

Costruire lo **scheletro runtime** del gioco: una funzione pura che fa girare turni e fasi
ed emette uno **stream di eventi**. È la fondamenta su cui si agganciano tutti gli slice
successivi (giocare carte, effetti, combattimento, …) e lo stream eventi è ciò che il
layer animazione consumerà.

E1 fa girare una "partita vuota": setup → cicla untap/upkeep/pesca/main1/combat/main2/end
→ passa turno, con pesca, scarto-a-7, regola primo-non-pesca, deckout. **Nessuna carta si
gioca, nessun combattimento** (slice successivi).

### Decomposizione engine (contesto)
E1 è il primo di 7 slice ordinati per dipendenza:
1. **E1 — Core loop + eventi** (questo)
2. E2 — Giocare permanenti + mana (avamposti→mana, creature vanilla, summoning sickness)
3. E3 — Interprete effetti (esegue i verbi di `carte.json`)
4. E4 — Combattimento
5. E5 — Stack & priorità (Istanti)
6. E6 — Vittoria/obiettivi/respawn (3-vite)
7. E7 — Leader/Alleati

---

## 2. Principio architetturale

**Engine = funzione pura.** Nessuna mutazione, nessun side-effect, nessun I/O.

```ts
applica(stato, azione):
  | { ok: true; stato: StatoPartita; eventi: Evento[] }
  | { ok: false; errore: string }
```

Stesso `(stato, azione)` → stesso risultato sempre. Conseguenze:
- **Testabile** al 100% (input→output puro)
- **Replay** da log azioni (rigioca le azioni → stesso stato)
- **Multiplayer autoritativo** pronto (il server rigioca le azioni dei client)

**Determinismo / RNG:** la casualità (shuffle) usa un **PRNG seedabile** il cui stato vive
dentro `StatoPartita`. Nessun `Math.random`/`Date.now` globale (vietati nell'ambiente e
comunque incompatibili col replay). Il seed si passa a `iniziaPartita`.

**Azione illegale → `{ok:false, errore}`**, mai crash. L'engine valida ogni azione contro
lo stato corrente.

---

## 3. Stato

```ts
type Fase = "untap" | "upkeep" | "pesca" | "main1" | "combat" | "main2" | "end";

interface ConfigPartita {
  hpIniziali: number;          // 30 o 50 (configurabile)
  manoIniziale: number;        // 6
  limiteMano: number;          // 7
  primoNonPescaT1: boolean;    // true (regola anti-vantaggio)
  penalitàMazzoVuoto: "perdita_immediata" | "danno_per_turno";
  dannoMazzoVuoto?: number;    // usato se penalità = danno_per_turno
}

interface ManaPool {
  // mana per fazione + generico; in E1 sempre vuoto (avamposti = E2)
  nord: number; sud: number; est: number; ovest: number; centro: number; generico: number;
}

interface CartaIstanza {
  iid: string;        // id istanza unico (targeting + animazioni)
  defId: string;      // id definizione in carte.json (es. "FORGIA_DELLA_FIAMMA")
  proprietario: number;
  tappata: boolean;
  // contatori/segnalini → E3
}

interface Giocatore {
  id: number;
  hp: number;
  mazzo: CartaIstanza[];     // ordinato, cima = indice 0
  mano: CartaIstanza[];
  campo: CartaIstanza[];     // vuoto in E1
  cimitero: CartaIstanza[];
  esilio: CartaIstanza[];
  manaDisponibile: ManaPool; // azzerato a fine turno
  morti: number;             // sistema 3-vite (E6); presente ma non usato in E1
}

interface StatoPartita {
  config: ConfigPartita;
  rng: number;               // stato PRNG (mulberry32: un intero a 32 bit)
  giocatori: Giocatore[];    // 2..4
  turnoDi: number;           // indice giocatore attivo
  numeroTurno: number;       // parte da 1
  fase: Fase;
  primoGiocatore: number;
  finita: boolean;
  vincitore?: number;
}
```

**Definizioni carte** = `carte.json` (output del parser, slice motore). In E1 servono solo
`tipo`/`costo`/`atk`/`def` (già presenti). Le istanze referenziano `defId`.

```ts
type DefinizioniCarte = Map<string, CartaParsata>; // chiave = defId

type DeckList = string[]; // lista di defId (60 voci); la costruzione mazzi è un altro slice
```

L'engine è **puro**: le definizioni si passano a `iniziaPartita`, non si leggono da disco
nel core. `carte-db.ts` è un helper di caricamento per app/test.

---

## 4. Azioni

```ts
type Azione =
  | { t: "iniziaPartita"; seed: number; config: ConfigPartita; mazzi: DeckList[]; carte: DefinizioniCarte }
  | { t: "avanzaFase" }
  | { t: "scarta"; iids: string[] };
```

| Azione | Effetto |
|---|---|
| `iniziaPartita` | crea N giocatori da `mazzi`, istanzia le carte (iid univoci), mescola ogni mazzo (Fisher-Yates con RNG seedato), distribuisce `manoIniziale` carte a testa, sceglie `primoGiocatore` (indice 0), entra turno 1 fase `untap`. Emette `partitaIniziata` + le pescate iniziali. |
| `avanzaFase` | avanza alla fase successiva ed esegue la sua logica automatica (vedi §5). Da `end` → passa al prossimo giocatore e incrementa `numeroTurno`. |
| `scarta` | risolve il punto-decisione scarto-a-`limiteMano` a fine turno. Valida che gli `iids` siano in mano e di numero corretto. |

Validazioni: `avanzaFase`/`scarta` su partita `finita` → `{ok:false}`. `scarta` quando non
richiesto, o `iids` errati/in numero sbagliato → `{ok:false}`. `iniziaPartita` con <2 o >4
mazzi → `{ok:false}`.

---

## 5. Ciclo fasi e logica automatica

Ordine per turno: `untap → upkeep → pesca → main1 → combat → main2 → end → (prossimo turno)`.

| Fase | Logica automatica (E1) | Eventi |
|---|---|---|
| `untap` | stappa tutte le carte tappate del giocatore attivo (in E1 il campo è vuoto → nessuna) | `faseEntrata`, `cartaStappata`* |
| `upkeep` | nessuna (effetti upkeep = E3) | `faseEntrata` |
| `pesca` | il giocatore attivo pesca 1 dalla cima del mazzo, **salvo** `primoGiocatore` al `numeroTurno===1` se `primoNonPescaT1`. Se mazzo vuoto → penalità `mazzoVuoto` | `faseEntrata`, `cartaPescata` \| `mazzoVuoto` |
| `main1` | pass-through (in E1) | `faseEntrata` |
| `combat` | pass-through | `faseEntrata` |
| `main2` | pass-through | `faseEntrata` |
| `end` | se `mano.length > limiteMano` → emette `richiestaScarto` e **blocca** l'avanzamento finché non arriva `scarta`. Poi azzera il mana dell'attivo, passa a `(turnoDi+1)%N`, `numeroTurno++`, entra `untap` del prossimo | `richiestaScarto` \| (`manaAzzerato`, `turnoPassato`, `turnoIniziato`, `faseEntrata`) |

**Deckout** (`penalitàMazzoVuoto`):
- `perdita_immediata` (default): il giocatore che non può pescare perde → `partitaFinita`
  (in 1v1 l'altro vince; in FFA il giocatore è eliminato — la gestione FFA completa
  dell'eliminazione è E6, in E1 si chiude la partita).
- `danno_per_turno`: il giocatore perde `dannoMazzoVuoto` HP invece di pescare; se HP ≤ 0
  → `partitaFinita`.

E1 chiude la partita **solo** su deckout. Vittoria per HP/obiettivi/3-vite = E6.

**Stato "in attesa di scarto":** quando `end` richiede lo scarto, `avanzaFase` successivi
ritornano `{ok:false}` finché non si esegue `scarta` con il numero corretto di carte.
(Non serve un campo extra: si deduce da `fase==="end" && mano.length>limiteMano`.)

---

## 6. Eventi (lo stream per la view)

```ts
type Evento =
  | { t: "partitaIniziata"; giocatori: number[]; primo: number }
  | { t: "turnoIniziato"; giocatore: number; numeroTurno: number }
  | { t: "faseEntrata"; fase: Fase }
  | { t: "cartaStappata"; iid: string }
  | { t: "cartaPescata"; giocatore: number; iid: string }
  | { t: "mazzoVuoto"; giocatore: number }
  | { t: "manaAzzerato"; giocatore: number }
  | { t: "richiestaScarto"; giocatore: number; quantità: number }
  | { t: "cartaScartata"; giocatore: number; iid: string }
  | { t: "turnoPassato"; da: number; a: number }
  | { t: "partitaFinita"; vincitore?: number; motivo: string };
```

**Info nascosta:** l'engine emette eventi completi (con `iid`/`defId`). Nascondere la mano
avversaria o gli obiettivi segreti (regolamento §9) è compito del **layer presentazione/
trasporto**, che redige gli eventi per ogni viewer. L'engine resta puro e onnisciente.

---

## 7. Struttura file (modulo `engine/`)

`engine/` è un modulo **runtime puro**, separato da `motore/` (tool parser build-time con
CLI). Separati così l'app non si trascina parser/CLI. (Nota nomi: "motore" in IT = engine;
distinti per ruolo — `motore`=parser, `engine`=runtime.)

```
engine/
├─ package.json          vitest + tsx (nessuna dipendenza app)
├─ tsconfig.json         (con @types/node, come motore/)
├─ src/
│  ├─ stato.ts           StatoPartita, Giocatore, CartaIstanza, Fase, ConfigPartita, ManaPool
│  ├─ eventi.ts          union Evento
│  ├─ azioni.ts          union Azione + DeckList, DefinizioniCarte
│  ├─ rng.ts             mulberry32: next(stato)→{val:number(0..1), stato:number}; mescola(arr, stato)→{arr, stato}
│  ├─ setup.ts           iniziaPartita: istanzia carte, shuffle, distribuisce, primo, turno 1
│  ├─ fasi.ts            logica per-fase (untap/upkeep/pesca/main1/combat/main2/end)
│  ├─ engine.ts          applica(stato, azione) → dispatch + validazione
│  └─ carte-db.ts        helper: legge carte.json → Map<defId, CartaParsata>
└─ test/
   ├─ rng.test.ts
   ├─ setup.test.ts
   ├─ fasi.test.ts
   └─ engine.test.ts
```

**Tipi condivisi col parser:** `CartaParsata` vive in `motore/src/schema.ts`. L'engine può
ridichiarare un tipo minimale `DefCarta { defId; tipo; costo?; ... }` per non accoppiarsi al
modulo parser, oppure importare il tipo. Decisione in fase di piano: preferire un tipo
minimale locale `DefCarta` per disaccoppiamento (l'engine usa solo pochi campi).

---

## 8. Testing (TDD)

- **rng**: stesso seed → stessa sequenza; `mescola` deterministico e permutazione valida.
- **setup**: ogni giocatore riceve `manoIniziale` carte; mazzo residuo = 60−manoIniziale;
  `iid` tutti univoci; `primoGiocatore` impostato; emette `partitaIniziata`.
- **fasi**: `untap` stappa le carte tappate; `pesca` sposta cima mazzo→mano + evento;
  `primoNonPescaT1` salta la pesca al turno 1 del primo; `end` azzera mana + passa turno;
  `(turnoDi+1)%N` con N=2,3,4.
- **engine**: un ciclo turno completo emette gli eventi nell'ordine atteso; rotazione
  turni 2/3/4 giocatori; deckout (`perdita_immediata` e `danno_per_turno`) → `partitaFinita`;
  scarto-a-7 (richiestaScarto blocca, poi `scarta` sblocca); azioni illegali → `{ok:false}`.

---

## 9. Criteri di successo

1. `iniziaPartita` con 2 mazzi reali (defId da `carte.json`) produce uno stato valido +
   eventi di setup, deterministico per seed dato.
2. Una sequenza di `avanzaFase` fa girare turni completi per 2, 3 e 4 giocatori, emettendo
   lo stream eventi corretto.
3. Lo stesso seed + stessa sequenza di azioni → stato finale identico (replay).
4. Deckout chiude la partita; scarto-a-7 funziona; regola primo-non-pesca rispettata.
5. Ogni azione illegale ritorna `{ok:false, errore}` senza eccezioni.
6. Engine puro: nessun accesso a filesystem/`Math.random`/`Date` nel core (`src/` tranne
   `carte-db.ts` helper).

---

## 10. Cosa NON include E1 (slice successivi)

- Giocare carte / mana da avamposti (E2)
- Interprete effetti / verbi (E3)
- Combattimento, summoning sickness (E4)
- Stack & priorità / Istanti (E5)
- Vittoria/obiettivi/3-vite, eliminazione FFA completa (E6) — E1 chiude solo su deckout
- Leader/Alleati, contatori utilizzo, evoluzione (E7)
- Rete / redazione per-viewer / AI / UI

---

## 11. Note / rischi

| Rischio | Mitigazione |
|---|---|
| Accoppiamento engine↔parser via tipi | Tipo locale minimale `DefCarta` nell'engine; usa solo i campi necessari |
| `carte.json` non ancora su `main` (PR motore aperta) | Branch E1 parte dal branch motore; rebase su main al merge |
| Stato "in attesa scarto" implicito può confondere | Dedotto da `fase/mano`; documentato; coperto da test espliciti |
| FFA: eliminazione vs partita-finita | In E1 deckout chiude la partita; eliminazione FFA progressiva = E6 |
