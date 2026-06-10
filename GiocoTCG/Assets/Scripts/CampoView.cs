using System.Collections.Generic;
using UnityEngine;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

// Prima vista 3D: disegna la mano del giocatore 0 come carte sul tavolo,
// pilotata dallo stato dell'engine. Gira automaticamente al Play (niente da
// configurare in editor). Versione 0: carte = rettangoli colorati, niente
// arte/testo ancora. Serve a vedere il ponte engine -> grafica.
public static class CampoView
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run()
    {
        StatoPartita stato = CostruisciPartita();
        InquadraCamera();
        CreaTavolo();
        DisegnaMano(stato.Giocatori[0].Mano);

        var nomi = new List<string>();
        foreach (var c in stato.Giocatori[0].Mano) nomi.Add(c.DefId);
        Debug.Log($"[View] mano P0 ({nomi.Count}): {string.Join(", ", nomi)}");
    }

    // Crea un materiale URP del colore dato (URP/Lit usa _BaseColor, non _Color).
    private static Material MatUrp(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(sh != null ? sh : Shader.Find("Standard"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        return m;
    }

    private static StatoPartita CostruisciPartita()
    {
        string[] nomi = { "Drago", "Mago", "Golem", "Fata", "Lich", "Bestia", "Angelo", "Spettro" };
        var ids = new List<string>();
        var carte = new Dictionary<string, DefCarta>();
        foreach (var n in nomi)
        {
            ids.Add(n);
            carte[n] = new DefCarta(n, "Creatura", Costo: new ManaCosto { Generico = 2 }, Atk: 2, Def: 2);
        }
        var config = new ConfigPartita
        {
            HpIniziali = 30, ManoIniziale = 5, LimiteMano = 7,
            PrimoNonPescaT1 = true, PenalitaMazzoVuoto = PenalitaMazzoVuoto.PerditaImmediata,
        };
        var mazzi = new List<IReadOnlyList<string>> { new List<string>(ids), new List<string>(ids) };
        return GameEngine.Applica(null, new IniziaPartita(7, config, mazzi, carte)).Stato;
    }

    private static void InquadraCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0f, 2.4f, -3.4f);
        cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
    }

    private static void CreaTavolo()
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Plane);
        t.name = "Tavolo";
        t.transform.position = Vector3.zero;
        t.transform.localScale = new Vector3(2f, 1f, 2f);
        t.GetComponent<Renderer>().material = MatUrp(new Color(0.10f, 0.13f, 0.18f));
    }

    private static void DisegnaMano(IReadOnlyList<CartaIstanza> mano)
    {
        int n = mano.Count;
        float passo = 0.78f;
        float startX = -(n - 1) * passo / 2f;
        Color[] colori =
        {
            new Color(0.85f,0.25f,0.30f), new Color(0.95f,0.6f,0.2f),
            new Color(0.9f,0.85f,0.3f),  new Color(0.35f,0.75f,0.4f),
            new Color(0.3f,0.7f,0.85f),  new Color(0.4f,0.45f,0.85f),
            new Color(0.7f,0.4f,0.8f),   new Color(0.85f,0.85f,0.9f),
        };
        for (int i = 0; i < n; i++)
        {
            var carta = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carta.name = "Carta_" + mano[i].DefId;
            carta.transform.localScale = new Vector3(0.63f, 0.88f, 0.04f);
            float x = startX + i * passo;
            carta.transform.position = new Vector3(x, 0.55f, 0f);
            // leggera inclinazione + ventaglio (stile Snap)
            carta.transform.rotation = Quaternion.Euler(-14f, 0f, (n / 2f - i) * 4f);
            carta.GetComponent<Renderer>().material = MatUrp(colori[i % colori.Length]);
        }
    }
}
