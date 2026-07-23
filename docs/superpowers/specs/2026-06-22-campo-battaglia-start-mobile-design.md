# Campo di Battaglia — Start Mobile (evoluzione)

*Spec di design. Evoluzione di `UI/UI_CAMPO_BATTAGLIA.md` per il formato senza terre Start Mobile (Leader-avatar). Da realizzare su app.emergent.sh, poi collegare al motore `engine-cs`.*

Data: 2026-06-22

---

## 1. Scopo e contesto

Adattare il campo di battaglia esistente al nuovo modello Leader-avatar del formato Start Mobile. Il mockup attuale (`UI/UI_CAMPO_BATTAGLIA.md` + `UI/campo_battaglia_mockup.html`) è costruito sul modello vecchio (Leader con Rientro, vite respawn, mana generico, combat Hearthstone-diretto). Questo spec definisce **solo i delta** rispetto a quel documento: tutto ciò che non è elencato qui resta invariato.

Target invariato: **390×844px portrait** (iPhone standard), React Native / Expo (`app/`), estetica dark-fantasy cosmico (palette, font Cinzel, stelle, nebbia, divisore centrale).

Decisioni di design alla base (prese in brainstorming 2026-06-22):

- Punto di partenza: **evolvere** il mockup esistente, non ridisegnare.
- Combat: **Magic attacco/blocco** (difensore assegna bloccanti). *Contraddice il `docs/DESIGN_V1.md` locale che sceglie HS-diretto; il locale non è aggiornato col push del socio.*
- Leader-avatar: **slot dedicato** in campo + hex faccia per i Punti Vita.
- Punti Vita = **vita del giocatore**, nessun respawn (niente Rientro).
- Energia tipizzata: **tutta del colore della fazione del Leader** (mono-fazione = un colore).

## 2. Invariato (riferimento `UI_CAMPO_BATTAGLIA.md`)

Restano identici e NON vanno reimplementati:

- Palette dark-fantasy cosmico, oro `#c9a84c`, viola cosmico, colori fazione (Nord `#5588cc`, Sud `#cc4422`, Est `#44aa66`, Ovest `#8844aa`, Centro `#aaaaaa`).
- Tipografia: Cinzel (UI) + Crimson Text (lore).
- Viewport 390×844, posizioni assolute scalate su 390 di base.
- Sfondo: gradiente, 90 stelle `twinkle`, nebbia centrale.
- Divisore centrale con gemma + numero turno.
- Indicatore turno + fase (top center).
- Zone creature (avversario sopra divisore, giocatore sotto), slot creatura 54×76px, max 6 slot.
- Fan mano giocatore (bottom, z-index 30), mano avversario coperta (top).
- Pannelli laterali per Mazzo, Cimitero, Satellite, Tragedia, Benedizione, Obiettivo (mini-carte 42×58px).
- Bottone Fine Turno (lato destro, verticale).
- Badge formato obiettivo `◆ 0/N` (segreto, solo progresso).
- Z-index layers e animazioni hover/twinkle esistenti.

## 3. Delta di design

### A. Leader-avatar → slot dedicato

**Cambia:** il Leader non è più una mini-carta nel pannello laterale (Zona di Comando). Diventa un **combattente in campo** in uno slot dedicato.

- **Posizione:** lato sinistro di ciascuna metà campo, all'altezza del divisore. Layout: `[SLOT LEADER] | [riga creature]` per entrambi i giocatori (avversario sopra, giocatore sotto).
- **Dimensione:** più grande di una creatura (es. ~64×88px) per leggerlo come avatar-combattente, distinto dalle creature.
- **Contenuto dello slot:**
  - Art del Leader.
  - **Forza / Costituzione** in basso (Forza rosso, Costituzione blu) — stesso schema cromatico delle creature.
  - Badge **Assalto N** (costo energia per dichiararlo attaccante).
  - Badge **progresso flip** (vedi §E).
  - Stato **tappato** quando ha attaccato (`rotate(13deg)`, opacity ridotta, come le creature).
