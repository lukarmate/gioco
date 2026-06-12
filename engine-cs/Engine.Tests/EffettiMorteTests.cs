using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E3c.2a — trigger morte: quando una creatura muore (combat o verbo Distruggi)
    // scattano i suoi effetti morte. Controllore = proprietario della creatura morta.
    public class EffettiMorteTests
    {
        private static DefCarta Creatura(string id, int atk, int def, params Effetto[] eff)
            => new DefCarta(id, "Creatura", Atk: atk, Def: def, Effetti: eff.ToList());

        [Fact]
        public void Morte_daDistruggi_faScattareGliEffetti()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["STERMINIO"] = Creatura("STERMINIO", 1, 1,
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Distruggi(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte)),
                    })),
                ["RIANIMATO"] = Creatura("RIANIMATO", 2, 2,
                    new Effetto(Trigger.Morte, new AzioneEffetto[]
                    {
                        new GeneraToken("Spettro", 1, 1), // "tu" = proprietario del morto
                    })),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            (s, _) = E2.MettiInCampo(s, 1, "RIANIMATO"); // creatura avversaria con morte
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "STERMINIO");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            var g1 = r.Stato!.Giocatori[1];
            Assert.DoesNotContain(g1.Campo, c => c.DefId == "RIANIMATO"); // morto
            Assert.Contains(g1.Campo, c => c.DefId == "TOKEN_Spettro");   // morte -> token
            var tok = Assert.Single(r.Eventi.OfType<TokenGenerato>());
            Assert.Equal(1, tok.Giocatore); // il token va al proprietario del morto
            Assert.Contains(r.Eventi, e => e is CreaturaDistrutta);
        }

        [Fact]
        public void Morte_inCombat_faScattareGliEffetti()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["GUERRIERO"] = Creatura("GUERRIERO", 3, 3),                 // attaccante (sopravvive)
                ["MARTIRE"] = Creatura("MARTIRE", 2, 2,                      // bersaglio che muore
                    new Effetto(Trigger.Morte, new AzioneEffetto[] { new Pesca(1) })),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            string att, blk;
            (s, att) = E2.MettiInCampo(s, 0, "GUERRIERO");
            (s, blk) = E2.MettiInCampo(s, 1, "MARTIRE");
            s = GameEngine.Applica(s, new AvanzaFase()).Stato!; // Main1 -> Combat
            Assert.Equal(Fase.Combat, s.Fase);
            int mazzo1Prima = s.Giocatori[1].Mazzo.Count;

            var r = GameEngine.Applica(s, new Attacca(att, blk));

            Assert.True(r.Ok, r.Errore);
            // GUERRIERO 3 ATK vs MARTIRE 2 DEF -> MARTIRE muore -> morte: Pesca 1 per il giocatore 1.
            Assert.Contains(r.Eventi, e => e is CreaturaDistrutta d && d.Iid == blk);
            Assert.Equal(mazzo1Prima - 1, r.Stato!.Giocatori[1].Mazzo.Count);
            Assert.Single(r.Eventi.OfType<CartaPescata>(), p => p.Giocatore == 1);
        }

        [Fact]
        public void Morte_senzaEffetti_nessunExtra()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["BOIA"] = Creatura("BOIA", 1, 1,
                    new Effetto(Trigger.Etb, new AzioneEffetto[]
                    {
                        new Distruggi(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte)),
                    })),
                ["VANILLA"] = Creatura("VANILLA", 2, 2),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            (s, _) = E2.MettiInCampo(s, 1, "VANILLA");
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "BOIA");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid));

            Assert.True(r.Ok, r.Errore);
            Assert.Single(r.Eventi.OfType<CreaturaDistrutta>());
            Assert.Empty(r.Eventi.OfType<TokenGenerato>());
        }
    }
}
