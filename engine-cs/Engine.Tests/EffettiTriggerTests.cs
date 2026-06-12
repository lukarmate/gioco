using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E3c.1 — altri trigger (upkeep, attacco) + verbo genera_token.
    public class EffettiTriggerTests
    {
        private static DefCarta Creatura(string id, params Effetto[] eff)
            => new DefCarta(id, "Creatura", Atk: 2, Def: 2, Effetti: eff.ToList());

        [Fact]
        public void Upkeep_esegueEffettiDeiPermanentiInCampo()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["SANGUISUGA"] = Creatura("SANGUISUGA",
                    new Effetto(Trigger.Upkeep, new AzioneEffetto[]
                    {
                        new Mill(new Bersaglio("giocatore", Proprietario.Avversario), 1),
                    })),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 0, "SANGUISUGA");
            int mazzoOppPrima = s.Giocatori[1].Mazzo.Count;

            // Fase iniziale = Untap; un AvanzaFase entra in Upkeep e fa scattare gli effetti.
            var r = GameEngine.Applica(s, new AvanzaFase());

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(Fase.Upkeep, r.Stato!.Fase);
            Assert.Equal(mazzoOppPrima - 1, r.Stato.Giocatori[1].Mazzo.Count);
            Assert.Single(r.Eventi.OfType<CartaMacinata>());
        }

        [Fact]
        public void Attacco_esegueEffettoDellAttaccante()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["PREDONE"] = Creatura("PREDONE",
                    new Effetto(Trigger.Attacco, new AzioneEffetto[] { new Pesca(1) })),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInCampo(s, 0, "PREDONE"); // niente summoning sickness (default)
            s = GameEngine.Applica(s, new AvanzaFase()).Stato!; // Main1 -> Combat
            Assert.Equal(Fase.Combat, s.Fase);
            int mazzoPrima = s.Giocatori[0].Mazzo.Count;

            var r = GameEngine.Applica(s, new Attacca(iid)); // bersaglio null = HP avversario

            Assert.True(r.Ok, r.Errore);
            Assert.Equal(mazzoPrima - 1, r.Stato!.Giocatori[0].Mazzo.Count);
            Assert.Single(r.Eventi.OfType<CreaturaAttacca>());
            Assert.Single(r.Eventi.OfType<CartaPescata>());
        }

        [Fact]
        public void GeneraToken_etb_creaTokenInCampoConDef()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["NECROMANTE"] = Creatura("NECROMANTE",
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new GeneraToken("Non-Morto", 1, 1),
                    })),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "NECROMANTE");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            var g = r.Stato!.Giocatori[0];
            Assert.Contains(g.Campo, c => c.DefId == "TOKEN_Non-Morto");
            Assert.True(r.Stato.Carte.ContainsKey("TOKEN_Non-Morto"));
            Assert.Equal(1, r.Stato.Carte["TOKEN_Non-Morto"].Atk);
            Assert.Equal(1, r.Stato.Carte["TOKEN_Non-Morto"].Def);
            var ev = Assert.Single(r.Eventi.OfType<TokenGenerato>());
            Assert.Equal("Non-Morto", ev.Nome);
            Assert.Equal(0, ev.Giocatore);
        }

        [Fact]
        public void GeneraToken_eSummoningSick()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["NECROMANTE"] = Creatura("NECROMANTE",
                    new Effetto(Trigger.Etb, new AzioneEffetto[] { new GeneraToken("Non-Morto", 1, 1) })),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "NECROMANTE");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            var token = r.Stato!.Giocatori[0].Campo.First(c => c.DefId == "TOKEN_Non-Morto");
            Assert.True(token.EntrataQuestoTurno);
        }
    }
}