- **Legame con l'hex faccia:** slot Leader e hex faccia sono la stessa entità. Hex = vita (Punti Vita), slot = combattente (Forza/Cost/Assalto/flip). Visivamente legati da glow dello stesso colore fazione.

**Rimosso:** dal pannello laterale spariscono la mini-carta Leader con corona ♛, il badge †morti e il badge ⬡costo-rientro (`UI_CAMPO_BATTAGLIA.md` §6.5, sezione Leader).

### B. Punti Vita (hex faccia)

**Cambia:** l'hex avatar non mostra più HP generico + vite respawn, ma i **Punti Vita del Leader**.

- Hex faccia mostra il **numero PV corrente**. Valore iniziale per-Leader: Xirlia 35, Shai 25, Kazet 20, Marika 32, Vaelos 30.
- Barra opzionale verde→rosso proporzionale ai PV.
- **PV 0 = sconfitta**, partita finita per quel giocatore.

**Rimosso:** indicatore vite respawn ♥♥♥ (`UI_CAMPO_BATTAGLIA.md` §6.5 / §9), badge †morti, badge ⬡rientro, badge ⏳attesa. Nessun respawn nel modello Start Mobile.

### C. Energia tipizzata

**Cambia:** le sfere mana viola generiche diventano **pip colorati per fazione**.

- Colore dei pip = fazione del Leader (Nord blu / Sud rosso / Est verde / Ovest viola / Centro grigio). Mono-fazione al lancio = un solo colore.
- Pip pieno = energia disponibile; pip spento/scuro = già spesa nel turno.
- L'ammontare cresce automaticamente +1 a turno fino al cap (come l'`EnergiaMax` attuale del motore). Ricarica a inizio turno.
- Posizione: stessa fascia del vecchio display mana, accanto all'hex faccia.

### D. Combat Magic attacco/blocco

**Cambia:** dal modello attacco-diretto a **dichiarazione attaccanti + assegnazione bloccanti**.

- **Attacco (turno attivo):** tap su una propria creatura non tappata e senza summoning sickness → scegli bersaglio. Dichiarare l'attacco la **tappa**. Il vincolo Provocazione (Taunt) resta: se l'avversario controlla una creatura con Provocazione, va attaccata prima.
- **Blocco (turno avversario) — interazione nuova:** quando l'avversario dichiara attaccanti, compare un **overlay difensivo**. Il difensore trascina le proprie creature non tappate sopra gli attaccanti in arrivo per assegnarle come bloccanti, poi **conferma**. Una creatura bloccante intercetta il danno dell'attaccante.
  - **Il Leader NON blocca:** non è assegnabile come bloccante. Lo si protegge solo con creature che bloccano o con magie che evitano il danno.
  - Implica **input nel turno avversario** (fase difensiva) — vedi §6 rischio.
- **Leader come attaccante:** per dichiararlo attaccante si paga **Assalto N** energia (valore stampato sul Leader), **una volta per turno**. Il badge Assalto mostra il costo; è **disabilitato** se: già usato questo turno, oppure è il primo turno (il Leader non ha Velocità, non attacca a T1).
  - **Costituzione-corazza solo in attacco:** quando il Leader attacca, la Costituzione assorbe il danno del bloccante; l'eccesso passa ai suoi **Punti Vita**. In difesa il Leader non usa Forza/Costituzione.

### E. Flip del Leader

**Nuovo elemento.**

- Lo slot Leader mostra un **badge di progresso della condizione di flip** stampata (es. "PV ≤ 15", "3 creature", "avversario ha subito ≥ N"). Il badge riflette quanto manca alla condizione.
- Al soddisfacimento della condizione (anche durante il turno avversario): **animazione di flip** — flash oro + swap dell'art — e lo slot passa alla **forma potenziata** (nuove Forza/Costituzione + marker della nuova abilità).
- Il flip è **permanente e una-tantum**; **mantiene i Punti Vita correnti** (cambiano solo Forza/Costituzione + abilità).

### F. Carte creatura — naming stat

