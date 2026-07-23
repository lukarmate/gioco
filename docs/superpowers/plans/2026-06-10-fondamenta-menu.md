# Fondamenta + Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Costruire le fondamenta visive + di navigazione dell'app TCG e i primi due schermi reali (Home e Opzioni), sostituendo il mockup statico attuale.

**Architecture:** Expo + expo-router (navigazione file-based). Design system centralizzato in `src/theme` e `src/components`; ogni schermo monta solo componenti del design system. Logica testabile (i18n, persistenza impostazioni) isolata in moduli puri con unit test; gli schermi hanno smoke test di render. Verifica visiva finale via Expo Go / simulatore.

**Tech Stack:** React Native 0.76, Expo ~52, expo-router, expo-font, react-native-reanimated, @react-native-async-storage/async-storage, @expo/vector-icons, jest-expo + @testing-library/react-native.

**Working dir:** `app/` (dentro repo `gioco`). Branch: `feature/fondamenta-menu`.

---

## File Structure

```
app/
├── app.json                        modificato: scheme, plugin expo-router, expo-font
├── package.json                    modificato: deps + script test, main = expo-router/entry
├── babel.config.js                 modificato: plugin reanimated
├── jest.config.js                  nuovo: preset jest-expo
├── jest.setup.js                   nuovo: mock AsyncStorage, reanimated
├── App.js                          rimosso (sostituito da app/ router)
├── index.js                        rimosso (sostituito da expo-router/entry)
├── assets/fonts/                   nuovo: Cinzel-Regular.ttf, Cinzel-Bold.ttf, CrimsonText-Regular.ttf, CrimsonText-Italic.ttf
├── app/                            schermi (expo-router)
│   ├── _layout.js                  root: font, ThemeProvider, SettingsProvider, Stack
│   ├── index.js                    HOME
│   ├── opzioni.js                  OPZIONI
│   ├── gioca.js                    placeholder "In arrivo"
│   ├── collezione.js               placeholder "In arrivo"
│   └── negozio.js                  placeholder "In arrivo"
├── src/
│   ├── theme/
│   │   ├── colors.js               palette
│   │   ├── tokens.js               spacing, radius, glow, fontSizes
│   │   └── index.js                export aggregato + nomi font
│   ├── components/
│   │   ├── ScreenBg.js             sfondo gradiente
│   │   ├── Button.js               primario + secondario
│   │   ├── Panel.js                riquadro bordo oro
│   │   ├── Toggle.js               switch on/off
│   │   ├── Slider.js               slider 0-100
│   │   ├── OptionStepper.js        selettore ‹ valore ›
│   │   ├── Modal.js                modale a tema
│   │   ├── Title.js                titolo Cinzel con glow
│   │   └── ComingSoon.js           contenuto schermo placeholder
│   ├── i18n/
│   │   ├── strings.js              dizionario IT/EN
│   │   └── index.js                useT() hook + provider lingua
│   └── state/
│       └── settings.js             SettingsProvider + persistenza AsyncStorage
└── mockups/                        nuovo (design-first): home.html, opzioni.html
```

---

## Task 0: Mockup browser (design-first)

Validare il look di Home + Opzioni nel companion visivo PRIMA di scrivere RN. Nessun test automatico: la verifica è l'approvazione visiva di Luca.

**Files:**
- Create: `app/mockups/home.html`
- Create: `app/mockups/opzioni.html`

- [ ] **Step 1: Creare mockup HTML statici** di Home e Opzioni a 390×844, usando palette/font della spec (oro `#c9a84c`, gradiente `#050508→#0c0618`, viola cosmico, Cinzel via Google Fonts CDN). Layout = Sezioni C/D del design doc.

- [ ] **Step 2: Mostrare nel companion visivo** e iterare su colori/glow/spaziatura/gerarchia finché Luca approva. Annotare ogni scostamento dalla palette base in un commento HTML in cima al file (servirà come riferimento per i token RN nel Task 2).

- [ ] **Step 3: Commit**

```bash
git add app/mockups/
git commit -m "design: mockup HTML Home + Opzioni approvati"
```

**Verifica:** Luca conferma a voce il look dei due mockup. Gate: non procedere a Task 1 senza approvazione.

---

## Task 1: Setup dipendenze + scaffolding expo-router

**Files:**
- Modify: `app/package.json`
- Modify: `app/app.json`
- Modify: `app/babel.config.js`
- Remove: `app/App.js`, `app/index.js`
- Create: `app/app/_layout.js`
- Create: `app/app/index.js` (temporaneo, schermo vuoto)

- [ ] **Step 1: Installare le dipendenze**

```bash
cd app
npx expo install expo-router react-native-safe-area-context react-native-screens expo-linking expo-constants expo-font react-native-reanimated @react-native-async-storage/async-storage @expo/vector-icons
```

- [ ] **Step 2: Impostare `main` su expo-router** in `app/package.json`

```json
"main": "expo-router/entry",
```

E aggiungere lo script di test (usato dai task successivi):

```json
"scripts": {
  "start": "expo start",
  "ios": "expo start --ios",
  "android": "expo start --android",
  "test": "jest"
}
```

- [ ] **Step 3: Configurare `app/app.json`** — aggiungere `scheme` e il plugin expo-router/font

```json
{
  "expo": {
    "name": "Gioco",
    "slug": "gioco",
    "scheme": "gioco",
    "version": "1.0.0",
    "orientation": "portrait",
    "userInterfaceStyle": "dark",
    "ios": { "supportsTablet": false, "bundleIdentifier": "com.gioco.tcg" },
    "android": { "package": "com.gioco.tcg" },
    "plugins": ["expo-router", "expo-font"]
  }
}
```

- [ ] **Step 4: Abilitare reanimated in `app/babel.config.js`**

```js
module.exports = function (api) {
  api.cache(true);
  return {
    presets: ['babel-preset-expo'],
    plugins: ['react-native-reanimated/plugin'],
  };
};
```

- [ ] **Step 5: Rimuovere i vecchi entry**

```bash
git rm app/App.js app/index.js
```

- [ ] **Step 6: Creare root layout minimo** `app/app/_layout.js`

