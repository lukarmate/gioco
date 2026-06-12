# HANDOFF — Stato lavori Gioco TCG

> File di passaggio aggiornato a fine di ogni sessione. Dice **dove siamo** e **cosa manca**.
> Ultimo aggiornamento: **2026-06-10**

---

## ⚠️ DECISIONE 2026-06-10: SI PASSA A UNITY (C#)

**Mira grafica = mix tra Marvel Snap e Magic** (carte juicy, VFX pesanti). Entrambe le referenze sono fatte in Unity. React Native + Skia/Lottie non arriva a quel livello di juice → si combatte il framework.

**Cosa significa:**
- Il progetto futuro è un **progetto Unity in C#** sul disco.
- Claude Code lavora editando i file `.cs`/scene/prefab da qui; Luca tiene **Unity Editor aperto** e preme Play per vedere i risultati (loop manuale: io edito → tu testi → mi dici cosa rompe → fixo).
- **NON** esiste integrazione live: io scrivo file, non "vedo" l'editor. (Esistono MCP server Unity community per leggere console/stato editor — valutare in futuro, setup extra.)

**Cosa si riusa e cosa si butta:**
- ✅ **Il DESIGN si riusa**: engine come funzione pura, stream di eventi (logica ≠ animazione), `carte.json` come formato dati, flusso socio (markdown → parser → JSON).
- ✅ **`carte.json` riusabile**: C# legge lo stesso JSON. Il socio continua a scrivere carte in markdown.
- ❌ **Il CODICE TypeScript si riscrive in C#**: engine E1 (28 test) e parser vanno re-implementati. Le *idee* restano, il codice no.

**Da decidere a inizio prossima sessione (Unity):**
- ✅ DECISO: **3D** (Universal 3D / URP, stile Snap — carte in spazio 3D + VFX).
- VFX day-1 o polish finale?
- Livello Unity/C# di Luca (parte da zero?) → influenza il piano.
- Setup repo: il progetto Unity sta nello stesso repo o nuovo? (`.gitignore` Unity, Git LFS per asset pesanti).
- Portare engine puro + parser in C#: primo slice di ripartenza.