**Cambia:** le statistiche creatura passano da ATK/DEF a **Forza (rosso) / Costituzione (blu)**, per coerenza col combat e col Leader. Resta lo schema cromatico (Forza rosso, Costituzione blu) del mockup. Solo rinomina di label/semantica, layout invariato.

## 4. Mappa stati UI → motore (per il collegamento futuro)

Il collegamento al motore `engine-cs` è una fase successiva; qui solo la corrispondenza di riferimento.

| Elemento UI | Stato motore (`Stato.cs`) | Note |
|---|---|---|
| PV hex faccia | `Giocatore.Hp` (o nuovo campo PV Leader) | valore iniziale per-Leader via `ConfigPartita.HpIniziali` |
| Pip energia tipizzata | `Giocatore.Energia` / `EnergiaMax` | tipizzazione = colore fazione, nuova info |
| Slot Leader (Forza/Cost) | `StatoLeader` (nuovi campi: Forza, Costituzione, PV, formaFlip) | il record attuale ha ATK/DEF impliciti + Morti/Rientro: da rifare |
| Badge Assalto | nuovo: costo Assalto + flag "usato questo turno" | riusa pattern `AttacchiQuestoTurno` |
| Progresso/animazione flip | nuovo: trigger di stato (condizione enumerata) | check su stream `Evento`, anche fuori turno |
| Overlay blocco | nuovo: fase difensiva attacco/blocco | il `DichiaraBlocchi` era stato rimosso nel DESIGN_V1 locale |
| Slot creatura Forza/Cost | `CartaIstanza` (BonusAtk/BonusDef → Forza/Cost) | rinomina |

## 5. Componenti (unità isolabili)

1. **HexFaccia** — mostra PV correnti + barra; input: `pv`, `pvMax`, `fazione`. Nessuna logica di gioco.
2. **SlotLeader** — art + Forza/Cost + badge Assalto + badge flip + stato tappato; input: stato Leader; emette intent "dichiara Leader attaccante".
3. **BarraEnergia** — pip colorati fazione; input: `energia`, `energiaMax`, `coloreFazione`.
4. **ZonaCreature** — riga slot 54×76, max 6; input: lista creature, lato (giocatore/avversario).
5. **CartaCreatura** — art + Forza/Cost + dot fazione + keyword badge + stato tappato.
6. **OverlayBlocco** — modale difensivo: lista attaccanti in arrivo + drag-target bloccanti + conferma; input: attaccanti, creature difensive disponibili; emette assegnazione bloccanti.
7. **PannelloLaterale** — mini-carte Mazzo/Cimitero/Satellite/Tragedia/Benedizione/Obiettivo (invariato, meno il Leader).
8. **FanMano** — invariato.
9. **IndicatoreTurnoFase** — invariato, ma le fasi includono la fase difensiva (blocco).

## 6. Rischi e questioni aperte

- **Input nel turno avversario (blocco):** il modello Magic attacco/blocco introduce interazione difensiva mid-turn (overlay blocco). Il `docs/DESIGN_V1.md` locale aveva scelto HS-diretto **apposta** per evitare block-timer / sync / disconnessioni. Questa scelta va confermata col socio e impatta sia l'app sia il motore (riemerge `DichiaraBlocchi`). **Da chiarire col socio.**
- **Flip off-turn:** il progresso/animazione flip può scattare nel turno avversario → richiede check di stato continuo lato motore (vedi `tasks/todo.md`, sezione motore Leader). Lato UI serve poter animare il flip in qualsiasi momento, non solo nel proprio turno.
- **Naming Forza/Costituzione sulle creature:** assunto per coerenza; confermare che anche le creature (non solo il Leader) usino Forza/Costituzione.
- **Energia multi-fazione:** lo spec copre solo mono-fazione (lancio). Leader bi/tri/quad/penta richiederanno un modello di energia multi-colore (display a pip misti) — fuori scope qui.

## 7. Fuori scope

- Collegamento effettivo al motore (`engine-cs`) — fase successiva.
- Modello energia multi-fazione.
- Implementazione del flip lato motore (vedi `tasks/todo.md`).
- Animazioni oltre flip/twinkle/hover già definite.