```js
import { Stack } from 'expo-router';

export default function RootLayout() {
  return <Stack screenOptions={{ headerShown: false }} />;
}
```

- [ ] **Step 7: Creare schermo home temporaneo** `app/app/index.js`

```js
import { View, Text } from 'react-native';

export default function Home() {
  return (
    <View style={{ flex: 1, backgroundColor: '#050508', justifyContent: 'center', alignItems: 'center' }}>
      <Text style={{ color: '#c9a84c' }}>boot ok</Text>
    </View>
  );
}
```

- [ ] **Step 8: Verificare il boot**

Run: `cd app && npx expo start`
Expected: l'app si avvia su Expo Go / simulatore e mostra "boot ok" su sfondo scuro. Nessun errore rosso.

- [ ] **Step 9: Commit**

```bash
git add app/package.json app/app.json app/babel.config.js app/app/
git commit -m "feat: scaffolding expo-router + dipendenze base"
```

---

## Task 2: Setup test infra + design tokens

**Files:**
- Create: `app/jest.config.js`
- Create: `app/jest.setup.js`
- Create: `app/src/theme/colors.js`
- Create: `app/src/theme/tokens.js`
- Create: `app/src/theme/index.js`
- Test: `app/src/theme/__tests__/theme.test.js`

- [ ] **Step 1: Installare dev deps di test**

```bash
cd app
npx expo install --dev jest-expo jest @testing-library/react-native
```

- [ ] **Step 2: Configurare jest** `app/jest.config.js`

```js
module.exports = {
  preset: 'jest-expo',
  setupFiles: ['<rootDir>/jest.setup.js'],
  transformIgnorePatterns: [
    'node_modules/(?!((jest-)?react-native|@react-native(-community)?|expo(nent)?|@expo(nent)?/.*|@expo-google-fonts/.*|react-navigation|@react-navigation/.*|@unimodules/.*|unimodules|sentry-expo|native-base|react-native-svg|@react-native-async-storage/.*|react-native-reanimated))',
  ],
};
```

- [ ] **Step 3: Mock globali** `app/jest.setup.js`

```js
import '@testing-library/react-native/extend-expect';
jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock')
);
require('react-native-reanimated').setUpTests?.();
```

- [ ] **Step 4: Scrivere il test dei token (deve fallire)** `app/src/theme/__tests__/theme.test.js`

```js
import { colors, tokens, fonts } from '../index';

test('colori chiave presenti e corretti', () => {
  expect(colors.gold).toBe('#c9a84c');
  expect(colors.bgTop).toBe('#050508');
  expect(colors.bgBottom).toBe('#0c0618');
  expect(colors.faction.nord).toBe('#5588cc');
});

test('token spacing e font name', () => {
  expect(tokens.spacing.md).toBeGreaterThan(0);
  expect(fonts.title).toBe('Cinzel-Bold');
});
```

- [ ] **Step 5: Eseguire il test — deve fallire**

Run: `cd app && npm test -- theme`
Expected: FAIL ("Cannot find module '../index'").

- [ ] **Step 6: Implementare i token** — `app/src/theme/colors.js`

```js
export const colors = {
  gold: '#c9a84c',
  goldDim: 'rgba(201,168,76,0.6)',
  bgTop: '#050508',
  bgBottom: '#0c0618',
  cosmic: 'rgba(100,60,180,0.6)',
  hpPlayer: '#27ae60',
  hpEnemy: '#c0392b',
  mana: '#7a4acc',
  text: '#e8e2d0',
  faction: {
    nord: '#5588cc', sud: '#cc4422', est: '#44aa66', ovest: '#8844aa', centro: '#aaaaaa',
  },
};
```

`app/src/theme/tokens.js`

```js
export const tokens = {
  spacing: { xs: 4, sm: 8, md: 16, lg: 24, xl: 40 },
  radius: { sm: 8, md: 12, lg: 20, pill: 999 },
  fontSize: { sm: 12, md: 15, lg: 20, xl: 30, title: 44 },
  glow: { color: '#c9a84c', shadowOpacity: 0.6, shadowRadius: 12, shadowOffset: { width: 0, height: 0 } },
};

export const fonts = {
  title: 'Cinzel-Bold',
  ui: 'Cinzel-Regular',
  body: 'CrimsonText-Regular',
  bodyItalic: 'CrimsonText-Italic',
};
```

`app/src/theme/index.js`

```js
export { colors } from './colors';
export { tokens, fonts } from './tokens';
```

- [ ] **Step 7: Eseguire il test — deve passare**

Run: `cd app && npm test -- theme`
Expected: PASS (2 test).

- [ ] **Step 8: Commit**

```bash
git add app/jest.config.js app/jest.setup.js app/src/theme/
git commit -m "feat: test infra (jest-expo) + design tokens"
```

---

## Task 3: Caricamento font

**Files:**
- Create: `app/assets/fonts/` (Cinzel-Regular.ttf, Cinzel-Bold.ttf, CrimsonText-Regular.ttf, CrimsonText-Italic.ttf)
- Modify: `app/app/_layout.js`

- [ ] **Step 1: Scaricare i font** (Google Fonts) in `app/assets/fonts/`

```bash
cd app && mkdir -p assets/fonts && cd assets/fonts
curl -sL -o Cinzel-Regular.ttf "https://github.com/google/fonts/raw/main/ofl/cinzel/Cinzel%5Bwght%5D.ttf"
curl -sL -o CrimsonText-Regular.ttf "https://github.com/google/fonts/raw/main/ofl/crimsontext/CrimsonText-Regular.ttf"
curl -sL -o CrimsonText-Italic.ttf "https://github.com/google/fonts/raw/main/ofl/crimsontext/CrimsonText-Italic.ttf"
```

Nota: Cinzel su Google Fonts è variable (un solo file `Cinzel[wght].ttf`). Caricarlo due volte con nomi logici `Cinzel-Regular` e `Cinzel-Bold` puntando allo stesso file è accettabile; il peso visivo si regola con `fontWeight` dove serve. Copiare il file:

```bash
cp Cinzel-Regular.ttf Cinzel-Bold.ttf
```

