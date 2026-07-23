# TASK — Lancio Set Mono Iniziale

> Mazzo: 60 carte totali (incluso Leader). Solo mono-fazione al primo lancio.

---

## Stato

- [x] 5 Leader mono (Xirlia/Shai/Kazet/Marika/Vaelos)
- [x] 41 Creature mono (8-9 per fazione)
- [x] Avamposti — tutte le tipologie mono coperte
- [ ] Magie mono
- [ ] Artefatti / Equipaggiamenti mono
- [ ] Santuari mono
- [ ] Satelliti mono
- [ ] Tragedie mono
- [ ] Benedizioni mono

---

## FASE 1 — Magie mono (in corso)

Target: 7 magie per fazione (mix Sorcery/Istante, mix rarità, mix Comune→Eterno).

- [x] **1.1 — Magie Nord** (7 carte: Raffica Gelida, Scudo di Brina, Tempesta di Brina, Riflesso del Permafrost, Editto Glaciale, Visione del Nord, Inverno Eterno)
- [x] **1.2 — Magie Sud** (7 carte: Lancia di Fiamme, Furia delle Braci, Incendio Doloso, Doppia Fiamma, Sigillo di Caleth, Carica Vulcanica, Cataclisma di Cendrath)
- [x] **1.3 — Magie Est** (7 carte: Sussurro di Endal, Tocco del Prosciugatore, Voce dei Sepolti, Ritorno dalle Ombre, Maledizione del Lich, Patto col Falcemortis, Veglia di Mordeth)
- [x] **1.4 — Magie Ovest** (7 carte: Sussurro delle Foglie, Germoglio di Faelorn, Eco del Vecchio Bosco, Visione di Lyren, Patto delle Radici, Convergenza Arcana, Risveglio di Faelorn)
- [x] **1.5 — Magie Centro** (7 carte: Mano di Vael, Benedizione di Vael, Eco di Vael, Riallineamento del Nexus, Specchio del Nexus, Editto del Centro, Apoteosi di Mid Vael)

## FASE 2 — Artefatti / Equipaggiamenti

