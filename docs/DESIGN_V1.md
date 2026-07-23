# Design v1 — TCG dark-fantasy (versione commerciale snella)

> **Scopo del documento:** definire il gioco **v1 spedibile**, tagliato per mobile, retention e monetizzazione → obiettivo **exit**.
> Sostituisce operativamente `regole/REGOLE_BASE_TCG.md` (che resta come **riferimento del design completo "v2 avanzato"**).
> Data: 2026-06-11.

---

## 0. Decisioni di prodotto (perché questo doc esiste)

- **Obiettivo:** prodotto commerciale F2P mobile con metriche sane (D1/D7/D30 + ARPDAU) → **exit** (vendita a publisher/acquirer). Per un TCG senza IP l'exit si fa sulle **metriche**, non sul volume di contenuto. Quindi **tagliare è la strategia**, non un compromesso.
- **Mira estetica:** mix Marvel Snap × Magic (carte juicy, dark-fantasy). 3D leggero (URP), deve girare su telefoni di ~7-8 anni fa → VFX/texture dosati.
- **Filosofia di design:** poche leve, profonde (lezione Snap). Una sola meccanica-firma differenziante; tutto il resto tagliato al minimo per restare leggibile e veloce.
- **Meccanica-firma (la nostra anima):** la **corsa all'Obiettivo Segreto** — nessun competitor ce l'ha.

---

## 1. Forma del gioco v1 (sintesi)

```
Formato:        1v1 (niente FFA in v1)
Durata target:  5-7 minuti a partita
HP:             30
Energia:        automatica, +1 a turno (NIENTE terre/mana manuale)
Tipi di carta:  3 — Creatura · Magia · Leader
Mazzo:          30 carte + 1 Leader (Zona di Comando)
Mano iniziale:  5 · limite mano 7 · pesca 1/turno
Board:          max 6 slot creatura per giocatore
Obiettivo:      1 Obiettivo Segreto a testa, pescato da un pool curato
Vittoria:       HP avversario a 0  OPPURE  completa il tuo Obiettivo
Combat:         deterministico ATK vs DEF, niente stack/priorità
Monetizzazione: crediti (gratis giocando + rewarded ads, o comprabili) →
                pack + craft + cosmetici + battle pass. No cash-out, no scambio P2P.
```

