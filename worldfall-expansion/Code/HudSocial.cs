using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // En primera persona: bajo la brujula, lo que PeceraWB sabe de la persona que tienes mas cerca
    // (casa, rasgos, amigo, rival, sueno y el ultimo rumor). Lee estado_social.jsonl, que escribe
    // PeceraWB cada 10 s (PeceraWB/Code/Exporta.cs). Sin PeceraWB, no muestra nada. Solo lectura.
    internal static class HudSocial
    {
        static Dictionary<string, EstadoSocial> estados = new Dictionary<string, EstadoSocial>();
        static DateTime leido = DateTime.MinValue;
        static string ruta = "";
        static float tLee = -100f, tCalc;
        static string texto = "";
        static GUIStyle estilo, sombra;

        public static void Tick()
        {
            if (!Estado.Cfg.Bool("social")) { texto = ""; return; }
            float real = Time.unscaledTime;
            if (real - tCalc < 0.5f) return;
            tCalc = real;
            try
            {
                Actor yo; float yaw;
                if (!PuenteWorldfall.PrimeraPersona(out yo, out yaw)) { texto = ""; return; }
                if (real - tLee > 5f) { tLee = real; Recarga(); }
                if (estados.Count == 0) { texto = ""; return; }
                Actor c = Busquedas.Cercano(yo.current_position, (float)Estado.Cfg.Num("social_radio", 6), yo);
                EstadoSocial e;
                texto = c != null && estados.TryGetValue(Busquedas.IdDe(c), out e) ? e.Texto() : "";
            }
            catch (Exception ex) { Estado.Fallo("social", ex); texto = ""; }
        }

        static void Recarga()
        {
            ruta = Path.Combine(PuentePecera.CarpetaMundo(), "estado_social.jsonl");
            if (!File.Exists(ruta)) { estados.Clear(); return; }
            DateTime m = File.GetLastWriteTimeUtc(ruta);
            if (m == leido) return;
            leido = m;
            var lineas = new List<string>();
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var r = new StreamReader(fs, Encoding.UTF8))
            {
                string l;
                while ((l = r.ReadLine()) != null) lineas.Add(l);
            }
            estados = EstadoSocial.LeeTodo(lineas);
        }

        public static void Dibuja()
        {
            if (texto.Length == 0) return;
            try
            {
                if (estilo == null)
                {
                    estilo = new GUIStyle(GUI.skin.label);
                    estilo.alignment = TextAnchor.UpperCenter;
                    estilo.normal.textColor = new Color(0.62f, 0.86f, 1f);
                    sombra = new GUIStyle(estilo);
                    sombra.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                }
                int px = Mathf.Max(11, Mathf.RoundToInt(Screen.height / 60f));
                estilo.fontSize = px; sombra.fontSize = px;
                float y = Screen.height * (float)Estado.Cfg.Num("brujula_altura", 0.07) + Mathf.Max(12, Mathf.RoundToInt(Screen.height / 50f)) * 2.2f;
                var r = new Rect(0f, y, Screen.width, px * 5f);
                Traduccion.Saltar = true;
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), texto, sombra);
                GUI.Label(r, texto, estilo);
            }
            catch (Exception e) { Estado.Fallo("social_dibujo", e); }
            finally { Traduccion.Saltar = false; }
        }
    }
}
