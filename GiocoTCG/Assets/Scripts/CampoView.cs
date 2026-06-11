using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Engine.Core;
using GameEngine = Engine.Core.Engine;

// Prima vista 3D: disegna la mano del giocatore 0 come carte sul tavolo,
// pilotata dallo stato dell'engine. Gira automaticamente al Play (niente da
// configurare in editor). Carte = rettangoli colorati con testo (nome, costo,
// ATK/DEF) via TextMeshPro 3D. Serve a vedere il ponte engine -> grafica.
// NB: se il testo non appare, importa i font TMP da
//   Window > TextMeshPro > Import TMP Essential Resources.
public static class CampoView
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run()
    {
        StatoPartita stato = CostruisciPartita();
        InquadraCamera();
        CreaTavolo();
        DisegnaMano(stato, stato.Giocatori[0].Mano);

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

    private static readonly Vector3 ScalaCarta = new Vector3(0.63f, 0.88f, 0.04f);

    private static void DisegnaMano(StatoPartita stato, IReadOnlyList<CartaIstanza> mano)
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
            carta.transform.localScale = ScalaCarta;
            float x = startX + i * passo;
            carta.transform.position = new Vector3(x, 0.55f, 0f);
            // leggera inclinazione + ventaglio (stile Snap)
            carta.transform.rotation = Quaternion.Euler(-14f, 0f, (n / 2f - i) * 4f);
            carta.GetComponent<Renderer>().material = MatUrp(colori[i % colori.Length]);

            stato.Carte.TryGetValue(mano[i].DefId, out DefCarta def);
            ScriviTesto(carta.transform, mano[i].DefId, def);
        }
    }

    // Aggiunge nome + costo + ATK/DEF sulla faccia della carta (verso la camera).
    private static void ScriviTesto(Transform carta, string nome, DefCarta def)
    {
        // Costo = totale del ManaCosto (versione 0: numero singolo).
        if (def?.Costo != null)
            AggiungiEtichetta(carta, def.Costo.Totale.ToString(), 0.40f, 0.30f, TextAlignmentOptions.Top);

        AggiungiEtichetta(carta, nome, 0.10f, 0.26f, TextAlignmentOptions.Center);

        if (def?.Atk != null || def?.Def != null)
            AggiungiEtichetta(carta, $"{def.Atk ?? 0}/{def.Def ?? 0}", -0.40f, 0.30f, TextAlignmentOptions.Bottom);
    }

    // Etichetta TMP 3D figlia della carta. yLocale = posizione verticale sulla
    // faccia (la carta va da -0.5 a 0.5). Counter-scale per annullare la scala
    // non uniforme del parent; rot 180Y per affacciarsi alla camera (lato -Z).
    private static void AggiungiEtichetta(Transform carta, string testo, float yLocale, float dim, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt_" + testo);
        go.transform.SetParent(carta, false);
        go.transform.localPosition = new Vector3(0f, yLocale, -0.6f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = new Vector3(1f / ScalaCarta.x, 1f / ScalaCarta.y, 1f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = testo;
        tmp.fontSize = dim;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.rectTransform.sizeDelta = new Vector2(0.95f, 0.5f);
    }
}
