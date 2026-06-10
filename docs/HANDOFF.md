# HANDOFF — Stato lavori Gioco TCG

> File di passaggio aggiornato a fine di ogni sessione. Dice **dove siamo** e **cosa manca**.
> Ultimo aggiornamento: **2026-06-10**

---

## 1. Visione (dove vogliamo arrivare)

App mobile TCG dark-fantasy (React Native + Expo). Catena di costruzione:

```
carte (markdown, le scrive il socio)
   → PARSER        → carte.json (dati strutturati)
   → ENGINE        → stream di eventi (logica di partita pura)
   → ANIMAZIONI    → eventi mostrati a schermo
   → UI/APP        → gioco giocabile
```

Divisione lavoro: **socio** (`apeizon`) scrive le carte; **Luca** fa tutto il resto (app, motore, grafica). Repo `apeizon/gioco` (Luca = collaboratore read-only → si lavora via fork `lukarmate/gioco` + PR).

---

## 2. FATTO ✅

### Parser carte (`motore/`)
- Traduce le carte in prosa → `dist-motore/carte.json` strutturato.
- Grammatica frasi-template + validatore; carte troppo complesse → `MOTORE-DA-FARE.md` (coda handler custom).
- CI GitHub Action: su push rigenera output + valida.
- Cheat-sheet per il socio: `docs/motore/grammatica.md`.
- **Copertura: 62%** (52 CAT1 vanilla · 144 CAT2 parsate · 121 CAT3 da fare a mano), su 317 carte.
- Avamposti (95) classificati per archetipo da cartella (non per prosa).
- **PR #1 aperta:** `github.com/apeizon/gioco/pull/1` (branch `feature/motore-parser-carte`).

### Engine E1 — Core loop (`engine/`)
- Scheletro runtime: `applica(stato, azione) → {ok, stato, eventi[]} | {ok:false, errore}`. Funzione **pura**, deterministica (RNG seedato, replay testato).
- Fa girare turni/fasi (untap→upkeep→pesca→main1→combat→main2→end), pesca, scarto-a-7, regola primo-non-pesca, deckout, 2-4 giocatori.
- Emette lo **stream eventi** (carta pescata, turno passato, ecc.) — base per le animazioni.
- **28 test verdi**, tsc pulito.
- Branch `feature/engine-core-e1` (pushato sul fork). PR engine **non ancora aperta** (aspetta merge della PR #1 motore, poi rebase su main).

---

## 3. IN CORSO / PROSSIMO PASSO ⏳

Nessun lavoro attivo in esecuzione. Bivio deciso a inizio prossima sessione:
- **E2** (giocare permanenti + mana), oppure
- **Slice animazioni** (event-bus + sequencer + registro animazioni, prototipabile con gli eventi di E1).

---

## 4. ROADMAP — cosa manca 🗺️

### Engine (7 slice, E1 fatto)
| Slice | Cosa | Stato |
|---|---|---|
| E1 | Core loop + eventi | ✅ fatto |
| E2 | Giocare permanenti + mana (avamposti→mana, creature vanilla, summoning sickness) | ⬜ |
| E3 | Interprete effetti (esegue i verbi di carte.json) | ⬜ |
| E4 | Combattimento (attacco/blocco/danno/morti) | ⬜ |
| E5 | Stack & priorità (Istanti, LIFO) | ⬜ |
| E6 | Vittoria/obiettivi segreti/respawn 3-vite | ⬜ |
| E7 | Leader/Alleati (commander, fedeltà, evoluzione) | ⬜ |

### Altri pezzi
- **Parser**: estendere vocabolario per abbassare i 121 CAT3 (long tail: upkeep composti, trigger reattivi, abilità attivate) — opzionale/incrementale.
- **Animazioni**: event-bus + sequencer + registro animazioni. Stack RN: Reanimated + GestureHandler + Skia (VFX) + Lottie.
- **UI/App**: campo battaglia, mano, zona comando, ecc. (slice `fondamenta-menu` già avviato a parte).
- **Deck-building / collezione**: costruzione mazzi 60 carte + mazzo obiettivi.
- **Multiplayer / rete**: redazione eventi per-viewer (info nascosta), server autoritativo.

---

## 5. Decisioni chiave (per non ridiscuterle)
- Testo carte: **"tutte/ogni creatura" senza proprietario = tutte, tue + avversario**.
- Avamposti gestiti **per cartella/archetipo**, non parsando la prosa.
- Engine = **funzione pura** `applica()`, stato immutabile, RNG seedato (replay/multiplayer-ready).
- Giocatori modellati come **array 2-4** (FFA pronto).
- Animazioni: **logica ≠ animazione** — engine emette eventi, la view li consuma.

---

## 6. Come riprendere
1. Leggi questo file + i relativi spec/piani in `docs/superpowers/`.
2. Stato repo: branch `feature/engine-core-e1` (engine), `feature/motore-parser-carte` (parser, PR #1).
3. Test engine: `cd engine && npm test`. Test parser: `cd motore && npm test`.
4. Memoria persistente Claude: `project-gioco-tcg.md` (dettagli tecnici + decisioni).

---

## 7. Riferimenti file
- Regolamento completo: `regole/REGOLE_BASE_TCG.md`
- Spec/piani: `docs/superpowers/specs/` e `docs/superpowers/plans/` (motore + engine E1)
- Cheat-sheet socio: `docs/motore/grammatica.md`
- Coda carte da fare a mano: `MOTORE-DA-FARE.md`