Verifica: `ls -la app/assets/fonts/` mostra 4 file `.ttf` non vuoti (size > 10KB).

- [ ] **Step 2: Caricare i font nel root layout** `app/app/_layout.js`

```js
import { Stack } from 'expo-router';
import { useFonts } from 'expo-font';
import { View } from 'react-native';
import { colors } from '../src/theme';

export default function RootLayout() {
  const [loaded] = useFonts({
    'Cinzel-Regular': require('../assets/fonts/Cinzel-Regular.ttf'),
    'Cinzel-Bold': require('../assets/fonts/Cinzel-Bold.ttf'),
    'CrimsonText-Regular': require('../assets/fonts/CrimsonText-Regular.ttf'),
    'CrimsonText-Italic': require('../assets/fonts/CrimsonText-Italic.ttf'),
  });

  if (!loaded) {
    return <View style={{ flex: 1, backgroundColor: colors.bgTop }} />;
  }

  return <Stack screenOptions={{ headerShown: false }} />;
}
```

- [ ] **Step 3: Verifica visiva** — temporaneamente impostare `fontFamily: 'Cinzel-Bold'` sul Text "boot ok" in `app/app/index.js`, avviare `npx expo start` e confermare che il testo usa il serif Cinzel. Poi ripristinare.

- [ ] **Step 4: Commit**

```bash
git add app/assets/fonts/ app/app/_layout.js
git commit -m "feat: caricamento font Cinzel + Crimson Text"
```

---

## Task 4: Componenti base — ScreenBg, Title, Panel, Button

**Files:**
- Create: `app/src/components/ScreenBg.js`
- Create: `app/src/components/Title.js`
- Create: `app/src/components/Panel.js`
- Create: `app/src/components/Button.js`
- Test: `app/src/components/__tests__/base.test.js`

- [ ] **Step 1: Scrivere i test di render (devono fallire)** `app/src/components/__tests__/base.test.js`

```js
import { render, screen, fireEvent } from '@testing-library/react-native';
import ScreenBg from '../ScreenBg';
import Title from '../Title';
import Panel from '../Panel';
import Button from '../Button';
import { Text } from 'react-native';

test('ScreenBg rende i figli', () => {
  render(<ScreenBg><Text>ciao</Text></ScreenBg>);
  expect(screen.getByText('ciao')).toBeOnTheScreen();
});

test('Title mostra il testo', () => {
  render(<Title>GIOCO</Title>);
  expect(screen.getByText('GIOCO')).toBeOnTheScreen();
});

test('Panel rende i figli', () => {
  render(<Panel><Text>contenuto</Text></Panel>);
  expect(screen.getByText('contenuto')).toBeOnTheScreen();
});

test('Button mostra label e chiama onPress', () => {
  const onPress = jest.fn();
  render(<Button label="GIOCA" onPress={onPress} />);
  fireEvent.press(screen.getByText('GIOCA'));
  expect(onPress).toHaveBeenCalledTimes(1);
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- base`
Expected: FAIL (moduli non trovati).

- [ ] **Step 3: Implementare `ScreenBg.js`**

```js
import { LinearGradient } from 'expo-linear-gradient';
import { SafeAreaView, StyleSheet } from 'react-native';
import { colors, tokens } from '../theme';

export default function ScreenBg({ children }) {
  return (
    <LinearGradient colors={[colors.bgTop, colors.bgBottom]} style={styles.fill}>
      <SafeAreaView style={styles.safe}>{children}</SafeAreaView>
    </LinearGradient>
  );
}
const styles = StyleSheet.create({
  fill: { flex: 1 },
  safe: { flex: 1, paddingHorizontal: tokens.spacing.md },
});
```

- [ ] **Step 4: Implementare `Title.js`**

```js
import { Text, StyleSheet } from 'react-native';
import { colors, tokens, fonts } from '../theme';

export default function Title({ children, size = tokens.fontSize.title }) {
  return <Text style={[styles.t, { fontSize: size }]}>{children}</Text>;
}
const styles = StyleSheet.create({
  t: {
    color: colors.gold, fontFamily: fonts.title, letterSpacing: 2, textAlign: 'center',
    textShadowColor: colors.gold, textShadowRadius: 16, textShadowOffset: { width: 0, height: 0 },
  },
});
```

- [ ] **Step 5: Implementare `Panel.js`**

```js
import { View, StyleSheet } from 'react-native';
import { colors, tokens } from '../theme';

export default function Panel({ children, style }) {
  return <View style={[styles.p, style]}>{children}</View>;
}
const styles = StyleSheet.create({
  p: {
    backgroundColor: 'rgba(0,0,0,0.55)', borderWidth: 1, borderColor: colors.goldDim,
    borderRadius: tokens.radius.md, padding: tokens.spacing.md,
  },
});
```

- [ ] **Step 6: Implementare `Button.js`** (variant `primary` | `secondary`)

```js
import { Pressable, Text, StyleSheet } from 'react-native';
import { colors, tokens, fonts } from '../theme';

export default function Button({ label, onPress, variant = 'secondary', style }) {
  const primary = variant === 'primary';
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.base,
        primary ? styles.primary : styles.secondary,
        pressed && styles.pressed,
        style,
      ]}
    >
      <Text style={[styles.label, { fontSize: primary ? tokens.fontSize.lg : tokens.fontSize.md }]}>
        {label}
      </Text>
    </Pressable>
  );
}
const styles = StyleSheet.create({
  base: {
    borderRadius: tokens.radius.md, paddingVertical: tokens.spacing.md,
    paddingHorizontal: tokens.spacing.lg, alignItems: 'center', borderWidth: 1.5,
  },
  primary: {
    borderColor: colors.gold, backgroundColor: 'rgba(201,168,76,0.15)',
    shadowColor: colors.gold, shadowOpacity: 0.6, shadowRadius: 12, shadowOffset: { width: 0, height: 0 },
  },
  secondary: { borderColor: colors.goldDim, backgroundColor: 'rgba(0,0,0,0.5)' },
  pressed: { opacity: 0.7, transform: [{ scale: 0.98 }] },
  label: { color: colors.gold, fontFamily: fonts.ui, letterSpacing: 1.5, fontWeight: 'bold' },
});
```

