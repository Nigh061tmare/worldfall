using System;
using System.Collections.Generic;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Anade frases de recuerdos (Core/Frases.cs) a las claves del juego «happiness_dialog_<tipo>_<n>», que
    // Worldfall usa en sus charlas (Dialogue.GameLine). Solo anade claves nuevas tras las existentes: nunca
    // pisa las del juego. Si un cambio de idioma las borra, se vuelven a poner (cada 60 s se comprueba).
    // Worldfall cuenta las frases de cada tipo la primera vez que las usa: por eso se ponen al arrancar.
    internal static class FrasesPNJ
    {
        static readonly Dictionary<string, string> puestas = new Dictionary<string, string>();
        static float tRevisa = -100f;
        static bool listo;

        public static void Tick()
        {
            if (!Estado.Cfg.Bool("frases")) return;
            float real = Time.unscaledTime;
            if (real - tRevisa < (listo ? 60f : 2f)) return;
            tRevisa = real;
            try
            {
                if (LocalizedTextManager.current_language == null) return;
                if (listo && Intactas()) return;
                bool es = LocalizedTextManager.current_language.id == "es";
                puestas.Clear();
                int n = 0;
                foreach (string tipo in Frases.Tipos)
                    foreach (var kv in Frases.Plan(tipo, es, k => LocalizedTextManager.stringExists(k) && !puestas.ContainsKey(k)))
                    {
                        LocalizedTextManager.add(kv.Key, kv.Value, false, "", false);
                        puestas[kv.Key] = kv.Value;
                        n++;
                    }
                if (!listo) Debug.Log("[WorldfallExp] frases: " + n + " frases de recuerdos nuevas para los PNJ");
                listo = true;
            }
            catch (Exception e) { Estado.Fallo("frases", e); }
        }

        static bool Intactas()
        {
            foreach (var kv in puestas)
                return LocalizedTextManager.stringExists(kv.Key) && LocalizedTextManager.getText(kv.Key) == kv.Value;
            return true;
        }
    }
}
