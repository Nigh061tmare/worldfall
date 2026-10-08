using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Puente con PeceraWB SIN depender de su DLL: lee la cola de su cronica
    // (LocalLow\mkarpenko\WorldBox\PeceraWB\mundos\<mundo>\cronica.jsonl) y los hitos fuertes
    // (regicidio, destierro, sucesion disputada) abren busquedas. Si PeceraWB no esta instalado o
    // apagado, no hay fichero y no pasa nada. Solo lectura; nunca escribe en la carpeta de la pecera.
    internal static class PuentePecera
    {
        static string dirPecera = "", clave = "", ruta = "";
        static long offset = -1;
        static float tLee;
        static readonly StringBuilder resto = new StringBuilder();

        public static void Inicia()
        {
            if (!Estado.Cfg.Bool("pecera_busquedas")) return;
            dirPecera = Path.Combine(Path.GetDirectoryName(Estado.Dir), "PeceraWB");
            Debug.Log("[WorldfallExp] puente pecera: " + (Directory.Exists(dirPecera) ? "PeceraWB encontrada" : "sin PeceraWB (se ignora)"));
        }

        // Carpeta del mundo actual dentro de la de PeceraWB (mismo nombre que calcula la pecera).
        public static string CarpetaMundo()
        {
            if (dirPecera.Length == 0) dirPecera = Path.Combine(Path.GetDirectoryName(Estado.Dir), "PeceraWB");
            string seguro = Estado.ClaveMundo();
            foreach (char c in Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
            return Path.Combine(Path.Combine(dirPecera, "mundos"), seguro);
        }

        public static void Tick()
        {
            if (dirPecera.Length == 0 || !Busquedas.Activo || World.world == null) return;
            float real = Time.unscaledTime;
            if (real - tLee < 5f) return;
            tLee = real;

            string k = Estado.ClaveMundo();
            if (k != clave)
            {
                // Mundo nuevo: se empieza desde el final (la historia anterior no se convierte en busquedas).
                clave = k;
                string seguro = k;
                foreach (char c in Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
                ruta = Path.Combine(Path.Combine(Path.Combine(dirPecera, "mundos"), seguro), "cronica.jsonl");
                offset = File.Exists(ruta) ? new FileInfo(ruta).Length : 0;
                resto.Length = 0;
                return;
            }
            FotoMundo f = Busquedas.UltimaFoto;
            if (f == null || !File.Exists(ruta)) return;

            var cand = new List<Busqueda>();
            foreach (string l in LineasNuevas())
            {
                string tipo, texto;
                if (!LectorPecera.Lee(l, out tipo, out texto)) continue;
                Busqueda b = LectorPecera.Desde(tipo, texto, f);
                if (b != null) cand.Add(b);
            }
            Busquedas.Externas(cand);
        }

        static List<string> LineasNuevas()
        {
            var l = new List<string>();
            try
            {
                long largo = new FileInfo(ruta).Length;
                if (largo < offset) { offset = 0; resto.Length = 0; }   // la pecera roto el fichero
                if (largo == offset) return l;
                if (largo - offset > 1024 * 1024) offset = largo - 1024 * 1024;   // no leer megas de golpe
                using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    var buf = new byte[largo - offset];
                    int n = 0;
                    while (n < buf.Length) { int r = fs.Read(buf, n, buf.Length - n); if (r <= 0) break; n += r; }
                    offset += n;
                    resto.Append(new UTF8Encoding(false).GetString(buf, 0, n));
                }
                // Solo lineas completas; la ultima a medias se guarda para la proxima lectura.
                string s = resto.ToString();
                int corte = s.LastIndexOf('\n');
                if (corte < 0) return l;
                foreach (string x in s.Substring(0, corte).Split('\n'))
                    if (x.Trim().Length > 0) l.Add(x.Trim());
                resto.Length = 0;
                resto.Append(s.Substring(corte + 1));
            }
            catch (Exception e) { Estado.Fallo("pecera_lectura", e); }
            return l;
        }
    }
}