- [ ] **Step 7: Eseguire — deve passare**

Run: `cd app && npm test -- base`
Expected: PASS (4 test).

- [ ] **Step 8: Commit**

```bash
git add app/src/components/ScreenBg.js app/src/components/Title.js app/src/components/Panel.js app/src/components/Button.js app/src/components/__tests__/base.test.js
git commit -m "feat: componenti base ScreenBg/Title/Panel/Button"
```

---

## Task 5: Componenti controllo — Toggle, Slider, OptionStepper

**Files:**
- Create: `app/src/components/Toggle.js`
- Create: `app/src/components/Slider.js`
- Create: `app/src/components/OptionStepper.js`
- Test: `app/src/components/__tests__/controls.test.js`

- [ ] **Step 1: Scrivere i test (devono fallire)** `app/src/components/__tests__/controls.test.js`

```js
import { render, screen, fireEvent } from '@testing-library/react-native';
import Toggle from '../Toggle';
import OptionStepper from '../OptionStepper';

test('Toggle chiama onChange col valore invertito', () => {
  const onChange = jest.fn();
  render(<Toggle label="Animazioni" value={false} onChange={onChange} />);
  fireEvent.press(screen.getByTestId('toggle-Animazioni'));
  expect(onChange).toHaveBeenCalledWith(true);
});

test('OptionStepper avanza al valore successivo', () => {
  const onChange = jest.fn();
  render(<OptionStepper label="Qualità" options={['Bassa', 'Media', 'Alta']} value="Media" onChange={onChange} />);
  fireEvent.press(screen.getByTestId('stepper-next-Qualità'));
  expect(onChange).toHaveBeenCalledWith('Alta');
});

test('OptionStepper non supera l ultimo valore', () => {
  const onChange = jest.fn();
  render(<OptionStepper label="Qualità" options={['Bassa', 'Media', 'Alta']} value="Alta" onChange={onChange} />);
  fireEvent.press(screen.getByTestId('stepper-next-Qualità'));
  expect(onChange).toHaveBeenCalledWith('Alta');
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- controls`
Expected: FAIL (moduli non trovati).

- [ ] **Step 3: Implementare `Toggle.js`**

```js
import { Pressable, View, Text, StyleSheet } from 'react-native';
import { colors, tokens, fonts } from '../theme';

export default function Toggle({ label, value, onChange }) {
  return (
    <View style={styles.row}>
      <Text style={styles.label}>{label}</Text>
      <Pressable
        testID={`toggle-${label}`}
        onPress={() => onChange(!value)}
        style={[styles.track, value ? styles.on : styles.off]}
      >
        <View style={[styles.knob, value ? styles.knobOn : styles.knobOff]} />
      </Pressable>
    </View>
  );
}
const styles = StyleSheet.create({
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: tokens.spacing.sm },
  label: { color: colors.text, fontFamily: fonts.ui, fontSize: tokens.fontSize.md },
  track: { width: 46, height: 26, borderRadius: 13, borderWidth: 1, justifyContent: 'center', padding: 2 },
  on: { borderColor: colors.gold, backgroundColor: 'rgba(201,168,76,0.3)' },
  off: { borderColor: colors.goldDim, backgroundColor: 'rgba(0,0,0,0.5)' },
  knob: { width: 20, height: 20, borderRadius: 10, backgroundColor: colors.gold },
  knobOn: { alignSelf: 'flex-end' },
  knobOff: { alignSelf: 'flex-start', backgroundColor: colors.goldDim },
});
```

- [ ] **Step 4: Implementare `OptionStepper.js`** (selettore ‹ valore ›)

```js
import { Pressable, View, Text, StyleSheet } from 'react-native';
import { colors, tokens, fonts } from '../theme';

export default function OptionStepper({ label, options, value, onChange }) {
  const i = Math.max(0, options.indexOf(value));
  const go = (delta) => {
    const next = Math.min(options.length - 1, Math.max(0, i + delta));
    onChange(options[next]);
  };
  return (
    <View style={styles.row}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.ctrl}>
        <Pressable testID={`stepper-prev-${label}`} onPress={() => go(-1)}><Text style={styles.arrow}>‹</Text></Pressable>
        <Text style={styles.value}>{value}</Text>
        <Pressable testID={`stepper-next-${label}`} onPress={() => go(1)}><Text style={styles.arrow}>›</Text></Pressable>
      </View>
    </View>
  );
}
const styles = StyleSheet.create({
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: tokens.spacing.sm },
  label: { color: colors.text, fontFamily: fonts.ui, fontSize: tokens.fontSize.md },
  ctrl: { flexDirection: 'row', alignItems: 'center', gap: tokens.spacing.md },
  arrow: { color: colors.gold, fontSize: tokens.fontSize.lg, paddingHorizontal: 6 },
  value: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.md, minWidth: 70, textAlign: 'center' },
});
```

- [ ] **Step 5: Implementare `Slider.js`** (0–100, no test automatico: gesture; verifica visiva)

```js
import { View, Text, StyleSheet, PanResponder } from 'react-native';
import { useRef, useState } from 'react';
import { colors, tokens, fonts } from '../theme';

export default function Slider({ label, value, onChange }) {
  const [w, setW] = useState(1);
  const pan = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderMove: (_, g) => {
        const pct = Math.round(Math.min(100, Math.max(0, (g.moveX / w) * 100)));
        onChange(pct);
      },
    })
  ).current;
  return (
    <View style={styles.wrap}>
      <View style={styles.head}>
        <Text style={styles.label}>{label}</Text>
        <Text style={styles.pct}>{value}%</Text>
      </View>
      <View style={styles.track} onLayout={(e) => setW(e.nativeEvent.layout.width)} {...pan.panHandlers}>
        <View style={[styles.fill, { width: `${value}%` }]} />
      </View>
    </View>
  );
}
const styles = StyleSheet.create({
  wrap: { paddingVertical: tokens.spacing.sm },
  head: { flexDirection: 'row', justifyContent: 'space-between' },
  label: { color: colors.text, fontFamily: fonts.ui, fontSize: tokens.fontSize.md },
  pct: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.sm },
  track: { height: 8, borderRadius: 4, backgroundColor: 'rgba(0,0,0,0.7)', marginTop: 6, overflow: 'hidden', borderWidth: 1, borderColor: colors.goldDim },
  fill: { height: '100%', backgroundColor: colors.gold },
});
```

