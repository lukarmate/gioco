using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // Rifinitura: infliggi_danno a creatura + morte state-based dopo ogni azione.
    public class DannoCreaturaTests
    {
        [Fact]
        public void InfliggiDannoCreatura_accumulaEUccide()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["BOMBA"] = new DefCarta("BOMBA", "Creatura", Atk: 1, Def: 1,
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[]
                        {
                            new InfliggiDanno(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte), 2),
                        }),
                    }),
                ["DEBOLE"] = new DefCarta("DEBOLE", "Creatura", Atk: 1, Def: 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 1, "DEBOLE"); // creatura avversaria 1/2
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "BOMBA");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid)); // etb: 2 danni a tutte le creature avversarie

            Assert.True(r.Ok, r.Errore);
            Assert.Empty(r.Stato!.Giocatori[1].Campo); // DEBOLE (def 2) prende 2 -> muore
            Assert.Contains(r.Eventi, e => e is CreaturaDistrutta);
        }

        [Fact]
        public void InfliggiDannoCreatura_nonLetale_sopravviveColDanno()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["PUNGOLO"] = new DefCarta("PUNGOLO", "Creatura", Atk: 1, Def: 1,
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Etb, new AzioneEffetto[]
                        {
                            new InfliggiDanno(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte), 1),
                        }),
                    }),
                ["ORSO"] = new DefCarta("ORSO", "Creatura", Atk: 2, Def: 3),
            };
            var s = E2.Avvia(carte);
            string orso;
            (s, orso) = E2.MettiInCampo(s, 1, "ORSO"); // 2/3
            string iid;
            (s, iid) = E2.MettiInMano(s, 0, "PUNGOLO");

            var r = GameEngine.Applica(s, new GiocaCreatura(iid)); // 1 danno

            Assert.True(r.Ok, r.Errore);
            var c = r.Stato!.Giocatori[1].Campo.First(x => x.Iid == orso);
            Assert.Equal(1, c.Danno); // sopravvive con 1 danno (def 3)
        }

        [Fact]
        public void StateBased_debuffPassivoAZeroDef_uccide()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["MALEDIZIONE"] = new DefCarta("MALEDIZIONE", "Artefatto",
                    Effetti: new List<Effetto>
                    {
                        new Effetto(Trigger.Passiva, new AzioneEffetto[]
                        {
                            new ModificaStat(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte), 0, -2),
                        }),
                    }),
                ["FANTE"] = new DefCarta("FANTE", "Creatura", Atk: 1, Def: 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 1, "FANTE"); // avversario: 1/2
            string iid;
            (s, iid) = E2.MettiInCampo(s, 0, "MALEDIZIONE"); // mette in campo, ma serve un'azione per il check
            // gioca una carta qualsiasi del g0 per scatenare lo state-based globale
            string mid;
            (s, mid) = E2.MettiInMano(s, 0, "FANTE");
            var r = GameEngine.Applica(s, new GiocaCreatura(mid));

            // FANTE avversario: DEF effettiva 2-2 = 0 -> muore allo state-based
            Assert.True(r.Ok, r.Errore);
            Assert.DoesNotContain(r.Stato!.Giocatori[1].Campo, c => c.DefId == "FANTE");
        }
    }
}
