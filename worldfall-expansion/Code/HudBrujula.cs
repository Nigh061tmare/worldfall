using System;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Brujula de busquedas: una linea arriba de la pantalla con la busqueda que sigues, la distancia y
    // el rumbo a su objetivo. En primera persona (Worldfall) dice ademas hacia donde girar; en modo
    // dios mide desde el centro de la camara. F7 (brujula_tecla) cambia de busqueda; brujula=0 la apaga.
    //
    // Posicion: actor.current_position (Vector2, BaseSimObject) — verificada en Assembly-CSharp build 719.
    internal static class HudBrujula
    {
        static int seguida;
        static string linea = "";
        static float tCalc;
        static GUIStyle estilo, sombra;

        public static bool Activa { get { return Busquedas.Activo && Estado.Cfg.Bool("brujula"); } }

        public static void Tick()
        {
            if (!Activa) return;
            if (Input.GetKeyDown(Estado.Tecla(Estado.Cfg.Str("brujula_tecla"), KeyCode.F7)))
            {
                seguida++;
                tCalc = 0f;
            }
            float real = Time.unscaledTime;
            if (real - tCalc < 0.25f) return;   // 4 veces por segundo basta
            tCalc = real;
            linea = Calcula();
        }

        static string Calcula()
        {
            try
            {
                var activas = Busquedas.Activas;
                if (activas == null || activas.Count == 0) return "";
                if (seguida >= activas.Count) seguida = 0;
                Busqueda b = activas[seguida];
                string cab = "[" + (seguida + 1) + "/" + activas.Count + "] " + b.Titulo;
                if (b.Objetivo.Length == 0) return cab + " · vigila su ciudad";

                Actor obj = Busquedas.ActorDe(b.Objetivo);
                if (obj == null || !obj.isAlive()) return cab + " · " + b.ObjetivoNombre + ": sin rastro";

                Actor yo; float yaw;
                bool primera = PuenteWorldfall.PrimeraPersona(out yo, out yaw);
                Vector2 desde;
                if (primera) desde = yo.current_position;
                else
                {
                    Camera cam = Camera.main;
                    if (cam == null) return cab + " · " + b.ObjetivoNombre;
                    Vector3 p = cam.transform.position;
                    desde = new Vector2(p.x, p.y);
                }
                if (primera && yo == obj) return cab + " · ¡eres tu!";
                Vector2 hasta = obj.current_position;
                return cab + " · " + b.ObjetivoNombre + ": "
                    + Brujula.Linea(hasta.x - desde.x, hasta.y - desde.y, primera, yaw, Estado.Cfg.Bool("brujula_invertir"));
            }
            catch (Exception e) { Estado.Fallo("brujula", e); return ""; }
        }

        public static void Dibuja()
        {
            if (!Activa || linea.Length == 0) return;
            try
            {
                if (estilo == null)
                {
                    estilo = new GUIStyle(GUI.skin.label);
                    estilo.alignment = TextAnchor.UpperCenter;
                    estilo.fontStyle = FontStyle.Bold;
                    estilo.normal.textColor = new Color(1f, 0.86f, 0.45f);
                    sombra = new GUIStyle(estilo);
                    sombra.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                }
                int px = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 50f));
                estilo.fontSize = px; sombra.fontSize = px;
                float y = Screen.height * (float)Estado.Cfg.Num("brujula_altura", 0.07);
                var r = new Rect(0f, y, Screen.width, px * 2f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), linea, sombra);
                GUI.Label(r, linea, estilo);
            }
            catch (Exception e) { Estado.Fallo("brujula_dibujo", e); }
        }
    }
}