- [ ] **Step 6: Eseguire — deve passare**

Run: `cd app && npm test -- controls`
Expected: PASS (3 test).

- [ ] **Step 7: Commit**

```bash
git add app/src/components/Toggle.js app/src/components/Slider.js app/src/components/OptionStepper.js app/src/components/__tests__/controls.test.js
git commit -m "feat: controlli Toggle/Slider/OptionStepper"
```

---

## Task 6: i18n (IT/EN)

**Files:**
- Create: `app/src/i18n/strings.js`
- Create: `app/src/i18n/index.js`
- Test: `app/src/i18n/__tests__/i18n.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/src/i18n/__tests__/i18n.test.js`

```js
import { translate } from '../index';

test('restituisce la stringa nella lingua richiesta', () => {
  expect(translate('it', 'home.play')).toBe('Gioca');
  expect(translate('en', 'home.play')).toBe('Play');
});

test('fallback alla chiave se mancante', () => {
  expect(translate('it', 'chiave.inesistente')).toBe('chiave.inesistente');
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- i18n`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementare `strings.js`**

```js
export const strings = {
  it: {
    'home.subtitle': 'dark fantasy tcg',
    'home.play': 'Gioca',
    'home.collection': 'Collezione & Mazzi',
    'home.shop': 'Negozio & Battle Pass',
    'home.settings': 'Impostazioni & Profilo',
    'common.back': 'Indietro',
    'common.comingSoon': 'In arrivo',
    'opt.title': 'Impostazioni',
    'opt.audio': 'Audio', 'opt.music': 'Musica', 'opt.sfx': 'Effetti',
    'opt.graphics': 'Grafica', 'opt.cardAnim': 'Animazioni carte', 'opt.reduceMotion': 'Riduci movimento', 'opt.quality': 'Qualità effetti',
    'opt.language': 'Lingua', 'opt.account': 'Account', 'opt.login': 'Accedi / Logout', 'opt.notifications': 'Notifiche',
    'opt.legal': 'Legale', 'opt.version': 'Versione',
  },
  en: {
    'home.subtitle': 'dark fantasy tcg',
    'home.play': 'Play',
    'home.collection': 'Collection & Decks',
    'home.shop': 'Shop & Battle Pass',
    'home.settings': 'Settings & Profile',
    'common.back': 'Back',
    'common.comingSoon': 'Coming soon',
    'opt.title': 'Settings',
    'opt.audio': 'Audio', 'opt.music': 'Music', 'opt.sfx': 'Effects',
    'opt.graphics': 'Graphics', 'opt.cardAnim': 'Card animations', 'opt.reduceMotion': 'Reduce motion', 'opt.quality': 'Effects quality',
    'opt.language': 'Language', 'opt.account': 'Account', 'opt.login': 'Log in / Log out', 'opt.notifications': 'Notifications',
    'opt.legal': 'Legal', 'opt.version': 'Version',
  },
};
```

- [ ] **Step 4: Implementare `index.js`** (translate puro + hook che legge la lingua dalle impostazioni)

```js
import { strings } from './strings';
import { useSettings } from '../state/settings';

export function translate(lang, key) {
  return strings[lang]?.[key] ?? strings.it?.[key] ?? key;
}

export function useT() {
  const { settings } = useSettings();
  const lang = settings.language;
  return (key) => translate(lang, key);
}
```

- [ ] **Step 5: Eseguire — deve passare**

Run: `cd app && npm test -- i18n`
Expected: PASS (2 test). (Il test importa solo `translate`, puro — nessuna dipendenza dal provider.)

- [ ] **Step 6: Commit**

```bash
git add app/src/i18n/
git commit -m "feat: i18n IT/EN con translate puro + hook useT"
```

---

## Task 7: Stato impostazioni + persistenza (AsyncStorage)

**Files:**
- Create: `app/src/state/settings.js`
- Test: `app/src/state/__tests__/settings.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/src/state/__tests__/settings.test.js`

```js
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Text, Pressable } from 'react-native';
import { SettingsProvider, useSettings, DEFAULT_SETTINGS, STORAGE_KEY } from '../settings';

function Probe() {
  const { settings, update } = useSettings();
  return (
    <>
      <Text testID="music">{settings.music}</Text>
      <Text testID="lang">{settings.language}</Text>
      <Pressable testID="set" onPress={() => update({ music: 30, language: 'en' })}><Text>set</Text></Pressable>
    </>
  );
}

beforeEach(() => AsyncStorage.clear());

test('parte dai default', async () => {
  render(<SettingsProvider><Probe /></SettingsProvider>);
  await waitFor(() => expect(screen.getByTestId('music')).toHaveTextContent(String(DEFAULT_SETTINGS.music)));
});

test('update salva su AsyncStorage', async () => {
  render(<SettingsProvider><Probe /></SettingsProvider>);
  fireEvent.press(screen.getByTestId('set'));
  await waitFor(() => expect(screen.getByTestId('lang')).toHaveTextContent('en'));
  const raw = await AsyncStorage.getItem(STORAGE_KEY);
  expect(JSON.parse(raw).music).toBe(30);
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- settings`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementare `settings.js`**

```js
import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';

export const STORAGE_KEY = '@gioco/settings';
export const DEFAULT_SETTINGS = {
  music: 70, sfx: 85,
  cardAnimations: true, reduceMotion: false, quality: 'Alta',
  language: 'it', notifications: true,
};

const SettingsContext = createContext(null);

export function SettingsProvider({ children }) {
  const [settings, setSettings] = useState(DEFAULT_SETTINGS);

  useEffect(() => {
    AsyncStorage.getItem(STORAGE_KEY).then((raw) => {
      if (raw) setSettings({ ...DEFAULT_SETTINGS, ...JSON.parse(raw) });
    });
  }, []);

  const update = (patch) => {
    setSettings((prev) => {
      const next = { ...prev, ...patch };
      AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      return next;
    });
  };

  return <SettingsContext.Provider value={{ settings, update }}>{children}</SettingsContext.Provider>;
}

export function useSettings() {
  const ctx = useContext(SettingsContext);
  if (!ctx) throw new Error('useSettings deve stare dentro SettingsProvider');
  return ctx;
}
```

