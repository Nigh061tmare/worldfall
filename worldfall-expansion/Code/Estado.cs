using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Estado comun del plugin: carpeta propia, config.txt, avisos y registro de fallos.
    // Persistencia SOLO en LocalLow\mkarpenko\WorldBox\WorldfallExpansion\ (nunca en los saves del juego).
    internal static class Estado
    {
        public const string Version = "4.0.0";
        public static bool Activo;
        public static string Dir = "";
        public static Config Cfg = new Config();

        public static void Inicia()
        {
            Dir = Path.Combine(Application.persistentDataPath, "WorldfallExpansion");
            Directory.CreateDirectory(Dir);
            string f = Path.Combine(Dir, "config.txt");
            Cfg = Config.Desde(File.Exists(f) ? File.ReadAllLines(f, Encoding.UTF8) : null);
            // Se reescribe para que aparezcan las claves nuevas con su valor por defecto (respeta los tuyos).
            try { File.WriteAllText(f, Cfg.Serializa(), new UTF8Encoding(false)); }
            catch (Exception e) { Debug.LogWarning("[WorldfallExp] no puedo escribir config.txt: " + e.Message); }
            Activo = Cfg.Bool("activo");
        }

        // Texto breve arriba de la pantalla (se ve en modo dios y en primera persona).
        public static void Aviso(string texto, float segundos)
        {
            try { WorldTip.showNow(texto, false, "top", segundos); }
            catch (Exception e) { Fallo("aviso", e); }
        }

        static readonly Dictionary<string, int> fallos = new Dictionary<string, int>();

        // Un fallo nunca tumba el juego ni llena el log: 3 avisos por origen como mucho.
        public static void Fallo(string donde, Exception e)
        {
            int n; fallos.TryGetValue(donde, out n);
            fallos[donde] = ++n;
            if (n <= 3) Debug.LogWarning("[WorldfallExp] " + donde + ": " + e.GetType().Name + " " + e.Message + (n == 3 ? " (no se repetira)" : ""));
        }

        // Identificador de la partida cargada (mismo metodo que PeceraWB: por reflexion, sobrevive a cambios de tipo).
        public static string ClaveMundo()
        {
            try
            {
                var ms = Traverse.Create(World.world).Field("map_stats");
                string nombre = ms.Field("name").GetValue<string>() ?? "";
                object id = ms.Field("id").GetValue<object>();
                string k = (nombre + "_" + (id ?? "")).Trim('_');
                return k.Length > 0 ? k : "mundo";
            }
            catch (Exception) { return "mundo"; }
        }

        public static KeyCode Tecla(string s, KeyCode defecto)
        {
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), s, true); }
            catch (Exception) { return defecto; }
        }
    }
}
