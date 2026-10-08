using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pecera.Core;
using UnityEngine;

namespace PeceraWB
{
    // Lee las intervenciones divinas de worldfall-expansion (sus poderes de dios) y las mete en la
    // simulacion: cronica, memoria y afectos. Solo LEE el fichero del otro mod; sin el, no pasa nada.
    // Interruptor: intervenciones=1.
    internal static partial class Mundo
    {
        static string rutaIntervenciones = "", claveIntervenciones = "";
        static long offsetIntervenciones = -1;
        static float tIntervenciones;

        static void TickIntervenciones()
        {
            if (!Cfg.Bool("intervenciones")) return;
            float real = Time.unscaledTime;
            if (real - tIntervenciones < 5f) return;
            tIntervenciones = real;
            try
            {
                if (claveIntervenciones != Clave)
                {
                    claveIntervenciones = Clave;
                    string seguro = Clave;
                    foreach (char c in Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
                    rutaIntervenciones = Path.Combine(Path.Combine(Path.Combine(Path.GetDirectoryName(Dir), "WorldfallExpansion"), "mundos"), seguro);
                    rutaIntervenciones = Path.Combine(rutaIntervenciones, "intervenciones.jsonl");
                    // Mundo nuevo: lo anterior ya paso; se empieza desde el final.
                    offsetIntervenciones = File.Exists(rutaIntervenciones) ? new FileInfo(rutaIntervenciones).Length : 0;
                    return;
                }
                if (!File.Exists(rutaIntervenciones)) return;
                long largo = new FileInfo(rutaIntervenciones).Length;
                if (largo < offsetIntervenciones) offsetIntervenciones = 0;
                if (largo == offsetIntervenciones) return;
                string nuevo;
                using (var fs = new FileStream(rutaIntervenciones, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    fs.Seek(offsetIntervenciones, SeekOrigin.Begin);
                    var buf = new byte[largo - offsetIntervenciones];
                    int n = 0;
                    while (n < buf.Length) { int r = fs.Read(buf, n, buf.Length - n); if (r <= 0) break; n += r; }
                    nuevo = new UTF8Encoding(false).GetString(buf, 0, n);
                }
                int corte = nuevo.LastIndexOf('\n');
                if (corte < 0) return;
                offsetIntervenciones += new UTF8Encoding(false).GetByteCount(nuevo.Substring(0, corte + 1));
                foreach (string l in nuevo.Substring(0, corte).Split('\n'))
                {
                    Intervencion iv = Intervencion.Lee(l.Trim());
                    if (iv != null) Aplica(iv);
                }
            }
            catch (Exception e) { Fallo("intervenciones", e); }
        }

        static void Aplica(Intervencion iv)
        {
            string[] h = iv.Hito();
            if (h == null) return;
            Hito(h[0], h[1], double.Parse(h[2], System.Globalization.CultureInfo.InvariantCulture));
            Nombres[iv.Id] = iv.Nombre;
            switch (iv.Tipo)
            {
                case "wfx_bendicion":
                    Memoria.Registra(iv.Id, "divino", iv.Id, "fue bendecido por el Obelisco", 1.0);
                    Cultura.Suceso("honor", 0.5); break;
                case "wfx_maldicion":
                    Memoria.Registra(iv.Id, "divino", iv.Id, "fue maldito por los dioses", 1.0);
                    Cultura.Suceso("venganza", 0.5); break;
                case "wfx_juramento":
                    Memoria.Registra(iv.Id, "juramento", iv.Id, "juro lealtad a su reino", 0.8);
                    Cultura.Suceso("honor", 0.5); break;
                case "wfx_destierro":
                    Memoria.Registra(iv.Id, "destierro", iv.Id, "fue desterrado por los dioses", 1.0); break;
                case "wfx_discordia":
                {
                    string amigo = Extremo(iv.Id, true);
                    if (amigo == null) break;
                    Afectos.Evento(iv.Id, amigo, TipoEvento.Agravio, 0.8);
                    Afectos.Evento(amigo, iv.Id, TipoEvento.Traicion, 0.6);
                    Hito("discordia", iv.Nombre + " y " + NombreDe(amigo) + " ya no se hablan", 4);
                    Memoria.Registra(iv.Id, "discordia", amigo, "se peleo con " + NombreDe(amigo), 0.9);
                    break;
                }
                case "wfx_reconciliacion":
                {
                    string rival = Extremo(iv.Id, false);
                    if (rival == null) break;
                    Afectos.Evento(iv.Id, rival, TipoEvento.Perdon, 0.8);
                    Afectos.Evento(rival, iv.Id, TipoEvento.Perdon, 0.6);
                    Hito("reconciliacion", iv.Nombre + " y " + NombreDe(rival) + " hacen las paces", 3);
                    Cultura.Suceso("clemencia", 1);
                    break;
                }
            }
        }

        // Mejor amigo (true) o peor rival (false) de un personaje segun el modelo afectivo.
        static string Extremo(string id, bool amigo)
        {
            string mejor = null; double v = amigo ? 0.05 : -0.05;
            foreach (var kv in Afectos.Todos())
            {
                string x, y; ModeloAfectivo.Separa(kv.Key, out x, out y);
                if (x != id || y == id) continue;
                double s = Afectos.Sentimiento(x, y);
                if (amigo ? s > v : s < v) { v = s; mejor = y; }
            }
            return mejor;
        }
    }
}
