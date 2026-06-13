using System.Collections.Generic;
using System.Text.Json;
using Engine.Core;

namespace Engine.Data
{
    // Bridge parser -> engine. Isolato in un assembly separato perché dipende da
    // System.Text.Json: così Engine.Core resta senza dipendenze esterne e importabile in Unity.
    public static class CarteDb
    {
        // Trasforma il testo JSON di dist-motore/carte.json in Dictionary<defId, DefCarta>.
        // Popola: Tipo, Costo (parse stringa), Produzione (dal verbo "avamposto"),
        // Effetti (AST E3 per i verbi supportati). ATK/DEF non sono nel JSON (gap parser).
        public static IReadOnlyDictionary<string, DefCarta> CaricaCarte(string jsonText)
        {
            var m = new Dictionary<string, DefCarta>();
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return m;

            foreach (JsonElement v in doc.RootElement.EnumerateArray())
            {
                if (!v.TryGetProperty("id", out JsonElement idEl) || idEl.ValueKind != JsonValueKind.String)
                    continue;
                string? id = idEl.GetString();
                if (string.IsNullOrEmpty(id)) continue; // come TS: `if (!v.id) continue`

                string tipo = StrOpt(v, "tipo") ?? "";

                ManaCosto? costo = null;
                string? costoStr = StrOpt(v, "costo");
                if (!string.IsNullOrWhiteSpace(costoStr)) costo = ManaCosto.Parse(costoStr!);

                ManaProdotto? produzione = null;
                var effetti = new List<Effetto>();
                if (v.TryGetProperty("effetti", out JsonElement effEl) && effEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement e in effEl.EnumerateArray())
                        LeggiEffetto(e, effetti, ref produzione, tipo);
                }

                m[id!] = new DefCarta(
                    id!, tipo,
                    Costo: costo,
                    Produzione: produzione,
                    Effetti: effetti.Count > 0 ? effetti : null);
            }
            return m;
        }

        private static void LeggiEffetto(JsonElement e, List<Effetto> effetti, ref ManaProdotto? produzione, string tipo)
        {
            if (!TryTrigger(StrOpt(e, "trigger"), out Trigger trigger)) return;
            if (!e.TryGetProperty("azioni", out JsonElement azEl) || azEl.ValueKind != JsonValueKind.Array) return;

            var azioni = new List<AzioneEffetto>();
            foreach (JsonElement a in azEl.EnumerateArray())
            {
                string? verbo = StrOpt(a, "verbo");
                if (verbo == "avamposto")
                {
                    produzione = LeggiProduzione(a) ?? produzione;
                    continue; // la produzione non è un effetto residuo
                }
                AzioneEffetto? az = LeggiAzione(verbo, a, tipo);
                if (az != null) azioni.Add(az); // verbi non supportati (E3c) -> scartati
            }

            if (azioni.Count > 0) effetti.Add(new Effetto(trigger, azioni));
        }

        // Verbi supportati dall'executor E3a. Gli altri tornano null (scartati).
        private static AzioneEffetto? LeggiAzione(string? verbo, JsonElement a, string tipo)
        {
            // Su una Magia (carta non-permanente) i verbi stat sono ONE-SHOT persistenti (segnalini),
            // non aure continue: la magia va al cimitero, ma il bonus resta sulla creatura.
            bool eMagia = tipo.IndexOf("Magia", System.StringComparison.OrdinalIgnoreCase) >= 0;
            switch (verbo)
            {
                case "pesca":
                    return new Pesca(IntOpt(a, "valore"));
                case "genera_mana":
                    return new GeneraMana(IntOpt(a, "valore"), StrOpt(a, "colore") ?? "generico");
                case "infliggi_danno":
                    return new InfliggiDanno(LeggiBersaglio(a, "giocatore", Proprietario.Avversario), IntOpt(a, "valore"));
                case "distruggi":
                    return new Distruggi(LeggiBersaglio(a, "creatura", Proprietario.Avversario));
                case "mill":
                    // il parser non cattura il target del mill: convenzione = avversario.
                    return new Mill(LeggiBersaglio(a, "giocatore", Proprietario.Avversario), IntOpt(a, "valore"));
                case "genera_token":
                    return LeggiToken(a);
                case "modifica_stat":
                {
                    string? stat = StrOpt(a, "stat");
                    int v = IntOpt(a, "valore");
                    bool atkStat = string.Equals(stat, "ATK", System.StringComparison.OrdinalIgnoreCase);
                    var bers = LeggiBersaglio(a, "creatura", Proprietario.Tutti);
                    int atk = atkStat ? v : 0, def = atkStat ? 0 : v;
                    return eMagia ? new ApplicaStat(bers, atk, def) : new ModificaStat(bers, atk, def);
                }
                case "modifica_stat_combo":
                {
                    var bers = LeggiBersaglio(a, "creatura", Proprietario.Tutti);
                    int atk = IntOpt(a, "atk"), def = IntOpt(a, "def");
                    return eMagia ? new ApplicaStat(bers, atk, def) : new ModificaStat(bers, atk, def);
                }
                case "applica_stat":
                case "segnalino_stat":
                    return new ApplicaStat(LeggiBersaglio(a, "creatura", Proprietario.Tutti),
                        IntOpt(a, "atk"), IntOpt(a, "def"));
                case "concedi_keyword":
                    return new ConcediKeyword(LeggiBersaglio(a, "creatura", Proprietario.Tutti),
                        StrOpt(a, "keyword") ?? "");
                default:
                    return null;
            }
        }

