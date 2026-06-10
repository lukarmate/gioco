using System.Collections.Generic;
using UnityEngine;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

// Prova di collegamento engine <-> Unity.
// Gira AUTOMATICAMENTE all'avvio del Play (nessun GameObject da configurare):
// fa partire una partita, avanza qualche fase e scrive gli eventi nella Console.
// Serve solo a verificare che Engine.Core.dll giri dentro Unity. Si cancella poi.
public static class EngineSmokeTest
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run()
    {
        // Carte finte: 8 creature di test per ogni mazzo.
        var ids = new List<string>();
        var carte = new Dictionary<string, DefCarta>();
        for (int i = 0; i < 8; i++)
        {
            string id = "C" + i;
            ids.Add(id);
            carte[id] = new DefCarta(id, "Creatura — Test");
        }

        var config = new ConfigPartita
        {
            HpIniziali = 30,
            ManoIniziale = 2,
            LimiteMano = 7,
            PrimoNonPescaT1 = true,
            PenalitaMazzoVuoto = PenalitaMazzoVuoto.PerditaImmediata,
        };

        var mazzi = new List<IReadOnlyList<string>> { new List<string>(ids), new List<string>(ids) };

        var r = GameEngine.Applica(null, new IniziaPartita(5, config, mazzi, carte));
        Debug.Log($"[Engine] partita iniziata: ok={r.Ok}, eventi={r.Eventi.Count}");

        var stato = r.Stato;
        for (int k = 0; k < 6; k++)
        {
            var rr = GameEngine.Applica(stato, new AvanzaFase());
            stato = rr.Stato;
            Debug.Log($"[Engine] avanza -> fase {stato.Fase} (turno {stato.NumeroTurno}, giocatore {stato.TurnoDi})");
        }

        Debug.Log($"[Engine] mano P0 = {stato.Giocatori[0].Mano.Count} carte. " +
                  "Engine C# gira dentro Unity ✅");
    }
}