- [ ] **Step 4: Eseguire — deve passare**

Run: `cd app && npm test -- settings`
Expected: PASS (2 test).

- [ ] **Step 5: Montare il provider nel root layout** — modificare `app/app/_layout.js` per avvolgere lo Stack

```js
import { Stack } from 'expo-router';
import { useFonts } from 'expo-font';
import { View } from 'react-native';
import { colors } from '../src/theme';
import { SettingsProvider } from '../src/state/settings';

export default function RootLayout() {
  const [loaded] = useFonts({
    'Cinzel-Regular': require('../assets/fonts/Cinzel-Regular.ttf'),
    'Cinzel-Bold': require('../assets/fonts/Cinzel-Bold.ttf'),
    'CrimsonText-Regular': require('../assets/fonts/CrimsonText-Regular.ttf'),
    'CrimsonText-Italic': require('../assets/fonts/CrimsonText-Italic.ttf'),
  });
  if (!loaded) return <View style={{ flex: 1, backgroundColor: colors.bgTop }} />;
  return (
    <SettingsProvider>
      <Stack screenOptions={{ headerShown: false }} />
    </SettingsProvider>
  );
}
```

- [ ] **Step 6: Commit**

```bash
git add app/src/state/ app/app/_layout.js
git commit -m "feat: stato impostazioni con persistenza AsyncStorage"
```

---

## Task 8: Schermi placeholder "In arrivo"

**Files:**
- Create: `app/src/components/ComingSoon.js`
- Create: `app/app/gioca.js`
- Create: `app/app/collezione.js`
- Create: `app/app/negozio.js`
- Test: `app/src/components/__tests__/comingsoon.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/src/components/__tests__/comingsoon.test.js`

```js
import { render, screen } from '@testing-library/react-native';
import { SettingsProvider } from '../../state/settings';
import ComingSoon from '../ComingSoon';

test('ComingSoon mostra titolo e dicitura', () => {
  render(<SettingsProvider><ComingSoon title="Collezione" /></SettingsProvider>);
  expect(screen.getByText('Collezione')).toBeOnTheScreen();
  expect(screen.getByText('In arrivo')).toBeOnTheScreen();
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- comingsoon`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementare `ComingSoon.js`**

```js
import { View, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import ScreenBg from './ScreenBg';
import Title from './Title';
import Button from './Button';
import { useT } from '../i18n';
import { colors, tokens, fonts } from '../theme';

export default function ComingSoon({ title }) {
  const router = useRouter();
  const t = useT();
  return (
    <ScreenBg>
      <View style={styles.center}>
        <Title size={tokens.fontSize.xl}>{title}</Title>
        <Text style={styles.soon}>{t('common.comingSoon')}</Text>
        <Button label={t('common.back')} onPress={() => router.back()} style={{ marginTop: tokens.spacing.lg }} />
      </View>
    </ScreenBg>
  );
}
const styles = StyleSheet.create({
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: tokens.spacing.md },
  soon: { color: colors.goldDim, fontFamily: fonts.bodyItalic, fontSize: tokens.fontSize.lg },
});
```

- [ ] **Step 4: Implementare i tre schermi placeholder**

`app/app/gioca.js`

```js
import ComingSoon from '../src/components/ComingSoon';
export default function Gioca() { return <ComingSoon title="Gioca" />; }
```

`app/app/collezione.js`

```js
import ComingSoon from '../src/components/ComingSoon';
export default function Collezione() { return <ComingSoon title="Collezione" />; }
```

`app/app/negozio.js`

```js
import ComingSoon from '../src/components/ComingSoon';
export default function Negozio() { return <ComingSoon title="Negozio" />; }
```

- [ ] **Step 5: Eseguire — deve passare**

Run: `cd app && npm test -- comingsoon`
Expected: PASS (1 test).

- [ ] **Step 6: Commit**

```bash
git add app/src/components/ComingSoon.js app/src/components/__tests__/comingsoon.test.js app/app/gioca.js app/app/collezione.js app/app/negozio.js
git commit -m "feat: schermi placeholder In arrivo"
```

---

## Task 9: Schermo Home

**Files:**
- Modify: `app/app/index.js` (sostituire il temporaneo)
- Test: `app/app/__tests__/home.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/app/__tests__/home.test.js`

```js
import { render, screen } from '@testing-library/react-native';
import { SettingsProvider } from '../../src/state/settings';
import Home from '../index';

jest.mock('expo-router', () => ({ useRouter: () => ({ push: jest.fn(), back: jest.fn() }) }));

test('Home mostra titolo e voci di menu', () => {
  render(<SettingsProvider><Home /></SettingsProvider>);
  expect(screen.getByText('GIOCO')).toBeOnTheScreen();
  expect(screen.getByText('Gioca')).toBeOnTheScreen();
  expect(screen.getByText('Collezione & Mazzi')).toBeOnTheScreen();
  expect(screen.getByText('Negozio & Battle Pass')).toBeOnTheScreen();
  expect(screen.getByText('Impostazioni & Profilo')).toBeOnTheScreen();
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- home`
Expected: FAIL (Home è ancora lo schermo "boot ok").

- [ ] **Step 3: Implementare `app/app/index.js`**

