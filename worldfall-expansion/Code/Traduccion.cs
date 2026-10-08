using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Traduce al espanol los textos de Worldfall SIN tocar Worldfall.dll ni sus clases.
    //
    // Worldfall escribe sus textos en ingles y los pinta con el IMGUI de Unity. Este adaptador parchea
    // las cuatro puertas PUBLICAS de Unity por las que pasa todo ese texto (leido del binario de
    // Worldfall 0.9.2):
    //   GUI.Label(Rect, string, GUIStyle)   GUI.Button(Rect, string, GUIStyle)
    //   new GUIContent(string)              GUIContent.text = ...   (para medir el ancho)
    // y cambia el texto si esta en Traducciones/worldfall_es.txt. Lo que no esta, sale tal cual.
    // Son APIs de Unity, no de Worldfall: una actualizacion de Worldfall no las rompe; como mucho sus
    // textos nuevos saldran en ingles hasta anadirlos al fichero. Cada parche va por separado: si uno
    // no se instala, los demas siguen. WorldBox pinta su propia interfaz con uGUI, no la afecta.
    internal static class Traduccion
    {
        public static readonly Traductor T = new Traductor();
        static readonly object cerrojo = new object();
        // true mientras el propio plugin pinta (la brujula ya esta en espanol: ni traducir ni registrar).
        public static bool Saltar;
        static bool activo;
        static float tFlush;
        static string rutaFaltan = "";

        public static void Inicia()
        {
            if (!Estado.Cfg.Bool("traduccion")) { Debug.Log("[WorldfallExp] traduccion=0: Worldfall en ingles"); return; }
            string carpeta = CarpetaMod();
            int ficheros = 0;
            if (carpeta.Length > 0)
            {
                string dir = Path.Combine(carpeta, "Traducciones");
                if (Directory.Exists(dir))
                {
                    var fs = new List<string>(Directory.GetFiles(dir, "*_es.txt"));
                    fs.Sort(StringComparer.Ordinal);
                    foreach (string f in fs) { T.Carga(File.ReadAllLines(f, Encoding.UTF8)); ficheros++; }
                }
            }
            // Las tuyas mandan: LocalLow\...\WorldfallExpansion\traduccion_extra.txt (mismo formato).
            string extra = Path.Combine(Estado.Dir, "traduccion_extra.txt");
            if (File.Exists(extra)) { T.Carga(File.ReadAllLines(extra, Encoding.UTF8)); ficheros++; }
            else
            {
                try
                {
                    File.WriteAllText(extra, "# Tus traducciones (mandan sobre las del mod). Formato:\n"
                        + "#   Texto en ingles => Texto en espanol\n#   The army of {0} => El ejercito de {0}\n", new UTF8Encoding(false));
                }
                catch (Exception e) { Estado.Fallo("traduccion_extra", e); }
            }
            T.RegistrarFaltan = Estado.Cfg.Bool("traduccion_registro");
            rutaFaltan = Path.Combine(Estado.Dir, "traduccion_faltan.txt");

            int ok = 0;
            ok += Parchea("GUI.Label", typeof(GUI).GetMethod("Label", new[] { typeof(Rect), typeof(string), typeof(GUIStyle) }), "Arg1");
            ok += Parchea("GUI.Button", typeof(GUI).GetMethod("Button", new[] { typeof(Rect), typeof(string), typeof(GUIStyle) }), "Arg1");
            ok += Parchea("GUIContent()", typeof(GUIContent).GetConstructor(new[] { typeof(string) }), "Arg0");
            PropertyInfo texto = typeof(GUIContent).GetProperty("text");
            ok += Parchea("GUIContent.text", texto != null ? texto.GetSetMethod() : null, "Arg0");
            activo = ok > 0;
            Debug.Log("[WorldfallExp] traduccion: " + T.Exactas + " frases y " + T.Plantillas + " plantillas de " + ficheros
                + " fichero(s); " + ok + "/4 puertas de texto parcheadas" + (T.Errores > 0 ? "; " + T.Errores + " lineas ignoradas" : ""));
        }

        static int Parchea(string nombre, MethodBase m, string prefijo)
        {
            try
            {
                if (m == null) { Debug.LogWarning("[WorldfallExp] traduccion: no encuentro " + nombre); return 0; }
                var h = new Harmony("joseluis.wfexp.traduccion." + nombre);
                h.Patch(m, new HarmonyMethod(typeof(Traduccion).GetMethod(prefijo, BindingFlags.Static | BindingFlags.NonPublic)));
                return 1;
            }
            catch (Exception e) { Debug.LogWarning("[WorldfallExp] traduccion: " + nombre + " sin parche: " + e.Message); return 0; }
        }

        // Prefijos de Harmony: __0/__1 son el primer/segundo argumento (no depende del nombre del parametro).
        static void Arg0(ref string __0) { __0 = Seguro(__0); }
        static void Arg1(ref string __1) { __1 = Seguro(__1); }

        static string Seguro(string s)
        {
            if (string.IsNullOrEmpty(s) || Saltar) return s;
            try { lock (cerrojo) return T.Traduce(s); }
            catch (Exception e) { Estado.Fallo("traducir", e); return s; }
        }

        // Cada 30 s vuelca los textos que se vieron sin traducir (si traduccion_registro=1).
        public static void Tick()
        {
            if (!activo || !T.RegistrarFaltan) return;
            float real = Time.unscaledTime;
            if (real - tFlush < 30f) return;
            tFlush = real;
            try
            {
                List<string> l;
                lock (cerrojo) l = T.SacaFaltan();
                if (l.Count == 0) return;
                var sb = new StringBuilder();
                foreach (string s in l) sb.Append(Traductor.Escapa(s)).Append(" => \n");
                File.AppendAllText(rutaFaltan, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { Estado.Fallo("traduccion_faltan", e); }
        }

        static string CarpetaMod()
        {
            try
            {
                var d = Main.Instance != null ? Main.Instance.GetDeclaration() : null;
                return d != null && d.FolderPath != null ? d.FolderPath : "";
            }
            catch (Exception e) { Estado.Fallo("carpeta_mod", e); return ""; }
        }
    }
}
