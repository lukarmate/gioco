using System.Collections.Generic;
using System.Linq;
using Engine.Core;
using Xunit;

namespace Engine.Tests
{
    public class UntapE2Tests
    {
        private static IReadOnlyDictionary<string, DefCarta> Carte() => new Dictionary<string, DefCarta>
        {
            ["BESTIA"] = new DefCarta("BESTIA", "Creatura — Bestia",
                Costo: new ManaCosto { Est = 1 }, Atk: 2, Def: 2),
        };

        [Fact]
        public void UntapAzzeraSummoningSicknessDelGiocatoreAttivo()
        {
            var s = E2.Avvia(Carte());
            // creatura entrata questo turno nel campo del giocatore attivo (0)
            var carta = new CartaIstanza { Iid = "x", DefId = "BESTIA", Proprietario = 0, EntrataQuestoTurno = true };
            s = H.ConGiocatore(s, 0, g => g with { Campo = g.Campo.Concat(new[] { carta }).ToList() });

            var r = Fasi.EseguiEntrataFase(s, Fase.Untap);
            Assert.False(r.Stato.Giocatori[0].Campo.First(c => c.Iid == "x").EntrataQuestoTurno);
        }

        [Fact]
        public void UntapResettaFlagAvampostoGiocato()
        {
            var s = E2.Avvia(Carte());
            s = H.ConGiocatore(s, 0, g => g with { AvampostoGiocatoQuestoTurno = true });
            var r = Fasi.EseguiEntrataFase(s, Fase.Untap);
            Assert.False(r.Stato.Giocatori[0].AvampostoGiocatoQuestoTurno);
        }
    }
}
