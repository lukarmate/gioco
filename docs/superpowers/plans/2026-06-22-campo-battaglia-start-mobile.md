# Campo di Battaglia Start Mobile — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolvere il campo di battaglia Expo/React Native (`app/`) al modello Leader-avatar Start Mobile: slot Leader in campo, Punti Vita senza respawn, energia tipizzata, combat Magic attacco/blocco, flip.

**Architecture:** Decomporre l'attuale `App.js` monolitico in `src/` con logica di gioco pura (testabile a unità) separata dai componenti di presentazione. La logica (Assalto, flip, blocco, danno Leader, sconfitta) vive in moduli puri senza React; i componenti consumano stato + helper. Composizione finale in `CampoBattaglia` montato da `App.js`.

**Tech Stack:** Expo ~52, React Native 0.76.9, React 18.3.1, `expo-linear-gradient`. Test: `jest-expo` + `react-test-renderer` (bundled, niente librerie extra di rendering).

## Global Constraints

- Viewport base: **390×844px portrait**; posizioni assolute scalate su 390 di larghezza.
- Palette/estetica invariata da `UI/UI_CAMPO_BATTAGLIA.md`: oro `#c9a84c`, viola cosmico, font Cinzel. Colori fazione: Nord `#5588cc`, Sud `#cc4422`, Est `#44aa66`, Ovest `#8844aa`, Centro `#aaaaaa`.
- Punti Vita iniziali per-Leader: Xirlia 35, Shai 25, Kazet 20, Marika 32, Vaelos 30.
- **Nessun respawn**: PV ≤ 0 = sconfitta. Niente badge †morti/⬡rientro/♥♥♥.
- Energia: **tutta del colore della fazione del Leader** (mono-fazione = un colore), cresce +1/turno fino al cap.
- Stat creatura e Leader: **Forza** (rosso) / **Costituzione** (blu).
- Leader **non blocca**; per attaccare paga **Assalto N**, una volta per turno, mai al turno 1 (no Velocità).
- React 18.3.1 → usare `react-test-renderer@18.3.1` (versione allineata a React).
- Logica di gioco in moduli puri **senza import React**; i componenti non contengono regole.

Spec di riferimento: `docs/superpowers/specs/2026-06-22-campo-battaglia-start-mobile-design.md`.

---

## File Structure

```
app/
  App.js                         # MODIFY — monta <CampoBattaglia state={mockState} />
  package.json                   # MODIFY — devDeps test + script "test"
  jest.config.js                 # CREATE — preset jest-expo
  src/
    theme.js                     # CREATE — COLORS, FAZIONI, DIMS
    state/
      mockState.js               # CREATE — stato di esempio (2 giocatori, leader, energia, creature, mano, fase)
      combat.js                  # CREATE — logica pura: Assalto, spesa energia, danno Leader attaccante, sconfitta, blocco
      flip.js                    # CREATE — logica pura: valuta condizione flip enumerata, applica flip
    components/
      HexFaccia.js               # CREATE — PV + barra
      BarraEnergia.js            # CREATE — pip colorati fazione
      CartaCreatura.js           # CREATE — art + Forza/Costituzione + stato tappato
      ZonaCreature.js            # CREATE — riga slot (max 6)
      SlotLeader.js              # CREATE — Forza/Cost + badge Assalto + progresso/forma flip
      OverlayBlocco.js           # CREATE — assegnazione bloccanti (fase difensiva)
      PannelloLaterale.js        # CREATE — mini-carte Mazzo/Cimitero/Satellite/Tragedia/Benedizione/Obiettivo (senza Leader)
      FanMano.js                 # CREATE — fan carte in mano
      IndicatoreTurnoFase.js     # CREATE — numero turno + fase corrente
      CampoBattaglia.js          # CREATE — composizione di tutto
```

Test co-locati: `<file>.test.js` accanto al modulo.

---

### Task 1: Setup tooling + tema

**Files:**
- Modify: `app/package.json`
- Create: `app/jest.config.js`
- Create: `app/src/theme.js`
- Test: `app/src/theme.test.js`

**Interfaces:**
- Consumes: niente.
- Produces: `theme.js` esporta `COLORS` (oggetto), `FAZIONI` (mappa nome→colore: `nord|sud|est|ovest|centro`), `DIMS` (`{ W: 390, H: 844 }`).

- [ ] **Step 1: Inizializza git nella cartella app (non è un repo)**

```bash
cd "app" && git init && printf "node_modules/\n.expo/\n" > .gitignore
```

- [ ] **Step 2: Aggiungi devDeps e script test a package.json**

Modifica `app/package.json`: aggiungi a `scripts` la riga `"test": "jest"`, e in `devDependencies`:

```json
{
  "scripts": {
    "start": "expo start",
    "ios": "expo start --ios",
    "android": "expo start --android",
    "test": "jest"
  },
  "devDependencies": {
    "@babel/core": "^7.25.2",
    "jest": "^29.7.0",
    "jest-expo": "~52.0.0",
    "react-test-renderer": "18.3.1"
  }
}
```

Poi installa:

```bash
cd "app" && npm install
```

- [ ] **Step 3: Crea jest.config.js**

`app/jest.config.js`:

```javascript
module.exports = {
  preset: 'jest-expo',
  transformIgnorePatterns: [
    'node_modules/(?!((jest-)?react-native|@react-native(-community)?|expo(nent)?|@expo(nent)?/.*|@expo-google-fonts/.*|react-navigation|@react-navigation/.*|@unimodules/.*|unimodules|sentry-expo|native-base|react-native-svg))',
  ],
};
```

- [ ] **Step 4: Scrivi il test del tema (fallisce)**

`app/src/theme.test.js`:

```javascript
import { COLORS, FAZIONI, DIMS } from './theme';

test('FAZIONI ha i 5 colori fazione', () => {
  expect(FAZIONI.nord).toBe('#5588cc');
  expect(FAZIONI.sud).toBe('#cc4422');
  expect(FAZIONI.est).toBe('#44aa66');
  expect(FAZIONI.ovest).toBe('#8844aa');
  expect(FAZIONI.centro).toBe('#aaaaaa');
});

test('DIMS è 390x844', () => {
  expect(DIMS).toEqual({ W: 390, H: 844 });
});

test('COLORS ha oro', () => {
  expect(COLORS.gold).toBe('#c9a84c');
});
```

- [ ] **Step 5: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/theme.test.js`
Expected: FAIL — `Cannot find module './theme'`.

- [ ] **Step 6: Crea theme.js**

`app/src/theme.js`:

```javascript
export const COLORS = {
  gold: '#c9a84c',
  cosmic: 'rgba(100,60,180,0.6)',
  bgTop: '#050508',
  bgBottom: '#0c0618',
  forza: '#c0392b',      // rosso
  costituzione: '#5588cc', // blu
  pvFull: '#27ae60',
  pvLow: '#c0392b',
};