        private static AzioneEffetto? LeggiToken(JsonElement a)
        {
            if (!a.TryGetProperty("token", out JsonElement t) || t.ValueKind != JsonValueKind.Object) return null;
            string nome = StrOpt(t, "nome") ?? "Token";
            string controllore = StrOpt(t, "controllore") ?? "tu";
            return new GeneraToken(nome, IntOpt(t, "atk"), IntOpt(t, "def"), controllore);
        }

        private static ManaProdotto? LeggiProduzione(JsonElement a)
        {
            if (!a.TryGetProperty("mana", out JsonElement mEl) || mEl.ValueKind != JsonValueKind.Object) return null;
            int quantita = IntOpt(mEl, "quantita");
            bool scelta = mEl.TryGetProperty("scelta", out JsonElement sEl) && sEl.ValueKind == JsonValueKind.True;
            var colori = new List<string>();
            if (mEl.TryGetProperty("colori", out JsonElement cEl) && cEl.ValueKind == JsonValueKind.Array)
                foreach (JsonElement c in cEl.EnumerateArray())
                    if (c.ValueKind == JsonValueKind.String) colori.Add(c.GetString()!);
            return new ManaProdotto(quantita, colori, scelta);
        }

        // Legge il target -> Bersaglio. Se assente, usa i default forniti (tipo/proprietario).
        private static Bersaglio LeggiBersaglio(JsonElement a, string tipoDefault, Proprietario propDefault)
        {
            if (!a.TryGetProperty("target", out JsonElement t) || t.ValueKind != JsonValueKind.Object)
                return new Bersaglio(tipoDefault, propDefault);

            string tipo = StrOpt(t, "tipo") ?? tipoDefault;
            Proprietario prop = ParseProprietario(StrOpt(t, "proprietario"), propDefault);
            Quantificatore quant = ParseQuantificatore(StrOpt(t, "quantificatore"));
            string? filtro = StrOpt(t, "filtro");
            return new Bersaglio(tipo, prop, quant, filtro);
        }

        private static bool TryTrigger(string? s, out Trigger trigger)
        {
            switch (s)
            {
                case "etb": trigger = Trigger.Etb; return true;
                case "morte": trigger = Trigger.Morte; return true;
                case "upkeep": trigger = Trigger.Upkeep; return true;
                case "attacco": trigger = Trigger.Attacco; return true;
                case "attivata": trigger = Trigger.Attivata; return true;
                case "passiva": trigger = Trigger.Passiva; return true;
                default: trigger = Trigger.Etb; return false;
            }
        }

        private static Proprietario ParseProprietario(string? s, Proprietario def)
        {
            switch (s)
            {
                case "TUE": return Proprietario.Tue;
                case "AVVERSARIO": return Proprietario.Avversario;
                case "TUTTI": return Proprietario.Tutti;
                default: return def;
            }
        }

        private static Quantificatore ParseQuantificatore(string? s)
        {
            switch (s)
            {
                case "una": return Quantificatore.Una;
                case "ogni": return Quantificatore.Ogni;
                case "tutte": return Quantificatore.Tutte;
                default: return Quantificatore.Tutte;
            }
        }

        private static string? StrOpt(JsonElement el, string nome)
            => el.TryGetProperty(nome, out JsonElement p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

        private static int IntOpt(JsonElement el, string nome)
            => el.TryGetProperty(nome, out JsonElement p) && p.ValueKind == JsonValueKind.Number
                ? p.GetInt32()
                : 0;
    }
}