```js
import { View, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import ScreenBg from '../src/components/ScreenBg';
import Title from '../src/components/Title';
import Button from '../src/components/Button';
import { useT } from '../src/i18n';
import { colors, tokens, fonts } from '../src/theme';

export default function Home() {
  const router = useRouter();
  const t = useT();
  return (
    <ScreenBg>
      <View style={styles.header}>
        <Title>GIOCO</Title>
        <Text style={styles.subtitle}>{t('home.subtitle')}</Text>
      </View>

      <View style={styles.menu}>
        <Button variant="primary" label={t('home.play')} onPress={() => router.push('/gioca')} />
        <View style={styles.row}>
          <Button label={t('home.collection')} onPress={() => router.push('/collezione')} style={styles.half} />
          <Button label={t('home.shop')} onPress={() => router.push('/negozio')} style={styles.half} />
        </View>
        <Button label={t('home.settings')} onPress={() => router.push('/opzioni')} />
      </View>

      <View style={styles.footer}>
        <Text style={styles.currency}>Frammenti 1.2k  ◆  Essenza 340  ◆  Stelle 12</Text>
        <Text style={styles.version}>v1.0.0</Text>
      </View>
    </ScreenBg>
  );
}
const styles = StyleSheet.create({
  header: { alignItems: 'center', marginTop: tokens.spacing.xl },
  subtitle: { color: colors.goldDim, fontFamily: fonts.bodyItalic, fontSize: tokens.fontSize.md, marginTop: tokens.spacing.xs, letterSpacing: 1 },
  menu: { flex: 1, justifyContent: 'center', gap: tokens.spacing.md },
  row: { flexDirection: 'row', gap: tokens.spacing.md },
  half: { flex: 1 },
  footer: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingBottom: tokens.spacing.md },
  currency: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.sm },
  version: { color: colors.goldDim, fontFamily: fonts.ui, fontSize: tokens.fontSize.sm },
});
```

- [ ] **Step 4: Eseguire — deve passare**

Run: `cd app && npm test -- home`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add app/app/index.js app/app/__tests__/home.test.js
git commit -m "feat: schermo Home (menu principale)"
```

---

## Task 10: Schermo Opzioni

**Files:**
- Create: `app/app/opzioni.js`
- Test: `app/app/__tests__/opzioni.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/app/__tests__/opzioni.test.js`

```js
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { SettingsProvider } from '../../src/state/settings';
import { STORAGE_KEY } from '../../src/state/settings';
import Opzioni from '../opzioni';

jest.mock('expo-router', () => ({ useRouter: () => ({ back: jest.fn() }) }));
beforeEach(() => AsyncStorage.clear());

test('mostra le sezioni impostazioni', () => {
  render(<SettingsProvider><Opzioni /></SettingsProvider>);
  expect(screen.getByText('Audio')).toBeOnTheScreen();
  expect(screen.getByText('Grafica')).toBeOnTheScreen();
  expect(screen.getByText('Lingua')).toBeOnTheScreen();
});

test('toggle Animazioni carte persiste su AsyncStorage', async () => {
  render(<SettingsProvider><Opzioni /></SettingsProvider>);
  fireEvent.press(screen.getByTestId('toggle-Animazioni carte'));
  await waitFor(async () => {
    const raw = await AsyncStorage.getItem(STORAGE_KEY);
    expect(JSON.parse(raw).cardAnimations).toBe(false);
  });
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- opzioni`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementare `app/app/opzioni.js`**

```js
import { ScrollView, View, Text, Pressable, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import ScreenBg from '../src/components/ScreenBg';
import Panel from '../src/components/Panel';
import Slider from '../src/components/Slider';
import Toggle from '../src/components/Toggle';
import OptionStepper from '../src/components/OptionStepper';
import { useSettings } from '../src/state/settings';
import { useT } from '../src/i18n';
import { colors, tokens, fonts } from '../src/theme';

export default function Opzioni() {
  const router = useRouter();
  const { settings, update } = useSettings();
  const t = useT();
  return (
    <ScreenBg>
      <View style={styles.header}>
        <Pressable onPress={() => router.back()}><Text style={styles.back}>‹ {t('common.back')}</Text></Pressable>
        <Text style={styles.title}>{t('opt.title')}</Text>
      </View>
      <ScrollView contentContainerStyle={{ gap: tokens.spacing.md, paddingBottom: tokens.spacing.xl }}>
        <Panel>
          <Text style={styles.section}>{t('opt.audio')}</Text>
          <Slider label={t('opt.music')} value={settings.music} onChange={(v) => update({ music: v })} />
          <Slider label={t('opt.sfx')} value={settings.sfx} onChange={(v) => update({ sfx: v })} />
        </Panel>
        <Panel>
          <Text style={styles.section}>{t('opt.graphics')}</Text>
          <Toggle label={t('opt.cardAnim')} value={settings.cardAnimations} onChange={(v) => update({ cardAnimations: v })} />
          <Toggle label={t('opt.reduceMotion')} value={settings.reduceMotion} onChange={(v) => update({ reduceMotion: v })} />
          <OptionStepper label={t('opt.quality')} options={['Bassa', 'Media', 'Alta']} value={settings.quality} onChange={(v) => update({ quality: v })} />
        </Panel>
        <Panel>
          <Text style={styles.section}>{t('opt.language')}</Text>
          <OptionStepper label={t('opt.language')} options={['it', 'en']} value={settings.language} onChange={(v) => update({ language: v })} />
        </Panel>
        <Panel>
          <Text style={styles.section}>{t('opt.account')}</Text>
          <Toggle label={t('opt.notifications')} value={settings.notifications} onChange={(v) => update({ notifications: v })} />
          <Text style={styles.link}>{t('opt.login')}</Text>
        </Panel>
        <Panel>
          <Text style={styles.section}>{t('opt.legal')}</Text>
          <Text style={styles.legal}>Privacy · Termini · Crediti</Text>
          <Text style={styles.legal}>{t('opt.version')} 1.0.0</Text>
        </Panel>
      </ScrollView>
    </ScreenBg>
  );
}
const styles = StyleSheet.create({
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingVertical: tokens.spacing.md },
  back: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.md },
  title: { color: colors.gold, fontFamily: fonts.title, fontSize: tokens.fontSize.lg, letterSpacing: 1.5 },
  section: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.sm, letterSpacing: 2, marginBottom: tokens.spacing.sm, textTransform: 'uppercase' },
  link: { color: colors.gold, fontFamily: fonts.ui, fontSize: tokens.fontSize.md, marginTop: tokens.spacing.sm },
  legal: { color: colors.goldDim, fontFamily: fonts.body, fontSize: tokens.fontSize.md },
});
```

- [ ] **Step 4: Eseguire — deve passare**

Run: `cd app && npm test -- opzioni`
Expected: PASS (2 test).

- [ ] **Step 5: Commit**

```bash
git add app/app/opzioni.js app/app/__tests__/opzioni.test.js
git commit -m "feat: schermo Opzioni con persistenza"
```

---

## Task 11: Animazioni (reanimated) gated da Riduci movimento

**Files:**
- Create: `app/src/components/GlowTitle.js` (wrapper animato di Title)
- Modify: `app/app/index.js` (usare GlowTitle)
- Test: `app/src/components/__tests__/glowtitle.test.js`

- [ ] **Step 1: Scrivere il test (deve fallire)** `app/src/components/__tests__/glowtitle.test.js`

```js
import { render, screen } from '@testing-library/react-native';
import { SettingsProvider } from '../../state/settings';
import GlowTitle from '../GlowTitle';