export const FAZIONI = {
  nord: '#5588cc',
  sud: '#cc4422',
  est: '#44aa66',
  ovest: '#8844aa',
  centro: '#aaaaaa',
};

export const DIMS = { W: 390, H: 844 };
```

- [ ] **Step 7: Esegui — deve passare**

Run: `cd "app" && npm test -- src/theme.test.js`
Expected: PASS (3 test).

- [ ] **Step 8: Commit**

```bash
cd "app" && git add -A && git commit -m "chore: setup jest-expo + theme"
```

---

### Task 2: Stato di gioco mock

**Files:**
- Create: `app/src/state/mockState.js`
- Test: `app/src/state/mockState.test.js`

**Interfaces:**
- Consumes: `FAZIONI` da `theme.js`.
- Produces: `creaMockState()` → oggetto:
  ```
  {
    turno: number,
    fase: 'azioni' | 'blocco',
    giocatori: {
      gk:  Giocatore,   // giocatore locale (bottom)
      av:  Giocatore,   // avversario (top)
    }
  }
  Giocatore = {
    fazione: 'nord'|'sud'|'est'|'ovest'|'centro',
    energia: number, energiaMax: number,
    leader: Leader,
    creature: Creatura[],   // max 6
    mano: Carta[],
    mazzo: number, cimitero: number,
  }
  Leader = {
    nome, fazione, pv, pvMax, forza, costituzione,
    costoAssalto: number, assaltoUsatoQuestoTurno: boolean, tappato: boolean,
    flip: { tipo: 'pv_max'|'controlli_creature'|'avversario_subito', soglia: number },
    flipped: boolean,
    formaFlip: { forza, costituzione },
    dannoAvversarioSubito: number,
  }
  Creatura = { id, nome, fazione, forza, costituzione, tappata: boolean }
  Carta = { id, nome, fazione, costo, tipo: 'unita'|'magia' }
  ```

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/state/mockState.test.js`:

```javascript
import { creaMockState } from './mockState';

test('mock ha due giocatori con leader e PV iniziali', () => {
  const s = creaMockState();
  expect(s.giocatori.gk.leader.pv).toBeGreaterThan(0);
  expect(s.giocatori.gk.leader.pv).toBe(s.giocatori.gk.leader.pvMax);
  expect(s.giocatori.av.leader.pv).toBeGreaterThan(0);
});

test('mock ha al massimo 6 creature per giocatore', () => {
  const s = creaMockState();
  expect(s.giocatori.gk.creature.length).toBeLessThanOrEqual(6);
  expect(s.giocatori.av.creature.length).toBeLessThanOrEqual(6);
});

test('leader ha condizione flip enumerata valida', () => {
  const s = creaMockState();
  expect(['pv_max', 'controlli_creature', 'avversario_subito'])
    .toContain(s.giocatori.gk.leader.flip.tipo);
});

test('fase iniziale è azioni', () => {
  expect(creaMockState().fase).toBe('azioni');
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/state/mockState.test.js`
Expected: FAIL — `Cannot find module './mockState'`.

- [ ] **Step 3: Crea mockState.js**

`app/src/state/mockState.js`:

```javascript
function leaderXirlia() {
  return {
    nome: 'Xirlia', fazione: 'nord', pv: 35, pvMax: 35,
    forza: 4, costituzione: 3,
    costoAssalto: 3, assaltoUsatoQuestoTurno: false, tappato: false,
    flip: { tipo: 'pv_max', soglia: 15 }, flipped: false,
    formaFlip: { forza: 6, costituzione: 5 },
    dannoAvversarioSubito: 0,
  };
}

function leaderKazet() {
  return {
    nome: 'Kazet', fazione: 'sud', pv: 20, pvMax: 20,
    forza: 5, costituzione: 2,
    costoAssalto: 2, assaltoUsatoQuestoTurno: false, tappato: false,
    flip: { tipo: 'controlli_creature', soglia: 3 }, flipped: false,
    formaFlip: { forza: 7, costituzione: 3 },
    dannoAvversarioSubito: 0,
  };
}

export function creaMockState() {
  return {
    turno: 1,
    fase: 'azioni',
    giocatori: {
      gk: {
        fazione: 'nord', energia: 3, energiaMax: 3,
        leader: leaderXirlia(),
        creature: [
          { id: 'c1', nome: 'Guerriero', fazione: 'nord', forza: 2, costituzione: 3, tappata: false },
          { id: 'c2', nome: 'Mago', fazione: 'ovest', forza: 1, costituzione: 2, tappata: false },
        ],
        mano: [
          { id: 'm1', nome: 'Scudo', fazione: 'nord', costo: 1, tipo: 'magia' },
          { id: 'm2', nome: 'Druido', fazione: 'est', costo: 3, tipo: 'unita' },
        ],
        mazzo: 24, cimitero: 1,
      },
      av: {
        fazione: 'sud', energia: 2, energiaMax: 2,
        leader: leaderKazet(),
        creature: [
          { id: 'e1', nome: 'Drago', fazione: 'sud', forza: 3, costituzione: 4, tappata: false },
        ],
        mano: [{ id: 'h1' }, { id: 'h2' }, { id: 'h3' }],
        mazzo: 22, cimitero: 0,
      },
    },
  };
}
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/state/mockState.test.js`
Expected: PASS (4 test).

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: stato mock campo Start Mobile"
```

---

### Task 3: Logica combat (Assalto, energia, danno Leader, sconfitta, blocco)

**Files:**
- Create: `app/src/state/combat.js`
- Test: `app/src/state/combat.test.js`

**Interfaces:**
- Consumes: niente (funzioni pure).
- Produces:
  - `puoiAssalto({ turno, energia, costoAssalto, assaltoUsatoQuestoTurno })` → boolean
  - `spesaEnergia(energia, costo)` → number (mai negativo)
  - `dannoLeaderAttaccante(leader, dannoBloccante)` → `{ ...leader, pv }` (Costituzione assorbe, eccesso ai PV)
  - `isSconfitta(pv)` → boolean (`pv <= 0`)
  - `assegnaBloccante(assegnazioni, attaccanteId, bloccanteId)` → nuova mappa `{ [attaccanteId]: bloccanteId }` (immutabile)

- [ ] **Step 1: Scrivi i test (falliscono)**

`app/src/state/combat.test.js`:

```javascript
import { puoiAssalto, spesaEnergia, dannoLeaderAttaccante, isSconfitta, assegnaBloccante } from './combat';

