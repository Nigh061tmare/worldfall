using System;
using System.Collections.Generic;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Exporta el estado social de los vivos a mundos\<mundo>\estado_social.jsonl para otros mods
    // (worldfall-expansion lo muestra en primera persona). Solo escribe en la carpeta de la pecera.
    // Una linea por personaje: {"id","n","casa","rasgos","amigo","rival","sueno","rumor"}.
    // Se calcula en UNA pasada por las relaciones (no una por personaje). Interruptor: exporta_social=1.
    internal static partial class Mundo
    {
        static float tExporta;
        const int MaxExporta = 3000;

        static void TickExporta()
        {
            if (!Cfg.Bool("exporta_social")) return;
            float real = Time.unscaledTime;
            if (real - tExporta < 10f) return;
            tExporta = real;
            try { Exporta(); }
            catch (Exception e) { Fallo("exporta", e); }
        }

        static void Exporta()
        {
            if (Disco == null || Vivos.Count == 0) return;
            // Mejor amigo y peor rival de cada uno, en una sola pasada.
            var amigo = new Dictionary<string, KeyValuePair<string, double>>();
            var rival = new Dictionary<string, KeyValuePair<string, double>>();
            foreach (var kv in Afectos.Todos())
            {
                string x, y; ModeloAfectivo.Separa(kv.Key, out x, out y);
                if (x == y) continue;
                double s = Afectos.Sentimiento(x, y);
                KeyValuePair<string, double> cur;
                if (s > 0.05 && (!amigo.TryGetValue(x, out cur) || s > cur.Value)) amigo[x] = new KeyValuePair<string, double>(y, s);
                if (s < -0.05 && (!rival.TryGetValue(x, out cur) || s < cur.Value)) rival[x] = new KeyValuePair<string, double>(y, s);
            }
            // Ultimo rumor sobre cada uno.
            var rumor = new Dictionary<string, string>();
            var fugas = Secretos.Fugas;
            for (int i = fugas.Count - 1; i >= 0 && rumor.Count < MaxExporta; i--)
                if (!rumor.ContainsKey(fugas[i].Sujeto)) rumor[fugas[i].Sujeto] = fugas[i].Texto;

            var lineas = new List<string>();
            foreach (var kv in Vivos)
            {
                if (lineas.Count >= MaxExporta) break;
                Actor a = kv.Value;
                Ficha f;
                if (a == null || !Fichas.TryGetValue(kv.Key, out f)) continue;
                bool vivo;
                try { vivo = a.isAlive(); } catch (Exception) { vivo = false; }
                if (!vivo) continue;
                Faccion fac; facDe.TryGetValue(kv.Key, out fac);
                KeyValuePair<string, double> am, ri;
                string sueno = "";
                Sueno sn = Suenos.De(f);
                if (sn != null) sueno = sn.Texto + (sn.Cumplido ? " (cumplido)" : " (" + (int)(sn.Progreso * 100) + " %)");
                string rum; rumor.TryGetValue(kv.Key, out rum);
                lineas.Add("{\"id\":\"" + Json.Escape(kv.Key) + "\",\"n\":\"" + Json.Escape(f.Nombre)
                    + "\",\"casa\":\"" + Json.Escape(fac != null ? fac.Nombre : "")
                    + "\",\"rasgos\":\"" + Json.Escape(string.Join(", ", f.Rasgos.ToArray()))
                    + "\",\"amigo\":\"" + Json.Escape(amigo.TryGetValue(kv.Key, out am) ? NombreDe(am.Key) : "")
                    + "\",\"rival\":\"" + Json.Escape(rival.TryGetValue(kv.Key, out ri) ? NombreDe(ri.Key) : "")
                    + "\",\"sueno\":\"" + Json.Escape(sueno)
                    + "\",\"rumor\":\"" + Json.Escape(rum ?? "") + "\"}");
            }
            Disco.Rewrite("estado_social.jsonl", lineas);
        }
    }
}
