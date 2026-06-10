using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Engine.Core;

namespace Engine.Tests
{
    // Utility condivise dai test. I record sono immutabili → si modifica lo stato con `with`.
    public static class H
    {
        public static ConfigPartita Config() => new ConfigPartita
        {
            HpIniziali = 30,
            ManoIniziale = 2,
            LimiteMano = 7,
            PrimoNonPescaT1 = true,
            PenalitaMazzoVuoto = PenalitaMazzoVuoto.PerditaImmediata,
        };

        public static IReadOnlyDictionary<string, DefCarta> CarteFinte(IEnumerable<string> ids)
            => ids.ToDictionary(id => id, id => new DefCarta(id, "Creatura"));

        // Sostituisce il giocatore idx applicando una trasformazione (immutabile).
        public static StatoPartita ConGiocatore(StatoPartita s, int idx, Func<Giocatore, Giocatore> f)
            => s with { Giocatori = s.Giocatori.Select((g, i) => i == idx ? f(g) : g).ToList() };

        public static string Json<T>(T value)
            => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });

        public static StatoPartita Avvia(int seed, ConfigPartita config,
            IReadOnlyList<IReadOnlyList<string>> mazzi, IReadOnlyDictionary<string, DefCarta> carte)
        {
            var r = Engine.Core.Engine.Applica(null, new IniziaPartita(seed, config, mazzi, carte));
            if (!r.Ok) throw new Exception(r.Errore);
            return r.Stato!;
        }
    }
}