describe('puoiAssalto', () => {
  const base = { turno: 2, energia: 3, costoAssalto: 3, assaltoUsatoQuestoTurno: false };
  test('ok se energia basta, turno > 1, non usato', () => {
    expect(puoiAssalto(base)).toBe(true);
  });
  test('no al turno 1 (no Velocità)', () => {
    expect(puoiAssalto({ ...base, turno: 1 })).toBe(false);
  });
  test('no se energia insufficiente', () => {
    expect(puoiAssalto({ ...base, energia: 2 })).toBe(false);
  });
  test('no se già usato questo turno', () => {
    expect(puoiAssalto({ ...base, assaltoUsatoQuestoTurno: true })).toBe(false);
  });
});

describe('spesaEnergia', () => {
  test('sottrae', () => { expect(spesaEnergia(3, 2)).toBe(1); });
  test('non va sotto zero', () => { expect(spesaEnergia(1, 3)).toBe(0); });
});

describe('dannoLeaderAttaccante', () => {
  const leader = { pv: 30, costituzione: 3 };
  test('Costituzione assorbe tutto: PV invariati', () => {
    expect(dannoLeaderAttaccante(leader, 3).pv).toBe(30);
    expect(dannoLeaderAttaccante(leader, 2).pv).toBe(30);
  });
  test('eccesso oltre Costituzione va ai PV', () => {
    expect(dannoLeaderAttaccante(leader, 5).pv).toBe(28); // 5-3=2
  });
});

describe('isSconfitta', () => {
  test('PV 0 o meno = sconfitta', () => {
    expect(isSconfitta(0)).toBe(true);
    expect(isSconfitta(-2)).toBe(true);
    expect(isSconfitta(1)).toBe(false);
  });
});

