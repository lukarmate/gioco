# Prompt per app.emergent.sh — Campo di Battaglia (Start Mobile)

> Incolla il blocco sotto nel tab **Mobile App** di app.emergent.sh. È self-contained: descrive una sola schermata (il campo di battaglia) di un gioco di carte dark-fantasy. Basato su `2026-06-22-campo-battaglia-start-mobile-design.md`.

---

Costruisci la schermata "Campo di Battaglia" di un gioco di carte collezionabili dark-fantasy mobile (1v1), in portrait, ottimizzata per iPhone (390×844). Solo questa schermata, con dati di esempio statici (mock). Niente backend, niente login.

ESTETICA
- Stile dark-fantasy cosmico: sfondo gradiente molto scuro (#050508 in alto → #0c0618 in basso), stelle bianche che luccicano lentamente, nebbia viola al centro.
- Colore accento oro #c9a84c per bordi, testo, glow. Font serif maiuscolo stile "Cinzel".
- Ogni elemento con bordo sottile + leggero glow.

LAYOUT (dall'alto in basso)
1. In alto al centro: indicatore del turno (numero in un riquadro) + etichetta della fase corrente ("Fase Azioni" o "⚔ Fase Blocco").
2. META CAMPO AVVERSARIO (sopra il centro): a sinistra lo "Slot Leader" avversario, al centro la riga delle sue creature (max 6), a destra: avatar esagonale con i Punti Vita, barra energia, mini-carte Mazzo e Cimitero (con numero).
3. Linea divisoria centrale dorata con una gemma e il numero turno.
4. META CAMPO GIOCATORE (sotto il centro): speculare — a sinistra lo Slot Leader del giocatore, al centro le sue creature, a destra avatar+PV, barra energia, Mazzo/Cimitero.
5. In basso: la mano del giocatore come ventaglio di carte (5 carte sovrapposte, leggermente ruotate).

ELEMENTI CHIAVE (modello di gioco "Start Mobile", senza terre)
- SLOT LEADER: una carta-avatar più grande di una creatura. Mostra: arte, due statistiche in basso = Forza (numero rosso) e Costituzione (numero blu), un badge "Assalto N" (es. "Assalto 3"), e un piccolo badge che mostra la condizione di trasformazione (es. "PV ≤ 15" o "3 creature"). Quando il Leader ha già attaccato appare ruotato (tappato). Il badge "Assalto" è disabilitato (semitrasparente) se è il turno 1 o se l'energia non basta.
- AVATAR ESAGONALE (faccia): mostra solo il numero dei Punti Vita correnti + una barra (verde se alti, rossa se bassi). NON mostrare cuori di respawn, NON mostrare badge di morte o costo di rientro. A 0 Punti Vita il giocatore perde.
- BARRA ENERGIA: una fila di "pip" circolari colorati. Il colore dipende dalla fazione del Leader. I pip pieni = energia disponibile, i pip spenti = energia usata.
- CARTE CREATURA (max 6 per lato): arte + Forza (rosso) e Costituzione (blu) in basso + un pallino del colore della fazione. Se tappata, ruotata e più trasparente.
- COLORI FAZIONE: Nord = blu #5588cc, Sud = rosso #cc4422, Est = verde #44aa66, Ovest = viola #8844aa, Centro = grigio #aaaaaa. Per il primo lancio il mazzo è mono-fazione (un solo colore).

INTERAZIONE COMBAT (stile Magic: attacco + blocco)
- Nella propria fase, toccando una propria creatura non tappata si può dichiararla attaccante e scegliere un bersaglio (creatura avversaria, Leader avversario, o l'avatar). Attaccare la tappa.
- Quando l'avversario attacca, compare un OVERLAY DIFENSIVO a schermo intero: mostra le creature attaccanti in arrivo e, sotto, le proprie creature non tappate; l'utente tocca un attaccante e poi una propria creatura per assegnarla come bloccante, poi tocca "Conferma". IL LEADER NON PUÒ BLOCCARE (non compare tra i bloccanti disponibili).
- Il Leader per attaccare paga "Assalto N" energia, una sola volta per turno, mai al turno 1.

DATI DI ESEMPIO (mock)
- Giocatore (in basso): Leader "Xirlia" fazione Nord, PV 35/35, Forza 4, Costituzione 3, Assalto 3, condizione flip "PV ≤ 15". Energia 3/3. Creature: "Guerriero" (Nord, Forza 2, Cost 3), "Mago" (Ovest, Forza 1, Cost 2). Mano: "Scudo", "Druido", "Ombra". Mazzo 24, Cimitero 1.
- Avversario (in alto): Leader "Kazet" fazione Sud, PV 20/20, Forza 5, Costituzione 2, Assalto 2, condizione flip "3 creature". Energia 2/2. Creatura: "Drago" (Sud, Forza 3, Cost 4). Mazzo 22, Cimitero 0. Mano: 3 carte coperte.
- Turno 2, fase "Azioni".

Usa React Native / Expo. Genera dei componenti separati e riutilizzabili: avatar/PV, barra energia, slot Leader, carta creatura, riga creature, overlay blocco, ventaglio mano, indicatore turno, e il contenitore "Campo di Battaglia" che li compone.
