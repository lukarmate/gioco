using System.Collections.Generic;
using System.Linq;
using Xunit;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

namespace Engine.Tests
{
    // E3c.3 — effetti passivi (statici continui): modifica_stat / concedi_keyword.
    // Le stat NON si mutano: si leggono via StatEffettive (base + modificatori passivi attivi).
    public class EffettiPassivaTests
    {
        private static DefCarta Permanente(string id, params Effetto[] eff)
            => new DefCarta(id, "Artefatto", Effetti: eff.ToList());

        private static DefCarta Creatura(string id, int atk, int def)
            => new DefCarta(id, "Creatura", Atk: atk, Def: def);

        private static Effetto Passiva(params AzioneEffetto[] az)
            => new Effetto(Trigger.Passiva, az.ToList());

        private static CartaIstanza InCampo(StatoPartita s, int g, string iid)
            => s.Giocatori[g].Campo.First(c => c.Iid == iid);

        [Fact]
        public void StatEffettive_senzaPassive_ugualiAllaBase()
        {
            var carte = new Dictionary<string, DefCarta> { ["SOLDATO"] = Creatura("SOLDATO", 2, 2) };
            var s = E2.Avvia(carte);
            string id;
            (s, id) = E2.MettiInCampo(s, 0, "SOLDATO");
            var (atk, def) = Effetti.StatEffettive(s, InCampo(s, 0, id));
            Assert.Equal((2, 2), (atk, def));
        }

        [Fact]
        public void Passiva_buffProprie_nonTocca_avversario()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["VESSILLO"] = Permanente("VESSILLO",
                    Passiva(new ModificaStat(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), 1, 1))),
                ["SOLDATO"] = Creatura("SOLDATO", 2, 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 0, "VESSILLO");
            string mio, suo;
            (s, mio) = E2.MettiInCampo(s, 0, "SOLDATO");
            (s, suo) = E2.MettiInCampo(s, 1, "SOLDATO");

            Assert.Equal((3, 3), Effetti.StatEffettive(s, InCampo(s, 0, mio)));
            Assert.Equal((2, 2), Effetti.StatEffettive(s, InCampo(s, 1, suo))); // avversario intatto
        }

        [Fact]
        public void Passiva_debuffAvversario()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["GIOGO"] = Permanente("GIOGO",
                    Passiva(new ModificaStat(new Bersaglio("creatura", Proprietario.Avversario, Quantificatore.Tutte), -1, 0))),
                ["SOLDATO"] = Creatura("SOLDATO", 2, 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 0, "GIOGO");
            string suo;
            (s, suo) = E2.MettiInCampo(s, 1, "SOLDATO");

            Assert.Equal((1, 2), Effetti.StatEffettive(s, InCampo(s, 1, suo)));
        }

        [Fact]
        public void Passiva_buffTutti_cumula()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["TOTEM_A"] = Permanente("TOTEM_A",
                    Passiva(new ModificaStat(new Bersaglio("creatura", Proprietario.Tutti, Quantificatore.Tutte), 0, 1))),
                ["TOTEM_B"] = Permanente("TOTEM_B",
                    Passiva(new ModificaStat(new Bersaglio("creatura", Proprietario.Tutti, Quantificatore.Tutte), 0, 1))),
                ["SOLDATO"] = Creatura("SOLDATO", 2, 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 0, "TOTEM_A");
            (s, _) = E2.MettiInCampo(s, 0, "TOTEM_B");
            string mio;
            (s, mio) = E2.MettiInCampo(s, 0, "SOLDATO");

            Assert.Equal((2, 4), Effetti.StatEffettive(s, InCampo(s, 0, mio))); // +1+1 def
        }

        [Fact]
        public void Combat_usaStatEffettive()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["VESSILLO"] = Permanente("VESSILLO",
                    Passiva(new ModificaStat(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), 1, 0))),
                ["SOLDATO"] = Creatura("SOLDATO", 2, 2), // diventa 3/2 col vessillo
                ["MURO"] = Creatura("MURO", 0, 2),
            };
            var s = E2.Avvia(carte);
            s = E2.FinoAMain1(s);
            (s, _) = E2.MettiInCampo(s, 0, "VESSILLO");
            string att, blk;
            (s, att) = E2.MettiInCampo(s, 0, "SOLDATO");
            (s, blk) = E2.MettiInCampo(s, 1, "MURO");
            s = GameEngine.Applica(s, new AvanzaFase()).Stato!; // Combat

            var r = GameEngine.Applica(s, new Attacca(att, blk));

            // SOLDATO effettivo 3 ATK vs MURO 2 DEF -> il muro muore; MURO ha 0 ATK -> SOLDATO sopravvive.
            Assert.True(r.Ok, r.Errore);
            Assert.Contains(r.Eventi, e => e is CreaturaDistrutta d && d.Iid == blk);
            Assert.DoesNotContain(r.Eventi, e => e is CreaturaDistrutta d && d.Iid == att);
        }

        [Fact]
        public void Passiva_concediKeyword()
        {
            var carte = new Dictionary<string, DefCarta>
            {
                ["STENDARDO"] = Permanente("STENDARDO",
                    Passiva(new ConcediKeyword(new Bersaglio("creatura", Proprietario.Tue, Quantificatore.Tutte), "velocita"))),
                ["SOLDATO"] = Creatura("SOLDATO", 2, 2),
            };
            var s = E2.Avvia(carte);
            (s, _) = E2.MettiInCampo(s, 0, "STENDARDO");
            string mio;
            (s, mio) = E2.MettiInCampo(s, 0, "SOLDATO");

            Assert.Contains("velocita", Effetti.KeywordEffettive(s, InCampo(s, 0, mio)));
        }
    }
}
