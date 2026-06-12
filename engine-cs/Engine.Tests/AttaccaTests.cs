using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E4 (re-baseline v1) — attacco diretto stile Hearthstone + danno persistente.
    public class AttaccaTests
    {
        private static DefCarta Cr(string id, int atk, int def, params string[] keyword)
            => new DefCarta(id, "Creatura", Atk: atk, Def: def,
                Keyword: keyword.Length > 0 ? keyword.ToList() : null);

        // Avvia (giocatore 0 già in fase Azioni, può attaccare).
        private static StatoPartita InCombat(IReadOnlyDictionary<string, DefCarta> carte)
            => E2.Avvia(carte);

        private static CartaIstanza Carta(StatoPartita s, int g, string iid)
            => s.Giocatori[g].Campo.First(c => c.Iid == iid);

        [Fact]
        public void AttaccoAlGiocatore_riduceHp()
        {
            var carte = new Dictionary<string, DefCarta> { ["ORCO"] = Cr("ORCO", 3, 3) };
            var s = InCombat(carte);
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "ORCO");
            int hpPrima = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new Attacca(a)); // null = HP avversario

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpPrima - 3, r.Stato!.Giocatori[1].Hp);
            Assert.True(Carta(r.Stato, 0, a).Tappata); // attaccare tappa
            Assert.Single(r.Eventi.OfType<DannoGiocatore>());
        }

        [Fact]
        public void Trade_reciproco_entrambiMuoiono()
        {
            var carte = new Dictionary<string, DefCarta> { ["A"] = Cr("A", 2, 2), ["B"] = Cr("B", 2, 2) };
            var s = InCombat(carte);
            string a, b;
            (s, a) = E2.MettiInCampo(s, 0, "A");
            (s, b) = E2.MettiInCampo(s, 1, "B");

            var r = GameEngine.Applica(s, new Attacca(a, b));

            Assert.True(r.Ok, r.Errore);
            Assert.Empty(r.Stato!.Giocatori[0].Campo);
            Assert.Empty(r.Stato.Giocatori[1].Campo);
            Assert.Equal(2, r.Eventi.OfType<CreaturaDistrutta>().Count());
        }

        [Fact]
        public void Trade_attaccanteSopravvive()
        {
            var carte = new Dictionary<string, DefCarta> { ["FORTE"] = Cr("FORTE", 3, 3), ["DEBOLE"] = Cr("DEBOLE", 2, 2) };
            var s = InCombat(carte);
            string a, b;
            (s, a) = E2.MettiInCampo(s, 0, "FORTE");
            (s, b) = E2.MettiInCampo(s, 1, "DEBOLE");

            var r = GameEngine.Applica(s, new Attacca(a, b));

            Assert.True(r.Ok, r.Errore);
            // FORTE prende 2 (<3) sopravvive con Danno 2; DEBOLE prende 3 (>=2) muore.
            Assert.Single(r.Stato!.Giocatori[0].Campo);
            Assert.Equal(2, Carta(r.Stato, 0, a).Danno);
            Assert.Empty(r.Stato.Giocatori[1].Campo);
        }

        [Fact]
        public void DannoPersistente_dueAttacchiUccidono()
        {
            // MURO 0/4: due attaccanti 2/x lo uccidono accumulando danno (0 ATK => non li ferisce).
            var carte = new Dictionary<string, DefCarta> { ["A"] = Cr("A", 2, 5), ["MURO"] = Cr("MURO", 0, 4) };
            var s = InCombat(carte);
            string a1, a2, muro;
            (s, a1) = E2.MettiInCampo(s, 0, "A", iidSuffix: "1");
            (s, a2) = E2.MettiInCampo(s, 0, "A", iidSuffix: "2");
            (s, muro) = E2.MettiInCampo(s, 1, "MURO");

            s = GameEngine.Applica(s, new Attacca(a1, muro)).Stato!;
            Assert.Single(s.Giocatori[1].Campo); // ancora vivo (danno 2 < 4)
            Assert.Equal(2, Carta(s, 1, muro).Danno);

            var r = GameEngine.Applica(s, new Attacca(a2, muro));

            Assert.True(r.Ok, r.Errore);
            Assert.Empty(r.Stato!.Giocatori[1].Campo); // danno 4 >= 4 -> muore
        }

        [Fact]
        public void SummoningSickness_bloccaAttacco()
        {
            var carte = new Dictionary<string, DefCarta> { ["NUOVA"] = Cr("NUOVA", 2, 2) };
            var s = InCombat(carte);
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "NUOVA", sick: true);

            var r = GameEngine.Applica(s, new Attacca(a));

            Assert.False(r.Ok);
        }

        [Fact]
        public void Velocita_ignoraSummoningSickness()
        {
            var carte = new Dictionary<string, DefCarta> { ["SCATTO"] = Cr("SCATTO", 2, 2, "velocita") };
            var s = InCombat(carte);
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "SCATTO", sick: true);
            int hpPrima = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new Attacca(a));

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(hpPrima - 2, r.Stato!.Giocatori[1].Hp);
        }

        [Fact]
        public void Provocazione_obbligaIlBersaglio()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["ATT"] = Cr("ATT", 2, 2),
                ["GUARDIA"] = Cr("GUARDIA", 0, 4, "provocazione"),
                ["GREGARIO"] = Cr("GREGARIO", 1, 1),
            };
            var s = InCombat(carte);
            string a, guardia, gregario;
            (s, a) = E2.MettiInCampo(s, 0, "ATT");
            (s, guardia) = E2.MettiInCampo(s, 1, "GUARDIA");
            (s, gregario) = E2.MettiInCampo(s, 1, "GREGARIO");

            // Non può colpire gli HP né il gregario finché c'è la Provocazione.
            Assert.False(GameEngine.Applica(s, new Attacca(a)).Ok);
            Assert.False(GameEngine.Applica(s, new Attacca(a, gregario)).Ok);
            // Può colpire la guardia.
            Assert.True(GameEngine.Applica(s, new Attacca(a, guardia)).Ok);
        }

        [Fact]
        public void Travolta_eccessoVaAlGiocatore()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["BRUTO"] = Cr("BRUTO", 5, 5, "travolta"),
                ["MURO"] = Cr("MURO", 0, 2),
            };
            var s = InCombat(carte);
            string a, muro;
            (s, a) = E2.MettiInCampo(s, 0, "BRUTO");
            (s, muro) = E2.MettiInCampo(s, 1, "MURO");
            int hpPrima = s.Giocatori[1].Hp;

            var r = GameEngine.Applica(s, new Attacca(a, muro));

            Assert.True(r.Ok, r.Errore);
            Assert.Empty(r.Stato!.Giocatori[1].Campo);          // MURO muore (5 >= 2)
            Assert.Equal(hpPrima - 3, r.Stato.Giocatori[1].Hp); // eccesso 5-2 = 3 agli HP
        }

        [Fact]
        public void Tappata_nonAttacca()
        {
            var carte = new Dictionary<string, DefCarta> { ["ORCO"] = Cr("ORCO", 3, 3) };
            var s = InCombat(carte);
            string a;
            (s, a) = E2.MettiInCampo(s, 0, "ORCO", tappata: true);

            var r = GameEngine.Applica(s, new Attacca(a));

            Assert.False(r.Ok);
        }
    }
}