describe('assegnaBloccante', () => {
  test('assegna immutabilmente', () => {
    const a = {};
    const b = assegnaBloccante(a, 'e1', 'c1');
    expect(b).toEqual({ e1: 'c1' });
    expect(a).toEqual({}); // originale invariato
  });
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/state/combat.test.js`
Expected: FAIL — `Cannot find module './combat'`.

- [ ] **Step 3: Crea combat.js**

`app/src/state/combat.js`:

```javascript
export function puoiAssalto({ turno, energia, costoAssalto, assaltoUsatoQuestoTurno }) {
  if (turno <= 1) return false;            // no Velocità: niente attacco al primo turno
  if (assaltoUsatoQuestoTurno) return false; // una volta per turno
  return energia >= costoAssalto;
}

export function spesaEnergia(energia, costo) {
  return Math.max(0, energia - costo);
}

export function dannoLeaderAttaccante(leader, dannoBloccante) {
  const eccesso = Math.max(0, dannoBloccante - leader.costituzione);
  return { ...leader, pv: leader.pv - eccesso };
}

export function isSconfitta(pv) {
  return pv <= 0;
}

export function assegnaBloccante(assegnazioni, attaccanteId, bloccanteId) {
  return { ...assegnazioni, [attaccanteId]: bloccanteId };
}
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/state/combat.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: logica combat Assalto/blocco/danno Leader"
```

---

### Task 4: Logica flip

**Files:**
- Create: `app/src/state/flip.js`
- Test: `app/src/state/flip.test.js`

**Interfaces:**
- Consumes: niente.
- Produces:
  - `valutaFlip(flip, ctx)` → boolean. `flip = { tipo, soglia }`. `ctx = { pv, numCreature, dannoAvversarioSubito }`.
    - `pv_max`: `ctx.pv <= soglia`
    - `controlli_creature`: `ctx.numCreature >= soglia`
    - `avversario_subito`: `ctx.dannoAvversarioSubito >= soglia`
  - `applicaFlip(leader)` → `{ ...leader, flipped: true, forza, costituzione }` (prende `formaFlip`, **PV invariati**)
  - `etichettaFlip(flip)` → string per il badge progresso (es. `"PV ≤ 15"`, `"3 creature"`, `"avversario subisce 10"`)

- [ ] **Step 1: Scrivi i test (falliscono)**

`app/src/state/flip.test.js`:

```javascript
import { valutaFlip, applicaFlip, etichettaFlip } from './flip';

describe('valutaFlip', () => {
  test('pv_max scatta quando pv <= soglia', () => {
    expect(valutaFlip({ tipo: 'pv_max', soglia: 15 }, { pv: 15 })).toBe(true);
    expect(valutaFlip({ tipo: 'pv_max', soglia: 15 }, { pv: 16 })).toBe(false);
  });
  test('controlli_creature scatta quando numCreature >= soglia', () => {
    expect(valutaFlip({ tipo: 'controlli_creature', soglia: 3 }, { numCreature: 3 })).toBe(true);
    expect(valutaFlip({ tipo: 'controlli_creature', soglia: 3 }, { numCreature: 2 })).toBe(false);
  });
  test('avversario_subito scatta quando danno >= soglia', () => {
    expect(valutaFlip({ tipo: 'avversario_subito', soglia: 10 }, { dannoAvversarioSubito: 10 })).toBe(true);
  });
});

describe('applicaFlip', () => {
  test('passa a forma flip, PV invariati', () => {
    const l = { pv: 22, forza: 4, costituzione: 3, flipped: false, formaFlip: { forza: 6, costituzione: 5 } };
    const f = applicaFlip(l);
    expect(f.flipped).toBe(true);
    expect(f.forza).toBe(6);
    expect(f.costituzione).toBe(5);
    expect(f.pv).toBe(22);
  });
});

describe('etichettaFlip', () => {
  test('pv_max', () => { expect(etichettaFlip({ tipo: 'pv_max', soglia: 15 })).toBe('PV ≤ 15'); });
  test('controlli_creature', () => { expect(etichettaFlip({ tipo: 'controlli_creature', soglia: 3 })).toBe('3 creature'); });
  test('avversario_subito', () => { expect(etichettaFlip({ tipo: 'avversario_subito', soglia: 10 })).toBe('avversario subisce 10'); });
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/state/flip.test.js`
Expected: FAIL — `Cannot find module './flip'`.

- [ ] **Step 3: Crea flip.js**

`app/src/state/flip.js`:

```javascript
export function valutaFlip(flip, ctx) {
  switch (flip.tipo) {
    case 'pv_max': return ctx.pv <= flip.soglia;
    case 'controlli_creature': return ctx.numCreature >= flip.soglia;
    case 'avversario_subito': return ctx.dannoAvversarioSubito >= flip.soglia;
    default: return false;
  }
}

export function applicaFlip(leader) {
  return {
    ...leader,
    flipped: true,
    forza: leader.formaFlip.forza,
    costituzione: leader.formaFlip.costituzione,
  };
}

export function etichettaFlip(flip) {
  switch (flip.tipo) {
    case 'pv_max': return `PV ≤ ${flip.soglia}`;
    case 'controlli_creature': return `${flip.soglia} creature`;
    case 'avversario_subito': return `avversario subisce ${flip.soglia}`;
    default: return '';
  }
}
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/state/flip.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: logica flip Leader (condizioni enumerate)"
```

---

### Task 5: Componente HexFaccia

**Files:**
- Create: `app/src/components/HexFaccia.js`
- Test: `app/src/components/HexFaccia.test.js`

**Interfaces:**
- Consumes: `COLORS`, `FAZIONI` da `theme.js`.
- Produces: `<HexFaccia pv pvMax fazione />` — mostra il numero PV; nessun cuore respawn, nessun badge †/⬡.

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/components/HexFaccia.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import { Text } from 'react-native';
import HexFaccia from './HexFaccia';

function testi(tree) {
  return tree.root.findAllByType(Text).map(t => t.props.children).flat().join(' ');
}

test('mostra i PV correnti', () => {
  const tree = renderer.create(<HexFaccia pv={35} pvMax={35} fazione="nord" />);
  expect(testi(tree)).toContain('35');
});

test('non mostra cuori respawn né badge rientro', () => {
  const tree = renderer.create(<HexFaccia pv={30} pvMax={35} fazione="sud" />);
  const txt = testi(tree);
  expect(txt).not.toContain('♥');
  expect(txt).not.toContain('⬡');
  expect(txt).not.toContain('†');
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/HexFaccia.test.js`
Expected: FAIL — `Cannot find module './HexFaccia'`.

- [ ] **Step 3: Crea HexFaccia.js**

`app/src/components/HexFaccia.js`:

```javascript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { COLORS, FAZIONI } from '../theme';

export default function HexFaccia({ pv, pvMax, fazione }) {
  const colore = FAZIONI[fazione] || COLORS.gold;
  const ratio = pvMax > 0 ? pv / pvMax : 0;
  const barColore = ratio > 0.4 ? COLORS.pvFull : COLORS.pvLow;
  return (
    <View style={[styles.hex, { borderColor: colore, shadowColor: colore }]}>
      <Text style={styles.pv}>{pv}</Text>
      <View style={styles.barOuter}>
        <View style={[styles.barInner, { width: `${Math.max(0, ratio) * 100}%`, backgroundColor: barColore }]} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  hex: { width: 50, height: 50, borderWidth: 2, borderRadius: 10, alignItems: 'center', justifyContent: 'center', backgroundColor: COLORS.bgTop },
  pv: { color: COLORS.gold, fontSize: 16, fontWeight: 'bold' },
  barOuter: { width: 40, height: 5, backgroundColor: '#222', borderRadius: 3, marginTop: 2, overflow: 'hidden' },
  barInner: { height: 5 },
});
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/HexFaccia.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: componente HexFaccia (PV senza respawn)"
```

---

### Task 6: Componente BarraEnergia

**Files:**
- Create: `app/src/components/BarraEnergia.js`
- Test: `app/src/components/BarraEnergia.test.js`

**Interfaces:**
- Consumes: `FAZIONI`.
- Produces: `<BarraEnergia energia energiaMax fazione />` — renderizza `energiaMax` pip, ognuno `testID="pip"`; i primi `energia` sono pieni (colore fazione) e marcati `accessibilityState={{ selected: true }}`, il resto spenti.

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/components/BarraEnergia.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import BarraEnergia from './BarraEnergia';

function pips(tree) {
  return tree.root.findAll(n => n.props && n.props.testID === 'pip');
}

test('renderizza energiaMax pip totali', () => {
  const tree = renderer.create(<BarraEnergia energia={2} energiaMax={5} fazione="nord" />);
  expect(pips(tree).length).toBe(5);
});

test('riempie esattamente energia pip', () => {
  const tree = renderer.create(<BarraEnergia energia={2} energiaMax={5} fazione="nord" />);
  const pieni = pips(tree).filter(p => p.props.accessibilityState && p.props.accessibilityState.selected);
  expect(pieni.length).toBe(2);
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/BarraEnergia.test.js`
Expected: FAIL — `Cannot find module './BarraEnergia'`.

- [ ] **Step 3: Crea BarraEnergia.js**

`app/src/components/BarraEnergia.js`:

```javascript
import React from 'react';
import { View, StyleSheet } from 'react-native';
import { FAZIONI } from '../theme';

export default function BarraEnergia({ energia, energiaMax, fazione }) {
  const colore = FAZIONI[fazione] || '#7a4acc';
  const pips = [];
  for (let i = 0; i < energiaMax; i++) {
    const pieno = i < energia;
    pips.push(
      <View
        key={i}
        testID="pip"
        accessibilityState={{ selected: pieno }}
        style={[styles.pip, { backgroundColor: pieno ? colore : '#2a2440', borderColor: colore }]}
      />
    );
  }
  return <View style={styles.row}>{pips}</View>;
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: 3 },
  pip: { width: 11, height: 11, borderRadius: 6, borderWidth: 1 },
});
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/BarraEnergia.test.js`
Expected: PASS (5 pip totali, 2 pieni).

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: componente BarraEnergia (pip tipizzati)"
```

---

### Task 7: CartaCreatura + ZonaCreature

**Files:**
- Create: `app/src/components/CartaCreatura.js`
- Create: `app/src/components/ZonaCreature.js`
- Test: `app/src/components/CartaCreatura.test.js`
- Test: `app/src/components/ZonaCreature.test.js`

**Interfaces:**
- Consumes: `FAZIONI`, `COLORS`.
- Produces:
  - `<CartaCreatura creatura />` — mostra nome, Forza (rosso), Costituzione (blu); se `creatura.tappata` applica `transform rotate`.
  - `<ZonaCreature creature lato />` — riga di `CartaCreatura`, `lato` = `'gk'|'av'`; renderizza una `CartaCreatura` per elemento (max 6).

- [ ] **Step 1: Scrivi i test (falliscono)**

`app/src/components/CartaCreatura.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import { Text } from 'react-native';
import CartaCreatura from './CartaCreatura';

const c = { id: 'c1', nome: 'Guerriero', fazione: 'nord', forza: 2, costituzione: 3, tappata: false };

test('mostra forza e costituzione', () => {
  const tree = renderer.create(<CartaCreatura creatura={c} />);
  const txt = tree.root.findAllByType(Text).map(t => String(t.props.children));
  expect(txt).toContain('2');
  expect(txt).toContain('3');
});

test('creatura tappata ha rotate', () => {
  const tree = renderer.create(<CartaCreatura creatura={{ ...c, tappata: true }} />);
  const root = tree.root.findAll(n => n.props && n.props.testID === 'carta-creatura')[0];
  const transforms = (Array.isArray(root.props.style) ? root.props.style : [root.props.style])
    .flatMap(s => (s && s.transform) ? s.transform : []);
  expect(JSON.stringify(transforms)).toContain('rotate');
});
```

`app/src/components/ZonaCreature.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import ZonaCreature from './ZonaCreature';
import CartaCreatura from './CartaCreatura';

test('renderizza una carta per creatura', () => {
  const creature = [
    { id: 'c1', nome: 'A', fazione: 'nord', forza: 1, costituzione: 1, tappata: false },
    { id: 'c2', nome: 'B', fazione: 'sud', forza: 2, costituzione: 2, tappata: false },
  ];
  const tree = renderer.create(<ZonaCreature creature={creature} lato="gk" />);
  expect(tree.root.findAllByType(CartaCreatura).length).toBe(2);
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/CartaCreatura.test.js src/components/ZonaCreature.test.js`
Expected: FAIL — moduli mancanti.

- [ ] **Step 3: Crea CartaCreatura.js**

`app/src/components/CartaCreatura.js`:

```javascript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { FAZIONI, COLORS } from '../theme';

export default function CartaCreatura({ creatura }) {
  const colore = FAZIONI[creatura.fazione] || COLORS.gold;
  const style = [
    styles.carta,
    { borderColor: colore, shadowColor: colore },
    creatura.tappata ? { transform: [{ rotate: '13deg' }], opacity: 0.72 } : null,
  ];
  return (
    <View testID="carta-creatura" style={style}>
      <View style={styles.art} />
      <Text style={styles.nome} numberOfLines={1}>{creatura.nome}</Text>
      <View style={styles.stats}>
        <Text style={[styles.stat, { color: COLORS.forza }]}>{creatura.forza}</Text>
        <Text style={[styles.stat, { color: COLORS.costituzione }]}>{creatura.costituzione}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  carta: { width: 54, height: 76, borderWidth: 1, borderRadius: 8, backgroundColor: COLORS.bgBottom, padding: 2 },
  art: { flex: 1, backgroundColor: 'rgba(100,60,180,0.15)', borderRadius: 4 },
  nome: { color: COLORS.gold, fontSize: 6, textAlign: 'center' },
  stats: { flexDirection: 'row', justifyContent: 'space-between', paddingHorizontal: 2 },
  stat: { fontSize: 10, fontWeight: 'bold' },
});
```

- [ ] **Step 4: Crea ZonaCreature.js**

`app/src/components/ZonaCreature.js`:

```javascript
import React from 'react';
import { View, StyleSheet } from 'react-native';
import CartaCreatura from './CartaCreatura';

export default function ZonaCreature({ creature, lato }) {
  return (
    <View style={[styles.riga, lato === 'av' ? styles.av : styles.gk]}>
      {creature.slice(0, 6).map(c => (
        <CartaCreatura key={c.id} creatura={c} />
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  riga: { flexDirection: 'row', gap: 8, paddingHorizontal: 56, justifyContent: 'center' },
  av: {},
  gk: {},
});
```

- [ ] **Step 5: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/CartaCreatura.test.js src/components/ZonaCreature.test.js`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: CartaCreatura + ZonaCreature (Forza/Costituzione)"
```

---

### Task 8: Componente SlotLeader

**Files:**
- Create: `app/src/components/SlotLeader.js`
- Test: `app/src/components/SlotLeader.test.js`

**Interfaces:**
- Consumes: `FAZIONI`, `COLORS`; `puoiAssalto` da `state/combat.js`; `etichettaFlip` da `state/flip.js`.
- Produces: `<SlotLeader leader turno energia onAssalto />` — mostra Forza/Costituzione (della forma corrente: base o flip), badge `Assalto N` (disabilitato se `!puoiAssalto`, `testID="badge-assalto"` con prop `accessibilityState.disabled`), badge progresso flip (`testID="badge-flip"`, testo da `etichettaFlip`, nascosto se `leader.flipped`). Se `leader.flipped`, contorno/marker forma potenziata.

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/components/SlotLeader.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import { Text } from 'react-native';
import SlotLeader from './SlotLeader';

const leader = {
  nome: 'Xirlia', fazione: 'nord', pv: 35, pvMax: 35, forza: 4, costituzione: 3,
  costoAssalto: 3, assaltoUsatoQuestoTurno: false, tappato: false,
  flip: { tipo: 'pv_max', soglia: 15 }, flipped: false, formaFlip: { forza: 6, costituzione: 5 },
};
function txt(tree) { return tree.root.findAllByType(Text).map(t => String(t.props.children)).join(' '); }
function nodo(tree, id) { return tree.root.findAll(n => n.props && n.props.testID === id)[0]; }

test('mostra Forza/Costituzione base e costo Assalto', () => {
  const tree = renderer.create(<SlotLeader leader={leader} turno={2} energia={3} onAssalto={() => {}} />);
  const t = txt(tree);
  expect(t).toContain('4');
  expect(t).toContain('3');
  expect(t).toContain('Assalto 3');
});

test('Assalto disabilitato al turno 1', () => {
  const tree = renderer.create(<SlotLeader leader={leader} turno={1} energia={3} onAssalto={() => {}} />);
  expect(nodo(tree, 'badge-assalto').props.accessibilityState.disabled).toBe(true);
});

test('Assalto abilitato con energia sufficiente al turno 2', () => {
  const tree = renderer.create(<SlotLeader leader={leader} turno={2} energia={3} onAssalto={() => {}} />);
  expect(nodo(tree, 'badge-assalto').props.accessibilityState.disabled).toBe(false);
});

test('mostra progresso flip se non flippato', () => {
  const tree = renderer.create(<SlotLeader leader={leader} turno={2} energia={3} onAssalto={() => {}} />);
  expect(txt(tree)).toContain('PV ≤ 15');
});

test('flippato: usa stat forma flip e nasconde badge flip', () => {
  const tree = renderer.create(<SlotLeader leader={{ ...leader, flipped: true, forza: 6, costituzione: 5 }} turno={2} energia={3} onAssalto={() => {}} />);
  expect(txt(tree)).toContain('6');
  expect(tree.root.findAll(n => n.props && n.props.testID === 'badge-flip').length).toBe(0);
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/SlotLeader.test.js`
Expected: FAIL — `Cannot find module './SlotLeader'`.

- [ ] **Step 3: Crea SlotLeader.js**

`app/src/components/SlotLeader.js`:

```javascript
import React from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { FAZIONI, COLORS } from '../theme';
import { puoiAssalto } from '../state/combat';
import { etichettaFlip } from '../state/flip';

export default function SlotLeader({ leader, turno, energia, onAssalto }) {
  const colore = FAZIONI[leader.fazione] || COLORS.gold;
  const abilitato = puoiAssalto({
    turno, energia, costoAssalto: leader.costoAssalto,
    assaltoUsatoQuestoTurno: leader.assaltoUsatoQuestoTurno,
  });
  const slotStyle = [
    styles.slot,
    { borderColor: colore, shadowColor: colore },
    leader.flipped ? styles.flipped : null,
    leader.tappato ? { transform: [{ rotate: '13deg' }], opacity: 0.72 } : null,
  ];
  return (
    <View testID="slot-leader" style={slotStyle}>
      <View style={styles.art} />
      <Text style={styles.nome} numberOfLines={1}>{leader.nome}</Text>
      <View style={styles.stats}>
        <Text style={[styles.stat, { color: COLORS.forza }]}>{leader.forza}</Text>
        <Text style={[styles.stat, { color: COLORS.costituzione }]}>{leader.costituzione}</Text>
      </View>
      <Pressable
        testID="badge-assalto"
        accessibilityState={{ disabled: !abilitato }}
        disabled={!abilitato}
        onPress={onAssalto}
        style={[styles.assalto, { opacity: abilitato ? 1 : 0.4 }]}
      >
        <Text style={styles.assaltoTxt}>{`Assalto ${leader.costoAssalto}`}</Text>
      </Pressable>
      {!leader.flipped && (
        <View testID="badge-flip" style={styles.flipBadge}>
          <Text style={styles.flipTxt}>{etichettaFlip(leader.flip)}</Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  slot: { width: 64, height: 88, borderWidth: 2, borderRadius: 8, backgroundColor: COLORS.bgBottom, padding: 2 },
  flipped: { borderColor: COLORS.gold, borderWidth: 3 },
  art: { flex: 1, backgroundColor: 'rgba(201,168,76,0.15)', borderRadius: 4 },
  nome: { color: COLORS.gold, fontSize: 7, textAlign: 'center' },
  stats: { flexDirection: 'row', justifyContent: 'space-between', paddingHorizontal: 2 },
  stat: { fontSize: 11, fontWeight: 'bold' },
  assalto: { marginTop: 1, backgroundColor: '#2a2440', borderRadius: 3, alignItems: 'center' },
  assaltoTxt: { color: COLORS.gold, fontSize: 6 },
  flipBadge: { position: 'absolute', top: -7, right: -2, backgroundColor: '#000a', borderRadius: 3, paddingHorizontal: 2 },
  flipTxt: { color: COLORS.gold, fontSize: 6 },
});
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/SlotLeader.test.js`
Expected: PASS (5 test).

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: SlotLeader (Assalto badge + progresso/forma flip)"
```

---

### Task 9: Componente OverlayBlocco

**Files:**
- Create: `app/src/components/OverlayBlocco.js`
- Test: `app/src/components/OverlayBlocco.test.js`

**Interfaces:**
- Consumes: `COLORS`; `assegnaBloccante` da `state/combat.js`.
- Produces: `<OverlayBlocco attaccanti difensori onConferma />` — visibile solo in fase blocco. Per ogni attaccante un nodo `testID="attaccante"`; premendo un difensore (`testID="difensore"`) lo assegna all'attaccante selezionato; bottone `testID="conferma-blocco"` chiama `onConferma(assegnazioni)`. **Nessun difensore può essere il Leader** (la lista `difensori` passata non include il Leader — vincolo del chiamante).

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/components/OverlayBlocco.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import OverlayBlocco from './OverlayBlocco';

const attaccanti = [{ id: 'e1', nome: 'Drago' }];
const difensori = [{ id: 'c1', nome: 'Guerriero' }, { id: 'c2', nome: 'Mago' }];

function nodi(tree, id) { return tree.root.findAll(n => n.props && n.props.testID === id); }

test('renderizza attaccanti e difensori', () => {
  const tree = renderer.create(<OverlayBlocco attaccanti={attaccanti} difensori={difensori} onConferma={() => {}} />);
  expect(nodi(tree, 'attaccante').length).toBe(1);
  expect(nodi(tree, 'difensore').length).toBe(2);
});

test('conferma restituisce le assegnazioni fatte', () => {
  let result = null;
  let tree;
  renderer.act(() => {
    tree = renderer.create(<OverlayBlocco attaccanti={attaccanti} difensori={difensori} onConferma={(a) => { result = a; }} />);
  });
  // seleziona attaccante e1, poi difensore c1
  renderer.act(() => { nodi(tree, 'attaccante')[0].props.onPress(); });
  renderer.act(() => { nodi(tree, 'difensore')[0].props.onPress(); });
  renderer.act(() => { nodi(tree, 'conferma-blocco')[0].props.onPress(); });
  expect(result).toEqual({ e1: 'c1' });
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/OverlayBlocco.test.js`
Expected: FAIL — `Cannot find module './OverlayBlocco'`.

- [ ] **Step 3: Crea OverlayBlocco.js**

`app/src/components/OverlayBlocco.js`:

```javascript
import React, { useState } from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { COLORS } from '../theme';
import { assegnaBloccante } from '../state/combat';

export default function OverlayBlocco({ attaccanti, difensori, onConferma }) {
  const [selezionato, setSelezionato] = useState(null); // id attaccante
  const [assegnazioni, setAssegnazioni] = useState({});

  function premiDifensore(idDifensore) {
    if (!selezionato) return;
    setAssegnazioni(prev => assegnaBloccante(prev, selezionato, idDifensore));
  }

  return (
    <View style={styles.overlay}>
      <Text style={styles.titolo}>Assegna bloccanti</Text>
      <View style={styles.riga}>
        {attaccanti.map(a => (
          <Pressable key={a.id} testID="attaccante" onPress={() => setSelezionato(a.id)}
            style={[styles.carta, selezionato === a.id ? styles.sel : null]}>
            <Text style={styles.txt}>{a.nome}</Text>
            {assegnazioni[a.id] ? <Text style={styles.blk}>⛨ {assegnazioni[a.id]}</Text> : null}
          </Pressable>
        ))}
      </View>
      <View style={styles.riga}>
        {difensori.map(d => (
          <Pressable key={d.id} testID="difensore" onPress={() => premiDifensore(d.id)} style={styles.carta}>
            <Text style={styles.txt}>{d.nome}</Text>
          </Pressable>
        ))}
      </View>
      <Pressable testID="conferma-blocco" onPress={() => onConferma(assegnazioni)} style={styles.conferma}>
        <Text style={styles.confermaTxt}>Conferma</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  overlay: { position: 'absolute', left: 0, right: 0, top: 0, bottom: 0, backgroundColor: '#000c', justifyContent: 'center', padding: 16, zIndex: 200 },
  titolo: { color: COLORS.gold, fontSize: 14, textAlign: 'center', marginBottom: 12 },
  riga: { flexDirection: 'row', gap: 8, justifyContent: 'center', marginVertical: 8 },
  carta: { width: 54, height: 76, borderWidth: 1, borderColor: COLORS.gold, borderRadius: 8, alignItems: 'center', justifyContent: 'center', backgroundColor: COLORS.bgBottom },
  sel: { borderColor: '#fff', borderWidth: 2 },
  txt: { color: COLORS.gold, fontSize: 8, textAlign: 'center' },
  blk: { color: '#fff', fontSize: 7 },
  conferma: { marginTop: 16, alignSelf: 'center', backgroundColor: COLORS.gold, borderRadius: 6, paddingHorizontal: 20, paddingVertical: 8 },
  confermaTxt: { color: '#000', fontWeight: 'bold' },
});
```

- [ ] **Step 4: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/OverlayBlocco.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: OverlayBlocco (assegnazione bloccanti, Leader escluso)"
```

---

### Task 10: Pannelli, mano, indicatore turno

**Files:**
- Create: `app/src/components/PannelloLaterale.js`
- Create: `app/src/components/FanMano.js`
- Create: `app/src/components/IndicatoreTurnoFase.js`
- Test: `app/src/components/PannelloLaterale.test.js`
- Test: `app/src/components/IndicatoreTurnoFase.test.js`

**Interfaces:**
- Consumes: `COLORS`.
- Produces:
  - `<PannelloLaterale mazzo cimitero />` — mini-carte Mazzo (count) e Cimitero (count). **Nessuna mini-carta Leader** (rimossa dal modello vecchio).
  - `<FanMano carte />` — fan di `carte` (presentazionale).
  - `<IndicatoreTurnoFase turno fase />` — mostra numero turno e label fase (`azioni` → "Fase Azioni", `blocco` → "⚔ Fase Blocco").

- [ ] **Step 1: Scrivi i test (falliscono)**

`app/src/components/PannelloLaterale.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import { Text } from 'react-native';
import PannelloLaterale from './PannelloLaterale';

function txt(tree) { return tree.root.findAllByType(Text).map(t => String(t.props.children)).join(' '); }

test('mostra count mazzo e cimitero, niente Leader', () => {
  const tree = renderer.create(<PannelloLaterale mazzo={24} cimitero={3} />);
  const t = txt(tree);
  expect(t).toContain('24');
  expect(t).toContain('3');
  expect(t).not.toContain('♛');
});
```

`app/src/components/IndicatoreTurnoFase.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import { Text } from 'react-native';
import IndicatoreTurnoFase from './IndicatoreTurnoFase';

function txt(tree) { return tree.root.findAllByType(Text).map(t => String(t.props.children)).join(' '); }

test('mostra turno e fase azioni', () => {
  const tree = renderer.create(<IndicatoreTurnoFase turno={3} fase="azioni" />);
  expect(txt(tree)).toContain('3');
  expect(txt(tree)).toContain('Fase Azioni');
});

test('fase blocco', () => {
  const tree = renderer.create(<IndicatoreTurnoFase turno={3} fase="blocco" />);
  expect(txt(tree)).toContain('Fase Blocco');
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/PannelloLaterale.test.js src/components/IndicatoreTurnoFase.test.js`
Expected: FAIL — moduli mancanti.

- [ ] **Step 3: Crea PannelloLaterale.js**

`app/src/components/PannelloLaterale.js`:

```javascript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { COLORS } from '../theme';

function Mini({ label, count, borderColor }) {
  return (
    <View style={[styles.mc, { borderColor }]}>
      <Text style={styles.label}>{label}</Text>
      <Text style={styles.count}>{count}</Text>
    </View>
  );
}

export default function PannelloLaterale({ mazzo, cimitero }) {
  return (
    <View style={styles.pan}>
      <Mini label="Mazzo" count={mazzo} borderColor="#2a2a4a" />
      <Mini label="Cimitero" count={cimitero} borderColor="#4a1a1a" />
    </View>
  );
}

const styles = StyleSheet.create({
  pan: { gap: 6 },
  mc: { width: 42, height: 58, borderWidth: 1, borderRadius: 6, backgroundColor: COLORS.bgBottom, alignItems: 'center', justifyContent: 'center' },
  label: { color: COLORS.gold, fontSize: 5.5 },
  count: { color: COLORS.gold, fontSize: 12, fontWeight: 'bold' },
});
```

- [ ] **Step 4: Crea FanMano.js**

`app/src/components/FanMano.js`:

```javascript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { FAZIONI, COLORS } from '../theme';

export default function FanMano({ carte }) {
  const n = carte.length;
  return (
    <View style={styles.wrap}>
      {carte.map((c, i) => {
        const angolo = (i - (n - 1) / 2) * 8;
        return (
          <View key={c.id} style={[styles.carta, { borderColor: FAZIONI[c.fazione] || COLORS.gold, transform: [{ rotate: `${angolo}deg` }] }]}>
            <Text style={styles.costo}>{c.costo}</Text>
            <Text style={styles.nome} numberOfLines={1}>{c.nome}</Text>
          </View>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { flexDirection: 'row', justifyContent: 'center', alignItems: 'flex-end', height: 110 },
  carta: { width: 70, height: 98, marginHorizontal: -10, borderWidth: 1, borderRadius: 8, backgroundColor: COLORS.bgBottom, padding: 3 },
  costo: { color: COLORS.gold, fontSize: 10, fontWeight: 'bold' },
  nome: { color: COLORS.gold, fontSize: 7, textAlign: 'center', marginTop: 'auto' },
});
```

- [ ] **Step 5: Crea IndicatoreTurnoFase.js**

`app/src/components/IndicatoreTurnoFase.js`:

```javascript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { COLORS } from '../theme';

const LABEL = { azioni: 'Fase Azioni', blocco: '⚔ Fase Blocco' };

export default function IndicatoreTurnoFase({ turno, fase }) {
  return (
    <View style={styles.wrap}>
      <View style={styles.box}><Text style={styles.num}>{turno}</Text></View>
      <Text style={styles.fase}>{LABEL[fase] || fase}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { alignItems: 'center' },
  box: { width: 36, height: 36, borderWidth: 1, borderColor: COLORS.gold, borderRadius: 6, alignItems: 'center', justifyContent: 'center' },
  num: { color: COLORS.gold, fontSize: 17 },
  fase: { color: COLORS.gold, fontSize: 8, marginTop: 2 },
});
```

- [ ] **Step 6: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/PannelloLaterale.test.js src/components/IndicatoreTurnoFase.test.js`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: PannelloLaterale (no Leader) + FanMano + IndicatoreTurnoFase"
```

---

### Task 11: Composizione CampoBattaglia + wiring App.js

**Files:**
- Create: `app/src/components/CampoBattaglia.js`
- Modify: `app/App.js`
- Test: `app/src/components/CampoBattaglia.test.js`

**Interfaces:**
- Consumes: tutti i componenti precedenti; `creaMockState` da `state/mockState.js`.
- Produces: `<CampoBattaglia state />` — compone: indicatore turno (top), zona/slot/hex/energia avversario (top), divisore, zona/slot/hex/energia giocatore (bottom), pannelli laterali, fan mano; mostra `OverlayBlocco` solo se `state.fase === 'blocco'`. `App.js` monta `<CampoBattaglia state={creaMockState()} />`.

- [ ] **Step 1: Scrivi il test (fallisce)**

`app/src/components/CampoBattaglia.test.js`:

```javascript
import React from 'react';
import renderer from 'react-test-renderer';
import CampoBattaglia from './CampoBattaglia';
import { creaMockState } from '../state/mockState';
import SlotLeader from './SlotLeader';
import HexFaccia from './HexFaccia';
import OverlayBlocco from './OverlayBlocco';

test('compone due SlotLeader e due HexFaccia', () => {
  const tree = renderer.create(<CampoBattaglia state={creaMockState()} />);
  expect(tree.root.findAllByType(SlotLeader).length).toBe(2);
  expect(tree.root.findAllByType(HexFaccia).length).toBe(2);
});

test('overlay blocco nascosto in fase azioni', () => {
  const tree = renderer.create(<CampoBattaglia state={creaMockState()} />);
  expect(tree.root.findAllByType(OverlayBlocco).length).toBe(0);
});

test('overlay blocco visibile in fase blocco', () => {
  const s = creaMockState();
  s.fase = 'blocco';
  const tree = renderer.create(<CampoBattaglia state={s} />);
  expect(tree.root.findAllByType(OverlayBlocco).length).toBe(1);
});
```

- [ ] **Step 2: Esegui — deve fallire**

Run: `cd "app" && npm test -- src/components/CampoBattaglia.test.js`
Expected: FAIL — `Cannot find module './CampoBattaglia'`.

- [ ] **Step 3: Crea CampoBattaglia.js**

`app/src/components/CampoBattaglia.js`:

```javascript
import React from 'react';
import { View, StyleSheet } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { COLORS } from '../theme';
import IndicatoreTurnoFase from './IndicatoreTurnoFase';
import ZonaCreature from './ZonaCreature';
import SlotLeader from './SlotLeader';
import HexFaccia from './HexFaccia';
import BarraEnergia from './BarraEnergia';
import PannelloLaterale from './PannelloLaterale';
import FanMano from './FanMano';
import OverlayBlocco from './OverlayBlocco';

function MetaCampo({ g, turno, lato }) {
  return (
    <View style={styles.meta}>
      <View style={styles.colonnaLeader}>
        <SlotLeader leader={g.leader} turno={turno} energia={g.energia} onAssalto={() => {}} />
      </View>
      <ZonaCreature creature={g.creature} lato={lato} />
      <View style={styles.colonnaDestra}>
        <HexFaccia pv={g.leader.pv} pvMax={g.leader.pvMax} fazione={g.fazione} />
        <BarraEnergia energia={g.energia} energiaMax={g.energiaMax} fazione={g.fazione} />
        <PannelloLaterale mazzo={g.mazzo} cimitero={g.cimitero} />
      </View>
    </View>
  );
}

export default function CampoBattaglia({ state }) {
  const { gk, av } = state.giocatori;
  const difensori = gk.creature.filter(c => !c.tappata).map(c => ({ id: c.id, nome: c.nome }));
  const attaccanti = av.creature.map(c => ({ id: c.id, nome: c.nome }));
  return (
    <LinearGradient colors={[COLORS.bgTop, COLORS.bgBottom]} style={styles.bg}>
      <IndicatoreTurnoFase turno={state.turno} fase={state.fase} />
      <MetaCampo g={av} turno={state.turno} lato="av" />
      <View style={styles.divisore} />
      <MetaCampo g={gk} turno={state.turno} lato="gk" />
      <FanMano carte={gk.mano} />
      {state.fase === 'blocco' && (
        <OverlayBlocco attaccanti={attaccanti} difensori={difensori} onConferma={() => {}} />
      )}
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  bg: { flex: 1, paddingTop: 10 },
  meta: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', flex: 1 },
  colonnaLeader: { paddingLeft: 8 },
  colonnaDestra: { alignItems: 'flex-end', paddingRight: 8, gap: 6 },
  divisore: { height: 2, backgroundColor: COLORS.gold, marginVertical: 4 },
});
```

- [ ] **Step 4: Modifica App.js**

Sostituisci interamente `app/App.js` con:

```javascript
import React from 'react';
import { SafeAreaView, StyleSheet } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import CampoBattaglia from './src/components/CampoBattaglia';
import { creaMockState } from './src/state/mockState';

export default function App() {
  return (
    <SafeAreaView style={styles.root}>
      <StatusBar style="light" />
      <CampoBattaglia state={creaMockState()} />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: '#050508' },
});
```

- [ ] **Step 5: Esegui — deve passare**

Run: `cd "app" && npm test -- src/components/CampoBattaglia.test.js`
Expected: PASS (3 test).

- [ ] **Step 6: Esegui tutta la suite**

Run: `cd "app" && npm test`
Expected: PASS — tutti i file di test verdi.

- [ ] **Step 7: Commit**

```bash
cd "app" && git add -A && git commit -m "feat: CampoBattaglia composizione + wiring App.js"
```

---

## Note finali

- **Interattività completa (tap→attacco→spesa energia→risoluzione, flip live, conferma blocco che muta lo stato) è fuori scope di questo piano**: i componenti espongono gli intent (`onAssalto`, `onConferma`) ma il mock è statico. Il cablaggio allo stato vivo e poi al motore `engine-cs` è la fase successiva (vedi `tasks/todo.md`).
- **Questioni aperte da confermare col socio** (segnate nello spec §6): combat blocco vs HS-diretto del DESIGN_V1 locale; flip off-turn lato motore; naming Forza/Costituzione sulle creature; energia multi-fazione.
- Se si vuole costruire via **app.emergent.sh**: questo piano + lo spec sono il brief; i componenti `src/` e i loro test sono il contratto da rispettare.