**Stato vecchio stack RN/TS (congelato, non cancellato):** parser `motore/` (62%, PR #1 aperta) + engine `engine/` (E1, 28 test, branch `feature/engine-core-e1`). Serve come **reference di design** per la riscrittura C#, non come base di codice viva.

### ✅ FATTO 2026-06-10 (sessione Unity): Engine E1 portato in C#
- Nuova cartella `engine-cs/` — libreria **C# standalone**, target `netstandard2.1` (importabile da Unity), **zero dipendenze Unity**.
- `Engine.Core/` = porting fedele di E1: `Stato.cs`, `Eventi.cs`, `Azioni.cs`, `CarteDb.cs`, `Rng.cs` (mulberry32 bit-fedele al TS), `Setup.cs`, `Fasi.cs`, `Engine.cs`. Stile immutabile con `record` + `with` (al posto degli spread TS). `IsExternalInit.cs` = polyfill per init-setter su netstandard2.1.
- `Engine.Tests/` = 28 test portati (xUnit, net8.0). **28/28 verdi.**
- Build/test: `cd engine-cs && dotnet test` (sln `Engine.sln`).
- **.NET SDK 8.0.422** installato in `~/.dotnet` (no sudo, via dotnet-install.sh). Per usarlo: `export PATH="$HOME/.dotnet:$PATH"`.
- Unity NON ancora installato (Luca a zero Unity, download in corso lato suo). Stile grafico (3D Snap vs 2D Arena) ancora da decidere.

### ✅ FATTO 2026-06-10: Engine E2 (mana + permanenti) in C# — TDD
Cartella `engine-cs/`, stesso stile (record immutabili, funzione pura). **55/55 test verdi** (28 E1 + 27 E2).
- ✅ **A — Mana**: `Mana.cs`. `ManaCosto` (record, 5 colori + generico) + `ManaCosto.Parse(string)` che legge i costi del parser ("1 Centro", "2 Est + 1", "1 Centro più 1 mana qualsiasi"). `Mana.Paga(pool, costo)` → pool aggiornato o null (colorato esatto, generico da qualsiasi colore residuo). `ManaProdotto(Quantita, Colori, Scelta)` per gli avamposti. Test: `ManaTests.cs` (8).
- ✅ **B — GiocaAvamposto**: azione `GiocaAvamposto(iid)`. Solo Main Phase, max 1/turno (`Giocatore.AvampostoGiocatoQuestoTurno`), mano→campo, evento `AvampostoGiocato`. Test: `GiocaAvampostoTests.cs` (5).
- ✅ **C — AttivaAvamposto**: azione `AttivaAvamposto(iid, scelte?)`. Tappa l'avamposto, produce mana nel pool. `Scelta:false`→colore fisso; `Scelta:true`→terra duale, `scelte` sceglie il colore (validato). Evento `ManaGenerato`. Test: `AttivaAvampostoTests.cs` (7).
- ✅ **D — GiocaCreatura**: azione `GiocaCreatura(iid)`. Solo Main Phase, paga `ManaCosto` dal pool (`Mana.Paga`), mano→campo, marca summoning sickness (`CartaIstanza.EntrataQuestoTurno=true`). Evento `CreaturaGiocata`. Test: `GiocaCreaturaTests.cs` (5).
- ✅ **E — untap E2**: l'untap del giocatore attivo azzera `EntrataQuestoTurno` (sickness) di tutte le sue carte in campo e resetta `AvampostoGiocatoQuestoTurno`. Esteso `Fasi.Untap`. Test: `UntapE2Tests.cs` (2).

**Prossimo engine: E3** (interprete effetti — esegue i verbi di carte.json) o **E4** (combattimento: attacco/blocco/danno/morti, usa già summoning sickness + tap di E2).

**Modifiche al modello (E2):** `DefCarta` esteso (Costo/Produzione/Atk/Def opzionali). `StatoPartita.Carte` (dict defId→DefCarta) ora popolato a `iniziaPartita` così l'engine consulta costi/stat durante il gioco. `CartaIstanza.EntrataQuestoTurno`, `Giocatore.AvampostoGiocatoQuestoTurno`. Eventi nuovi: `AvampostoGiocato`, `ManaGenerato`, `CreaturaGiocata`.

### ✅ Unity creato + engine collegato (2026-06-10)
- Unity Hub + Editor **6.4 (6000.4.10f1) Apple Silicon** installati.
- **Stile grafico DECISO: 3D** (Snap×Magic). Progetto = template **Universal 3D** (URP), aperto e funzionante.
- Progetto in **`gioco/GiocoTCG/`** (era stato creato per errore in `~/Gioco Alessandro`, poi spostato nel repo).
- `gioco/GiocoTCG/.gitignore` Unity standard (ignora `Library/`, `Temp/`, csproj/sln generati, ecc.).

**Wiring engine ↔ Unity (architettura DLL):**
- Per importare l'engine pulito in Unity, `Engine.Core` è stato reso **senza dipendenze esterne**: `CarteDb` (usa System.Text.Json) estratto in nuovo progetto **`Engine.Data`** (`engine-cs/Engine.Data/`). Tests aggiornati, **55/55 ancora verdi**.
- `Engine.Core.dll` (netstandard2.1) copiata in **`GiocoTCG/Assets/Plugins/Engine/Engine.Core.dll`** — consumata dal gioco. NON gitignorata (è l'artefatto; rigenerabile).
- **Rebuild engine per Unity:** `engine-cs/build-for-unity.sh` (build Release + copia DLL). Lanciarlo dopo ogni modifica all'engine, poi tornare in Unity (ricompila da solo).
- Script di verifica: **`GiocoTCG/Assets/Scripts/EngineSmokeTest.cs`** — gira all'avvio del Play, logga eventi engine in Console. Verifica il collegamento; da cancellare dopo.

**✅ VERIFICATO 2026-06-10:** Play in Unity → Console mostra i log `[Engine]` (partita iniziata, fasi che avanzano, "mano P0 = 2 carte ✅"), zero errori. Engine C# gira dentro Unity. Collegamento end-to-end confermato.

### 🟡 IN CORSO 2026-06-10: prima vista 3D (Unity)
- `EngineSmokeTest.cs` **rimosso** (verifica completata).
- Nuovo: **`GiocoTCG/Assets/Scripts/CampoView.cs`** — gira al Play (RuntimeInitialize, niente da configurare in editor). Costruisce una partita via engine, inquadra la camera, crea un tavolo (plane) e disegna la **mano del giocatore 0 come carte 3D** (rettangoli colorati a ventaglio stile Snap). Versione 0: niente arte/testo, solo forme colorate — serve a vedere il ponte engine→grafica.
- Materiali: helper `MatUrp` usa shader `Universal Render Pipeline/Lit` con `_BaseColor` (URP non usa `_Color`).
- **✅ VERIFICATO 2026-06-10:** Play → tavolo scuro + 5 carte colorate a ventaglio nel Game view, log `[View] mano P0 (5): Lich, Angelo, Mago, Golem, Fata`. Ponte engine→grafica 3D confermato. Primo pezzo visibile del gioco.

**✅ FATTO 2026-06-11: testo sulle carte (TextMeshPro).** Ogni carta ha 3 etichette TMP world-space figlie (nome, costo totale, ATK/DEF), bianco bold + contorno nero, counter-scale per annullare la scala non uniforme della carta, rot 180Y per affacciarsi alla camera. Richiede TMP Essential Resources (importati, committati in `Assets/TextMesh Pro/`). NB: se il testo non rende → `Window > TextMeshPro > Import TMP Essential Resources`.

**Prossimi passi vista:** (1) ✅ testo carte fatto, (2) mostrare anche il campo e gli HP, (3) consumare lo *stream eventi* per animare (pesca, gioca carta), (4) input per giocare le carte. Poi arte vera.

### ✅ FATTO 2026-06-11: Engine E3a (interprete effetti) — trigger ETB, TDD
**74/74 test verdi** (E1 28 + E2 27 + E4 11 + E3a 8). File: `Effetti.cs`, test `EffettiEtbTests.cs`.
- **Modello (AST):** `Effetto(Trigger, Azioni[])`. `Trigger` enum (Etb/Morte/Upkeep/Attacco/Attivata/Passiva). `AzioneEffetto` = DU dei verbi. `Bersaglio(Tipo, Proprietario, Quantificatore, Filtro)` — `Proprietario` relativo al controllore (Tue/Avversario/Tutti), `Quantificatore` (Una/Tutte/Ogni).
- **Verbi E3a (deterministici):** `Pesca(n)` (il controllore pesca n), `GeneraMana(n,colore)`, `InfliggiDanno(bersaglio giocatore, n)` (gestisce morte→PartitaFinita 2p), `Distruggi(bersaglio creatura)` (Tutte/Ogni), `Mill(bersaglio giocatore, n)`.
- **Wiring:** trigger **Etb** scatta dentro `GiocaCreaturaImpl` subito dopo l'arrivo in campo; eventi effetto appesi a `CreaturaGiocata`.
- **Modello esteso:** `DefCarta.Effetti` (lista opzionale). Nuovo evento `CartaMacinata`.
- **Formato carte.json effetti** (catalogato): `effetti:[{trigger, azioni:[{verbo, ...}]}]`. Trigger nel data: passiva(141), attivata(4), etb(3), morte(2), upkeep(1), attacco(1). Verbi: avamposto(95, = produzione mana già in E2), modifica_stat_combo(14), modifica_stat(10), concedi_keyword(9), pesca(9), genera_mana(4), mill(4), infliggi_danno(3), + distruggi/genera_token/applica_stat/segnalino_stat (1 ciascuno).

### ✅ FATTO 2026-06-11: Engine E3b (loader JSON→AST) — TDD
**85/85 test verdi** (E3b +11, incluso smoke sul dataset reale). File: `Engine.Data/CarteDb.cs` esteso, test `CarteDbEffettiTests.cs`.
- `CarteDb.CaricaCarte` ora popola **Costo** (parse stringa via `ManaCosto.Parse`), **Produzione** (dal verbo `avamposto {mana:{quantita,colori,scelta}}` → `ManaProdotto`), **Effetti** (AST E3 per i verbi supportati).
- **Mappatura verbi** (solo quelli che l'executor E3a interpreta): `pesca`, `genera_mana`, `infliggi_danno`, `distruggi`, `mill`. Verbi non ancora supportati → **scartati** (E3c). Effetto senza azioni residue → omesso.
- **Mappatura target→Bersaglio:** proprietario (TUE/AVVERSARIO/TUTTI), quantificatore (una/tutte/ogni), filtro. Convenzione: `mill` senza target → giocatore Avversario.
- **Smoke reale:** `DatasetReale_caricaSenzaErrori` risale a `dist-motore/carte.json`, carica tutte le 317 carte, verifica costi/produzioni/effetti non vuoti.

**⚠️ GAP NOTO (parser a monte):** `carte.json` **non contiene ATK/DEF** (il parser non li estrae dal markdown) né campo `produzione` top-level. Quindi le creature caricate da `CarteDb` hanno `Atk/Def = null` → non combattono/non si pagano con stat reali finché il parser non viene esteso. I test E2/E4 usano `DefCarta` costruiti a mano con stat. **TODO parser:** estrarre atk/def dai markdown delle creature.

### ✅ FATTO 2026-06-11: Engine E3c.1 (trigger upkeep/attacco + genera_token) — TDD
**89/89 test verdi** (E3c.1 +4). File toccati: `Effetti.cs`, `Fasi.cs`, `Engine.cs`, `Eventi.cs`, `CarteDb.cs`; test `EffettiTriggerTests.cs`.
- **Trigger `upkeep`:** `Fasi.Upkeep` ora fa scattare gli effetti upkeep dei permanenti in campo del giocatore attivo all'ingresso della fase (snapshot del campo prima del loop).
- **Trigger `attacco`:** `DichiaraAttaccoImpl` fa scattare gli effetti `attacco` di ogni attaccante alla dichiarazione.
- **Verbo `genera_token`:** `GeneraToken(Nome, Atk, Def, Controllore)`. Crea una CartaIstanza token nel campo (summoning-sick), registra la sua `DefCarta` (con stat) in `stato.Carte`, iid deterministico (`sorgente#tokN`). Evento `TokenGenerato`. Mappato anche nel loader (`CarteDb`).

### ✅ FATTO 2026-06-11: Engine E3c.2 (trigger morte + attivata) — TDD
**96/96 test verdi** (E3c.2 +8). File: `Effetti.cs`, `Engine.cs`, `Azioni.cs`; test `EffettiMorteTests.cs`, `AttivaAbilitaTests.cs`.
- **Trigger `morte`:** `Effetti.EseguiMorti(stato, morti, ev)` fa scattare gli effetti morte (controllore = proprietario del morto). Agganciato sia al **combat** (`DichiaraBlocchiImpl`, dopo la risoluzione) sia al **verbo `Distruggi`**. Cascata naturale (board finito → termina; i verbi non-Distruggi non creano morti → niente loop infinito).
- **Trigger `attivata`:** nuova azione `AttivaAbilita(iid)`. Valida (permanente proprio in campo, non tappato, ha effetti `attivata`), tappa il permanente (1 uso/turno), esegue gli effetti. (Costo mana di attivazione: non modellato in v1; aggiungibile.)

### ✅ FATTO 2026-06-13: Engine E3c.3 (effetti passivi / layer stat) — TDD
**102/102 test verdi** (E3c.3 +6). File: `Effetti.cs`, `Engine.cs`, `Azioni.cs`; test `EffettiPassivaTests.cs`.
- **Architettura statici:** gli effetti `passiva` NON mutano lo stato. Nuove funzioni **`Effetti.StatEffettive(stato, carta)`** e **`Effetti.KeywordEffettive(stato, carta)`** = base (DefCarta) + somma dei modificatori passivi attivi su tutto il campo (scan dei permanenti, targeting relativo al controllore della sorgente).
- **Verbi statici:** `ModificaStat(Bersaglio, Atk, Def)` (copre `modifica_stat` e `modifica_stat_combo`), `ConcediKeyword(Bersaglio, Keyword)`. Aggiunto campo `DefCarta.Keyword`.
- **Combat ora legge le stat EFFETTIVE** (`DichiaraBlocchiImpl` usa `StatEffettive`, non più `def.Atk/Def` grezze) → buff/debuff passivi influenzano il combattimento.
- **Nota:** debuff che porta DEF≤0 non causa morte automatica (mancano le state-based actions; a parte). `Filtro` del bersaglio ancora ignorato. Loader (`CarteDb`) NON mappa ancora `modifica_stat`/`concedi_keyword` → da aggiungere (E3c.4) ora che l'AST esiste.

**E3 — cosa resta (E3c.4, opzionale/incrementale):**
- **Mappare nel loader** `modifica_stat`/`modifica_stat_combo`/`concedi_keyword` → AST (ora supportato).
- **Verbi con scelta** (`quantificatore: una` → targeting input).
- **Danno/segnalini persistenti sulle creature** (per `infliggi_danno` a creatura, `segnalino_stat`, `applica_stat`) → richiede un modello di danno/contatori su `CartaIstanza`.
- **State-based actions** (creatura con DEF≤0 muore subito, anche da debuff).

> ⚠️ **Gotcha ambiente test:** questa macchina è lenta a buildare (~60s a freddo) e `vstest` va in timeout se ci sono `dotnet test` concorrenti. Lanciare **UN SOLO** `dotnet test` per volta; se serve, `VSTEST_CONNECTION_TIMEOUT=300`. Non parallelizzare le build.

### 🟡 IN CORSO 2026-06-10: Engine E4 (combattimento) — core fatto, TDD
**66/66 test verdi** (E1 28 + E2 27 + E4 11).
- ✅ `DichiaraAttacco(attaccanti)` — solo in fase Combat; valida (creatura propria in campo, non tappata, niente summoning sickness), tappa gli attaccanti, registra `StatoPartita.Combattimento`. Evento `CreaturaAttacca`. Test: `DichiaraAttaccoTests.cs` (5).
- ✅ `DichiaraBlocchi(assegnazioni)` — mappa attaccante→bloccante; risolve il combattimento (regola 7.3: ATK attaccante vs DEF bloccante → `>` muore bloccante, `=` entrambi, `<` muore attaccante; non bloccato → danno agli HP del difensore). Morti → cimitero. Eventi `CreaturaBlocca`/`CreaturaDistrutta`/`DannoGiocatore`. Chiude `Combattimento`. Test: `CombattimentoTests.cs` (6).
- **Scope/limiti E4:** solo 2 giocatori (difensore = altro giocatore). Mancano: targeting FFA (3-4p), keyword combat (Travolta/Velocità), sotto-fasi combat formali, morte-giocatore/respawn (→ E6). Modello danno = confronto singolo ATK vs DEF (NON scambio simultaneo MTG), come da regole.
- **Nota:** se si avanza fase senza risolvere i blocchi, `Combattimento` resta valorizzato (verrà gestito con stack/priorità in E5).
- Modello: `StatoPartita.Combattimento` (record `Combattimento(Attaccanti)`), eventi combat in `Eventi.cs`, azioni in `Azioni.cs`.

**Prossimo passo generale:** finire E2 (D+E) in C#, poi creare progetto Unity e collegare l'engine.

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
| E3 | Interprete effetti (esegue i verbi di carte.json) | ✅ core fatto: E3a · E3b (loader) · E3c.1 (upkeep/attacco/token) · E3c.2 (morte/attivata) · E3c.3 (passiva/layer stat). Resta E3c.4 incrementale (map buff nel loader, verbi con scelta, danno/segnalini su creatura, state-based) |
| E4 | Combattimento (attacco/blocco/danno/morti) | 🟡 core fatto (2p) |
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
- **2026-06-10: stack = UNITY (C#)**, non più React Native. Mira grafica Marvel Snap × Magic. Vedi banner in cima al file. Vecchio codice RN/TS = reference di design, congelato.
- **2026-06-10: OBIETTIVO PRESTAZIONI — deve girare bene anche su telefoni vecchi/economici.** Da tenere in mente nelle scelte grafiche: dosare VFX/particellari, texture non esagerate, evitare post-processing pesante. Il 3D in sé non è il problema (un card game disegna pochi oggetti), ma il "juice" va calibrato per non escludere device low-end. Target: fluido su smartphone di ~7-8 anni fa.
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
