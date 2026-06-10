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

                string tipo = v.TryGetProperty("tipo", out JsonElement tEl) && tEl.ValueKind == JsonValueKind.String
                    ? (tEl.GetString() ?? "")
                    : "";
                m[id!] = new DefCarta(id!, tipo);
            }
            return m;
        }
    }
}