- [x] **2.0 — Nomadi (incolori)** — 20 carte: 6 Artefatti classici + 8 Artefatti Creatura + 6 Equipaggiamenti
- [x] **2.1 — Nord** (5 carte: Cristallo Gelido, Stendardo del Baluardo, Sentinella Glaciale, Armatura Glaciale, Sigillo del Permafrost)
- [x] **2.2 — Sud** (5 carte: Carbone Ardente, Bandiera della Frenesia, Berserker di Bronzo, Lama Incandescente, Forgia di Caleth)
- [x] **2.3 — Est** (5 carte: Brandello di Sudario, Stendardo dei Lamenti, Lich-Costrutto, Cinto di Endal, Calice del Falcemortis)
- [x] **2.4 — Ovest** (5 carte: Pietra Rúnica, Stendardo del Bosco Antico, Custode Druida, Mantello del Druido, Anello Arcano del Bosco)
- [x] **2.5 — Centro** (5 carte: Frammento del Nexus, Stendardo dell'Assorbimento, Sentinella del Nexus, Mantello del Riflesso, Specchio Centrale)

## FASE 3 — Santuari + Satelliti

- [x] 3.1 — Nord (4 Santuari + 4 Satelliti)
- [x] 3.2 — Sud (4 Santuari + 4 Satelliti)
- [x] 3.3 — Est (4 Santuari + 4 Satelliti)
- [x] 3.4 — Ovest (4 Santuari + 4 Satelliti)
- [x] 3.5 — Centro (4 Santuari + 4 Satelliti)
- [x] 3.0 — Nomadi (8 Santuari + 8 Satelliti)
- **FASE 3 COMPLETA** — 56 carte totali (28 Santuari + 28 Satelliti)

## FASE 4 — Tragedie + Benedizioni

Struttura: **3 Tragedie + 3 Benedizioni per fazione**. Meccanica Eco introdotta per Tragedie (vedi REGOLE_BASE_TCG.md §5.6).

- [x] 4.0 — Nomadi (3 Tragedie + 3 Benedizioni)
- [ ] 4.1 — Nord
- [ ] 4.2 — Sud
- [ ] 4.3 — Est
- [ ] 4.4 — Ovest
- [ ] 4.5 — Centro

---

# MOTORE — Leader-avatar Start Mobile (formato senza terre)

> Update socio (2026-06-22): 5 Leader-avatar riscritti in `FORMATO_START_MOBILE/leader/` + `DESIGN_V1.md`, pushati su main. NON ancora pullati in locale (qui no git). Nuovo modello: Forza/Costituzione + Punti Vita per-Leader (Xirlia 35, Shai 25, Kazet 20, Marika 32, Vaelos 30) + Passiva + Hero Power. Via ATK/DEF + Evoluzione + Rientro.
>
> Vincolo socio: NON partire sui Leader finché non ci si sente. Combat creature attacco/blocco OK.

## Verdetto carico motore (analisi su engine-cs ~1657 righe core)

3 meccaniche su 4 leggere, 1 pesante. Gestibile se si isola il flip.

- [ ] **Punti Vita per-Leader (banale)** — già c'è `HpIniziali` in `ConfigPartita` + `Hp` su `Giocatore`. Vita asimmetrica = init diverso. BLOCCATO da domanda design sotto.
- [ ] **Assalto N (leggero)** — costo energia su dichiarazione attacco Leader + flag once/turn + no-attacco-T1 (no Velocità). Pattern già esistente: `EntrataQuestoTurno`/summoning sickness, `AttacchiQuestoTurno`. Check su declare → paga → set flag.
- [ ] **Costituzione-corazza solo in attacco (medio)** — vive nel resolver combat nuovo (attaccante/bloccante Magic). Ramo: se attaccante==Leader → Costituzione assorbe danno bloccante, eccesso → Punti Vita. In difesa Leader NON usa Forza/Costituzione. Dipende da combat creature fatto.
- [ ] **Flip = trigger di stato off-turn (PESANTE — qui sta il "rifare")** — motore ora event-driven turn-based, niente checker continuo. Flip "appena condizione vera, anche turno avversario" = serve loop stile state-based-action dopo ogni mutazione, entrambi i turni. Flip permanente/una-tantum/irreversibile, mantiene Punti Vita correnti, cambia solo Forza/Costituzione + abilità.

## Regole anti-rifacimento per il flip

- [ ] **Un solo punto di check** — agganciare `ControllaFlip(stato)` allo stream `Evento` (già emessi per tutto). Dopo ogni evento risolto → check. NON sparpagliare.
- [ ] **Condizioni flip ENUMERATE, non codice per-Leader** — set chiuso di predicati data-driven (`PuntiVita <= X`, `ControlliCreature >= N`, `AvversarioSubito >= N danno`...). Ogni Leader sceglie predicato+soglia sulla carta. Zero `if (leader=="Xirlia")` nel motore. Testabile, no rifacimenti quando si aggiungono Leader.

## DOMANDA BLOCCANTE (rispondere prima di codice)

- [ ] **Punti Vita del Leader = vita del giocatore (perdi a 0), o corpo separato che muore mentre continui?** Cambia modello vittoria/combat. Ipotesi: avatar = giocatore (Punti Vita = loss condition), coerente con "Start Mobile niente terre, Leader-avatar". Collegata: avversario col combat Magic attacca direttamente il Leader-faccia, fermato solo da blocchi/magie?

## Sequenza (d'accordo col socio: non partire sui Leader ora)

- [ ] 1. Merge combat creature attacco/blocco, suite verde (Assalto + Costituzione ci montano sopra).
- [ ] 2. Leader-combat layer (Punti Vita + Assalto + Costituzione-corazza).
- [ ] 3. Flip system = milestone a sé, dopo che 1+2 reggono.

## Domanda aperta in discussione

- [ ] Sviluppo emergent? (vedi conversazione 2026-06-22)