Complessità totale ≈ Snap, ma con un gancio (l'Obiettivo) che Snap non ha.

---

## 2. Risorse — energia automatica

- **Niente Avamposti/terre, niente tap/untap per il mana.**
- Ogni giocatore ha **energia che sale di +1 all'inizio del proprio turno** (turno 1 = 1, …), fino a un **cap = 8** (deciso: i turni 9-10 a curva piena allungano troppo; la curva costi va disegnata su 1-8, le carte 7-8 sono i finisher).
- L'energia **si azzera e si ricarica** ogni turno (non si accumula).
- Le carte costano energia (= `ManaCosto.Totale`). Si gioca finché si ha energia.
- **Motivo:** rimuove mana-screw/flood, rimuove un intero tipo di carta, rimuove decisioni "amministrative". È la scelta deliberata che ha reso Snap veloce.
- *Identità colori:* i 5 colori restano come **identità di fazione** per deckbuilding/tematica, ma **non** governano il costo. **Deckbuilding (deciso):** mono-fazione del Leader + carte **Nomadi** (incolori, sempre legali). Multicolore = upsell v2.
- **Penalità mazzo vuoto (deciso): fatigue crescente** (1, 2, 3, … danno per ogni pesca a vuoto), non perdita immediata — così il `mill` resta pressione, non interruttore binario.

---

## 3. Struttura del turno (UNA fase azioni, stile Hearthstone)

**Deciso:** niente Main1/Combat/Main2 separate. Tre momenti:

1. **Inizio turno** (automatico): stappa + azzera summoning sickness, **ricarica energia (+1, cap 8)**, trigger `upkeep`, **pesca 1** (il primo non pesca al turno 1; mazzo vuoto → fatigue).
2. **Fase azioni** (libera): gioca carte, attiva abilità, **dichiara attacchi singoli**, in **qualsiasi ordine**.
3. **Fine turno** (automatico): scarta a 7, trigger di fine turno, passa.

**Motivo:** le due Main esistono in MTG per stack/istanti — entrambi tagliati. Senza istanti, l'ordine pre/post-combat è indifferente. Meno fasi = turni più corti = target 5-7 min realistico.

**Tagliato da v1:** stack & priorità, Istante, finestre di risposta. Combat ed effetti **deterministici**. (→ v2.)

---

## 4. Tipi di carta (3)

### 4.1 Creatura / Unità
- Permanente con **ATK** e **DEF**. Occupa **1 slot** (max 6).
- **Summoning sickness**: non attacca il turno in cui entra (salvo keyword *Velocità*).
- Attacca scegliendo il bersaglio (§6); **non blocca** (modello HS). Può avere *Provocazione* (forza l'avversario a colpirla per prima).
- **DEF = salute**, il danno si accumula e persiste (§6).
- Può avere effetti a trigger (vedi §7).

### 4.2 Magia / Spell
- Uso singolo: gioca → effetto → Cimitero.
- v1: giocabile solo nella **propria fase azioni** (no Istante → v2).

### 4.3 Leader / Eroe
- **1 per mazzo**, nella **Zona di Comando** (sempre visibile), non si pesca, disponibile da subito.
- Ha **ATK/DEF** + **1 Abilità Passiva** (attiva sempre) + **1 Hero Power** (attivabile 1 volta ogni X turni, costa energia).
- Si può **giocare in campo** pagando il costo: lì è una creatura normale (attacca/bersagliabile), occupa uno slot.
- **Morte del Leader:** torna in Zona di Comando (non al Cimitero). Per rigiocarlo: paga costo base **+ incremento cumulativo** per ogni morte (semplice; niente "attesa gratuita" in v1).
- **Tagliato da v1:** evoluzione del Leader (→ v2).

### 4.4 Tagliati da v1 (→ "modalità avanzata" v2)
Artefatto / Equipaggiamento / Artefatto-Creatura, Santuario, Tragedia (+ costo Eco), Alleato (fedeltà a livelli), Satellite, Benedizione, Avamposto, contatori-utilizzi. Restano nel design completo (`REGOLE_BASE_TCG.md`) come espansione futura.

---

## 5. Limite di board

- **Max 6 slot creatura per giocatore.** Migliora leggibilità su schermo verticale + performance low-end + forza scelte.
- **Regola "board pieno":** se è pieno, **non puoi giocare** un'altra creatura; i **token in eccesso** (`genera_token`) **fizzles** (non entrano).

---

## 6. Combattimento — modello attacco diretto (Hearthstone)

**DECISO** (no attacco/blocco, no reveal simultaneo): l'attaccante sceglie il bersaglio di ogni sua creatura; il difensore **non fa nulla** fuori dal proprio turno (zero input mid-turn → niente block-timer/sync/disconnessioni).

1. Nella propria fase azioni, dichiara `Attacca(attaccante, bersaglio)`. Attaccante = creatura propria non tappata, senza summoning sickness (salvo *Velocità*). Attaccare la **tappa**.
2. Bersaglio = **una creatura avversaria** oppure gli **HP del giocatore avversario**.
3. Risoluzione **immediata e deterministica**:
   - vs creatura: si infliggono danno a vicenda (ATK contro ATK). Il danno si **accumula** sulla creatura (vedi modello danno).
   - vs giocatore: ATK direttamente agli HP.
4. **Provocazione (Taunt):** se l'avversario controlla una creatura con Provocazione, devi attaccare quella prima di poter colpire altre creature o gli HP. È il vincolo di targeting che restituisce la profondità difensiva persa coi blocchi.
5. **Velocità:** ignora la summoning sickness. **Travolta:** il danno in eccesso oltre la "salute" della creatura uccisa passa agli HP del giocatore.

### Modello danno alla creatura (DECISO: persistente HS-style)
- La **DEF è la salute massima**. Il danno si **accumula** sulla creatura e **NON si resetta** a fine/inizio turno — resta finché non viene **curato** (effetti di cura, Hero Power difensivi).
- La creatura muore quando **danno accumulato ≥ DEF effettiva**.
- **Perché persistente e non reset-per-turno:** (1) coerenza col pool obiettivi (OB-08 "DEF non danneggiata" sarebbe degenere col reset); (2) bilanciamento del removal (v1 ha poche rimozioni → l'attrito da combat è necessario); (3) **un solo sistema danno** unificato con `infliggi_danno` da effetti; (4) abilita cura/heal come meccanica reale e il chip-damage come valuta. La leggibilità mobile è un problema risolto (HS mostra la salute corrente da 12 anni).

Niente stack/Istanti in risposta (v1).

---

## 7. Sistema effetti (engine)

Le carte portano **effetti** = `{trigger, azioni[]}`. Trigger supportati: `etb` (entra in campo), `upkeep`, `attacco`, `morte`, `attivata`. Verbi (azioni) v1: pesca, genera_mana→energia, infliggi_danno, distruggi, mill, genera_token (altri in arrivo). Targeting per proprietario (Tue/Avversario/Tutti) e quantificatore. (Dettaglio implementazione: `engine-cs/`, vedi `HANDOFF.md`.)

---

## 8. Obiettivi Segreti (la meccanica-firma)

### 8.1 Struttura
- **1 Obiettivo Segreto per giocatore**, **pescato a caso da un POOL globale curato** (~20-30 obiettivi). Niente mazzo-obiettivi costruito a mano in v1.
- L'obiettivo viene **assegnato dall'avversario** (in 1v1: l'avversario fa pescare il sistema) → mind-game.
- Il **contenuto è segreto**; vedi §8.3 per la visibilità del progresso.

### 8.2 Fairness (critico)
- Con un solo obiettivo a testa, la difficoltà deve essere **bilanciata**: in **ranked** tutti gli obiettivi del pool sono di **difficoltà/tempo ~equivalente**. Niente "facile vs difficile" casuale in competitivo.
- Modalità casual può avere varietà di difficoltà.

### 8.3 Segreto MA telegrafato (decide se è figo o "ingiusto")
- L'obiettivo è nascosto **nel contenuto**, ma il **progresso è pubblico in forma vaga**: barra/indicatore "lontano → vicino → quasi" oppure "2 condizioni su 3".
- Motivo: dà **counterplay** (l'avversario vede che stai per completarlo e può spingere sugli HP / interferire) → skill, non gotcha. Senza telegrafo, perdere a un win-con nascosto sembra ingiusto. **Questo è il punto #1 di game-feel dell'intera meccanica.**

### 8.4 Esempi di obiettivi (semplici, fair)
- "Infliggi danno da attacco allo stesso avversario per 3 turni consecutivi."
- "Controlla 4 creature contemporaneamente per la durata di un tuo turno."
- "Porta l'avversario a 10 HP o meno."
- "Abbi 6+ carte nel tuo Cimitero."

(Esempi del doc base più complessi/concatenati = casual o v2.)

### 8.5 Vittoria
Due vie, in parallelo, per tutta la partita:
- **HP avversario a 0**, oppure
- **Obiettivo completato**.
Il primo che soddisfa una delle due **vince**.
**Tagliato da v1:** sistema a 3 vite / respawn, formato "3 obiettivi facili", eliminazione FFA (→ v2).

---

## 9. Monetizzazione (pulita, acquisibile)

- **Valuta crediti:** guadagnabili **gratis** (vincendo/giocando partite + **rewarded video ads** → ricompense), oppure **acquistabili** (pacchetti di crediti reali → crediti).
- **Spesa crediti:** **pacchetti di carte** (RNG) + **craft/polvere** per fabbricare la carta singola desiderata + **art alternative / cosmetici** (board, skin Leader).
- **Battle pass** stagionale.
- **NO cash-out, NO marketplace di scambio carte P2P, NO valore monetario trasferibile** → niente rischio loot-box/gambling, niente percezione pay-to-win pura (il craft permette di *puntare* una carta con grind, non saltare la progressione col solo wallet).
- **Perché così:** modello provato (Hearthstone-like), legalmente sicuro, **acquisibile** (un acquirente non eredita responsabilità regolatorie).

---

## 10. Roadmap verso l'exit

1. **MVP giocabile** (questa forma v1) — 1v1 vs bot / hot-seat. Verifica il *feel* e la durata partita.
2. **Soft-launch ristretto** — misura **D1/D7/D30**. Se la retention è morta → si itera sul **core**, non si aggiungono feature.
3. **Monetizzazione + pass + ads** — misura **ARPDAU**.
4. **Metriche sane → pitch** a publisher/acquirer. L'exit si fa qui.

Principio guida costante: ogni feature candidata va pesata contro **"allunga la partita o il carico cognitivo?"**. Se sì, su mobile probabilmente costa più retention di quanta profondità dia.

---

## 11. Riuso del lavoro engine + re-baseline v1

Il codice C# (`engine-cs/`) regge in parte, ma il pivot a combat-HS + fase unica richiede un **re-baseline**:
- **Sopravvive:** E1 core loop · E3 interprete effetti completo (motore carte) · `StatEffettive`/`KeywordEffettive` · trigger morte/etb/upkeep/attivata · loader carte.
- **Da rifare (E4):** il combat attacco/blocco (`DichiaraAttacco`/`DichiaraBlocchi`) → **attacco diretto** `Attacca(attaccante, bersaglio)` + Provocazione/Velocità/Travolta. Sopravvivono morte/eventi.
- **Da cambiare (E2):** mana colorato/Avamposti → **energia automatica** (intero, +1/turno, cap 8, reset); costo = `ManaCosto.Totale`.
- **Da NON fare:** E5 stack/priorità (tagliato).
- **Nuovo:** modello **danno persistente su `CartaIstanza`** + state-based death · **fase unica** (collassa Untap..End) · **fatigue** · **cap board 6** + board-pieno · **sistema Obiettivi** (pool, assegnazione, tracking, telegrafo 3-stati, win-check parallelo) · **Leader**.

Checklist engine completa: vedi `FEEDBACK_DESIGN_V1_E_OBIETTIVI.md` §6 (analisi Fable 5, 2026-06-12).

---

## 12. Decisioni — CHIUSE (2026-06-13, post-review Fable 5)
- **Combat:** ✅ attacco diretto stile Hearthstone + Provocazione (§6). No attacco/blocco, no reveal simultaneo.
- **Danno creatura:** ✅ persistente HS-style, nessun reset (§6).
- **Struttura turno:** ✅ una sola fase azioni (§3).
- **Vincoli colore:** ✅ mono-fazione del Leader + Nomadi (§2).
- **Cap energia:** ✅ 8 (§2). **Mazzo vuoto:** ✅ fatigue crescente (§2).
- **Leader:** ✅ passiva debole, potenza nell'Hero Power.
- **Pool obiettivi:** ✅ 22 ranked + 6 casual, vedi `FEEDBACK_DESIGN_V1_E_OBIETTIVI.md` §3-4. Turni target da validare in playtest (telemetria §5 di quel doc).
- **Compliance:** pubblicare le probabilità pack in-app (Apple/Google).
- **Soft-launch:** mercati neutri (no audience Karmate); canale Karmate = moltiplicatore al lancio globale.
