using Engine.Core;
using Engine.Data;
using Xunit;

namespace Engine.Tests
{
    public class CarteDbTests
    {
        [Fact]
        public void TrasformaJsonCarteInMappaDefCarta()
        {
            string json = """
            [
              { "id": "FORGIA", "tipo": "Santuario", "nome": "Forgia", "categoria": "CAT2" },
              { "id": "MORDETH", "tipo": "Creatura", "nome": "Mordeth", "categoria": "CAT2" }
            ]
            """;
            var m = CarteDb.CaricaCarte(json);
            Assert.Equal(2, m.Count);
            Assert.Equal(new DefCarta("FORGIA", "Santuario"), m["FORGIA"]);
            Assert.Equal("Creatura", m["MORDETH"].Tipo);
        }

        [Fact]
        public void IgnoraVociSenzaId()
        {
            string json = """[ { "tipo": "X" }, { "id": "OK", "tipo": "Creatura" } ]""";
            var m = CarteDb.CaricaCarte(json);
            Assert.Single(m);
            Assert.True(m.ContainsKey("OK"));
        }
    }
}