test('GlowTitle rende comunque il testo (anim on/off)', () => {
  render(<SettingsProvider><GlowTitle>GIOCO</GlowTitle></SettingsProvider>);
  expect(screen.getByText('GIOCO')).toBeOnTheScreen();
});
```

- [ ] **Step 2: Eseguire — deve fallire**

Run: `cd app && npm test -- glowtitle`
Expected: FAIL (modulo non trovato).

- [ ] **Step 3: Implementare `GlowTitle.js`** — glow pulsante, disattivato se `reduceMotion`

```js
import { useEffect } from 'react';
import Animated, { useSharedValue, useAnimatedStyle, withRepeat, withTiming, cancelAnimation } from 'react-native-reanimated';
import Title from './Title';
import { useSettings } from '../state/settings';

export default function GlowTitle({ children, size }) {
  const { settings } = useSettings();
  const glow = useSharedValue(0.5);

  useEffect(() => {
    if (settings.reduceMotion) {
      cancelAnimation(glow);
      glow.value = 0.7;
      return;
    }
    glow.value = withRepeat(withTiming(1, { duration: 1800 }), -1, true);
    return () => cancelAnimation(glow);
  }, [settings.reduceMotion]);

  const style = useAnimatedStyle(() => ({ opacity: glow.value }));

  return (
    <Animated.View style={style}>
      <Title size={size}>{children}</Title>
    </Animated.View>
  );
}
```

- [ ] **Step 4: Usare GlowTitle nella Home** — in `app/app/index.js` sostituire l'import e l'uso di `Title` nell'header con `GlowTitle` (lasciando invariato il resto)

```js
// import Title from '../src/components/Title';   ← rimuovere
import GlowTitle from '../src/components/GlowTitle';
// ...nell'header:
<GlowTitle>GIOCO</GlowTitle>
```

- [ ] **Step 5: Eseguire i test (glowtitle + home) — devono passare**

Run: `cd app && npm test -- glowtitle home`
Expected: PASS. (Il test Home cerca ancora il testo 'GIOCO', reso da GlowTitle.)

- [ ] **Step 6: Commit**

```bash
git add app/src/components/GlowTitle.js app/src/components/__tests__/glowtitle.test.js app/app/index.js
git commit -m "feat: glow animato titolo Home (gated da Riduci movimento)"
```

---

## Task 12: Verifica end-to-end su dispositivo + PR

**Files:** nessuno nuovo (verifica + integrazione).

- [ ] **Step 1: Eseguire l'intera suite di test**

Run: `cd app && npm test`
Expected: tutti i test PASS, zero fallimenti.

- [ ] **Step 2: Avviare l'app reale**

Run: `cd app && npx expo start`
Aprire su Expo Go (QR) o simulatore iOS.

- [ ] **Step 3: Checklist verifica manuale** (confermare ognuna)
  - Home: titolo "GIOCO" con glow, sottotitolo, bottone "Gioca" dominante, griglia Collezione/Negozio, bottone Impostazioni, footer valute + versione. Font = Cinzel (serif).
  - Tap "Gioca"/"Collezione"/"Negozio" → schermo "In arrivo" → "Indietro" torna alla Home.
  - Tap "Impostazioni" → schermo Opzioni con tutte le sezioni (Audio, Grafica, Lingua, Account, Legale).
  - Muovere slider Musica/Effetti → percentuale cambia.
  - Toggle Animazioni carte / Riduci movimento → stato cambia; con Riduci movimento ON il glow del titolo si ferma.
  - Cambiare Lingua da `it` a `en` → i testi della Home/Opzioni passano in inglese.
  - Chiudere e riaprire l'app → impostazioni e lingua sono mantenute (persistenza).

- [ ] **Step 4: Push del branch**

```bash
cd ~/Desktop/"Gioco Alessandro"/gioco
git push -u origin feature/fondamenta-menu
```

- [ ] **Step 5: Aprire la Pull Request**

```bash
gh pr create --title "Fondamenta + Menu: design system, Home, Opzioni" \
  --body "Primo slice del lato app/client: expo-router, design system (theme + componenti), schermo Home, schermo Opzioni con persistenza, i18n IT/EN, animazioni gated. Tocca solo app/ e docs/ — nessun conflitto col lavoro carte. Vedi docs/superpowers/specs/2026-06-10-fondamenta-menu-design.md."
```

- [ ] **Step 6:** Comunicare al socio che `app/` ora ha la nuova struttura expo-router (vecchio `App.js` rimosso).

---

## Note finali

- **TDD pragmatico:** logica pura (i18n translate, settings store) e interazioni (toggle/stepper, render schermi) hanno test automatici; aspetto puramente visivo (gradienti, glow, slider gesture) verificato a mano su dispositivo (Task 12). Questo è coerente con un'app RN dove il rendering pixel non è unit-testabile in modo affidabile.
- **DRY:** ogni colore/spazio/font passa dai token; nessun valore hardcoded negli schermi.
- **YAGNI:** niente backend, audio reale, logica di gioco — solo ciò che serve a Home+Opzioni funzionanti.
- **Slice successivi** (piani separati): Collezione+DeckBuilder, Shop+Economia, Motore di gioco, Campo battaglia giocabile.
