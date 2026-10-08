using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Recompensas REALES al cerrarse una busqueda: renombre (actor.addRenown) y rasgos del Arsenal
    // (actor.addTrait). Es lo unico de las busquedas que ESCRIBE en el mundo: apagado por defecto
    // (busquedas_recompensas=0), con tope por hora real y registro en recompensas.log.
    // Firmas verificadas en el binario build 719: Actor.addRenown(int), Actor.addTrait(string, bool).
    internal static class Recompensas
    {
        static readonly Queue<float> hechas = new Queue<float>();

        public static void Aplica(Recompensa r, Busqueda b)
        {
            if (r == null || !Estado.Cfg.Bool("busquedas_recompensas")) return;
            try
            {
                float real = Time.unscaledTime;
                while (hechas.Count > 0 && real - hechas.Peek() > 3600f) hechas.Dequeue();
                if (hechas.Count >= Estado.Cfg.Ent("busquedas_recompensas_max_hora", 6, 1, 60)) { Log("tope por hora: nada para " + r.Nombre); return; }
                Actor a = Busquedas.ActorDe(r.Id);
                if (a == null || !a.isAlive()) return;
                hechas.Enqueue(real);
                var hecho = new List<string>();
                if (r.Renombre > 0) { a.addRenown(r.Renombre); hecho.Add("+" + r.Renombre + " renombre"); }
                if (r.Rasgo.Length > 0 && a.addTrait(r.Rasgo, false)) hecho.Add("rasgo " + r.Rasgo);
                if (hecho.Count == 0) return;
                Log(r.Nombre + " (" + r.Id + "): " + string.Join(", ", hecho.ToArray()) + " — " + r.Motivo + " [" + b.Titulo + "]");
                if (Estado.Cfg.Bool("busquedas_avisos")) Estado.Aviso(r.Nombre + " " + r.Motivo + ": " + string.Join(", ", hecho.ToArray()), 4f);
            }
            catch (Exception e) { Estado.Fallo("recompensa", e); }
        }

        static void Log(string linea)
        {
            try { File.AppendAllText(Path.Combine(Estado.Dir, "recompensas.log"), DateTime.Now.ToString("s") + " " + linea + "\n", new UTF8Encoding(false)); }
            catch (Exception e) { Estado.Fallo("recompensas_log", e); }
        }
    }
}
