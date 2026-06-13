using System.IO;
using System.Linq;
using Engine.Core;
using Engine.Data;
using Xunit;

namespace Engine.Tests
{
    // E3b — loader: carte.json -> DefCarta con Costo, Produzione, Effetti (AST E3).
    public class CarteDbEffettiTests
    {
        [Fact]
        public void Costo_stringaParsataInManaCosto()
        {
            string json = """
            [ { "id": "C", "tipo": "Creatura", "costo": "2 Est + 1" } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            ManaCosto? costo = m["C"].Costo;
            Assert.NotNull(costo);
            Assert.Equal(2, costo!.Est);
            Assert.Equal(1, costo.Generico);
        }

        [Fact]
        public void SenzaCostoNeEffetti_restaDefCartaMinima()
        {
            string json = """[ { "id": "S", "tipo": "Santuario" } ]""";
            var m = CarteDb.CaricaCarte(json);
            Assert.Equal(new DefCarta("S", "Santuario"), m["S"]); // Costo/Effetti null
        }

        [Fact]
        public void Effetto_etbPesca_mappato()
        {
            string json = """
            [ { "id": "P", "tipo": "Creatura", "effetti": [
                { "trigger": "etb", "azioni": [ { "verbo": "pesca", "valore": 2 } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var eff = m["P"].Effetti;
            Assert.NotNull(eff);
            var e = Assert.Single(eff!);
            Assert.Equal(Trigger.Etb, e.Trigger);
            var az = Assert.Single(e.Azioni);
            Assert.Equal(new Pesca(2), az);
        }

        [Fact]
        public void Effetto_generaMana_mappato()
        {
            string json = """
            [ { "id": "G", "tipo": "Artefatto", "effetti": [
                { "trigger": "attivata", "azioni": [ { "verbo": "genera_mana", "valore": 1, "colore": "Centro" } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var az = m["G"].Effetti!.Single().Azioni.Single();
            Assert.Equal(new GeneraMana(1, "Centro"), az);
        }

        [Fact]
        public void Verbo_avamposto_diventaProduzione_nonEffetto()
        {
            string json = """
            [ { "id": "A", "tipo": "Avamposto", "effetti": [
                { "trigger": "passiva", "azioni": [
                    { "verbo": "avamposto", "archetipo": "bounce",
                      "mana": { "quantita": 1, "colori": ["nord", "centro"], "scelta": true } } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            DefCarta a = m["A"];
            Assert.NotNull(a.Produzione);
            Assert.Equal(1, a.Produzione!.Quantita);
            Assert.Equal(new[] { "nord", "centro" }, a.Produzione.Colori);
            Assert.True(a.Produzione.Scelta);
            Assert.Null(a.Effetti); // l'avamposto non lascia effetti residui
        }

        [Fact]
        public void Target_mappatoInBersaglio()
        {
            string json = """
            [ { "id": "D", "tipo": "Magia", "effetti": [
                { "trigger": "etb", "azioni": [
                    { "verbo": "distruggi",
                      "target": { "tipo": "creatura", "proprietario": "AVVERSARIO", "quantificatore": "tutte" } } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var az = Assert.IsType<Distruggi>(m["D"].Effetti!.Single().Azioni.Single());
            Assert.Equal("creatura", az.Bersaglio.Tipo);
            Assert.Equal(Proprietario.Avversario, az.Bersaglio.Proprietario);
            Assert.Equal(Quantificatore.Tutte, az.Bersaglio.Quantificatore);
        }

        [Fact]
        public void InfliggiDanno_conTarget_mappato()
        {
            string json = """
            [ { "id": "F", "tipo": "Magia", "effetti": [
                { "trigger": "etb", "azioni": [
                    { "verbo": "infliggi_danno", "valore": 3,
                      "target": { "tipo": "giocatore", "proprietario": "AVVERSARIO", "quantificatore": "una" } } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var az = Assert.IsType<InfliggiDanno>(m["F"].Effetti!.Single().Azioni.Single());
            Assert.Equal(3, az.Valore);
            Assert.Equal(Proprietario.Avversario, az.Bersaglio.Proprietario);
            Assert.Equal(Quantificatore.Una, az.Bersaglio.Quantificatore);
        }

        [Fact]
        public void Mill_senzaTarget_defaultAvversario()
        {
            string json = """
            [ { "id": "M", "tipo": "Creatura", "effetti": [
                { "trigger": "upkeep", "azioni": [ { "verbo": "mill", "valore": 2 } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var az = Assert.IsType<Mill>(m["M"].Effetti!.Single().Azioni.Single());
            Assert.Equal(2, az.Valore);
            Assert.Equal(Proprietario.Avversario, az.Bersaglio.Proprietario);
        }

        [Fact]
        public void ModificaStat_mappato()
        {
            string json = """
            [ { "id": "X", "tipo": "Magia", "effetti": [
                { "trigger": "passiva", "azioni": [
                    { "verbo": "modifica_stat", "stat": "ATK", "valore": 1,
                      "target": { "tipo": "creatura", "proprietario": "TUTTI", "quantificatore": "tutte" } } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var az = Assert.IsType<ModificaStat>(m["X"].Effetti!.Single().Azioni.Single());
            Assert.Equal(1, az.Atk);
            Assert.Equal(0, az.Def);
        }

        [Fact]
        public void ModificaStatCombo_eConcediKeyword_mappati()
        {
            string json = """
            [ { "id": "Y", "tipo": "Magia", "effetti": [
                { "trigger": "passiva", "azioni": [
                    { "verbo": "modifica_stat_combo", "atk": -1, "def": -1,
                      "target": { "tipo": "creatura", "proprietario": "AVVERSARIO", "quantificatore": "tutte" } },
                    { "verbo": "concedi_keyword", "keyword": "arcano",
                      "target": { "tipo": "creatura", "proprietario": "TUE" } } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            var azioni = m["Y"].Effetti!.Single().Azioni;
            var combo = Assert.IsType<ModificaStat>(azioni[0]);
            Assert.Equal((-1, -1), (combo.Atk, combo.Def));
            var kw = Assert.IsType<ConcediKeyword>(azioni[1]);
            Assert.Equal("arcano", kw.Keyword);
        }

        [Fact]
        public void VerboNonSupportato_scartato()
        {
            // segnalino_stat non è ancora interpretabile -> azione scartata.
            string json = """
            [ { "id": "Z", "tipo": "Magia", "effetti": [
                { "trigger": "passiva", "azioni": [ { "verbo": "segnalino_stat", "atk": 1, "def": 1 } ] } ] } ]
            """;
            var m = CarteDb.CaricaCarte(json);
            Assert.Null(m["Z"].Effetti);
        }

        // Smoke sul dataset reale: carica dist-motore/carte.json e verifica che il
        // loader regga tutte le 317 carte senza eccezioni e popoli costi/produzioni/effetti.
        [Fact]
        public void DatasetReale_caricaSenzaErrori()
        {
            string? path = TrovaCarteJson();
            if (path == null) return; // ambiente senza repo: skip silenzioso

            var m = CarteDb.CaricaCarte(File.ReadAllText(path));
            Assert.True(m.Count >= 300, $"attese >=300 carte, trovate {m.Count}");
            Assert.True(m.Values.Any(d => d.Costo != null), "nessun costo parsato");
            Assert.True(m.Values.Any(d => d.Produzione != null), "nessuna produzione (avamposti)");
            Assert.True(m.Values.Any(d => d.Effetti != null), "nessun effetto parsato");
        }

        private static string? TrovaCarteJson()
        {
            var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null)
            {
                string cand = Path.Combine(dir.FullName, "dist-motore", "carte.json");
                if (File.Exists(cand)) return cand;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
