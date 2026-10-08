using System;
using System.Text;
using HarmonyLib;
using Pecera.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeceraWB
{
    // Muestra el estado social de la unidad seleccionada bajo su nombre en la ventana de unidad:
    // casa, caracter, mejor amigo, rival y sueno. Es una etiqueta propia, solo lectura: no toca
    // el nombre ni los datos del juego (mismo enfoque que usa Worldsmith para sus titulos).
    [HarmonyPatch]
    internal static class HookUnidad
    {
        const string Nombre = "pecera_social";
        static Text etiqueta;

        [HarmonyPatch(typeof(UnitWindow), "loadNameInput")]
        [HarmonyPostfix]
        static void Despues(UnitWindow __instance)
        {
            try { Refresca(__instance); }
            catch (Exception e) { Mundo.Fallo("ventana_unidad", e); }
        }

        static void Refresca(UnitWindow w)
        {
            if (!Mundo.Activo || w == null || w.name_input == null) return;
            Actor u = SelectedUnit.unit;
            string txt = (u != null && u.isAlive() && u.isSapient()) ? Mundo.LineasSociales(u) : null;
            if (string.IsNullOrEmpty(txt)) { if (etiqueta != null) etiqueta.gameObject.SetActive(false); return; }
            Construye(w);
            etiqueta.text = txt;
            etiqueta.gameObject.SetActive(true);
        }

        static void Construye(UnitWindow w)
        {
            if (etiqueta != null) { etiqueta.transform.SetParent(w.name_input.transform, false); return; }
            var go = new GameObject(Nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(w.name_input.transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 0f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -15f);   // debajo de la linea de titulo de otros mods
            r.sizeDelta = new Vector2(0f, 26f);
            etiqueta = go.GetComponent<Text>();
            etiqueta.font = LocalizedTextManager.current_font;
            etiqueta.fontSize = 8;
            etiqueta.alignment = TextAnchor.UpperCenter;
            etiqueta.supportRichText = false;
            etiqueta.horizontalOverflow = HorizontalWrapMode.Overflow;
            etiqueta.color = new Color(0.55f, 0.85f, 1f);
            etiqueta.raycastTarget = false;
        }
    }

    internal static partial class Mundo
    {
        // Dos lineas: "Casa de X · rasgos" y "Amigo: A · Rival: B · Sueño: meta (50 %)". null si aun no hay datos.
        public static string LineasSociales(Actor a)
        {
            string id = Id(a);
            Ficha f;
            if (!Fichas.TryGetValue(id, out f)) return null;

            string amigo = null, rival = null; double mejor = 0.05, peor = -0.05;
            foreach (var kv in Afectos.Todos())
            {
                string x, y; ModeloAfectivo.Separa(kv.Key, out x, out y);
                if (x != id) continue;
                double s = Afectos.Sentimiento(x, y);
                if (s > mejor) { mejor = s; amigo = y; }
                else if (s < peor) { peor = s; rival = y; }
            }
            Faccion fac; facDe.TryGetValue(id, out fac);

            var l1 = new StringBuilder();
            if (fac != null) l1.Append(fac.Nombre);
            if (f.Rasgos.Count > 0) { if (l1.Length > 0) l1.Append(" · "); l1.Append(string.Join(", ", f.Rasgos.ToArray())); }

            var l2 = new StringBuilder();
            if (amigo != null) l2.Append("Amigo: ").Append(NombreDe(amigo));
            if (rival != null) { if (l2.Length > 0) l2.Append(" · "); l2.Append("Rival: ").Append(NombreDe(rival)); }
            Sueno sn = Suenos.De(f);
            if (sn != null)
            {
                if (l2.Length > 0) l2.Append(" · ");
                l2.Append("Sueño: ").Append(sn.Texto).Append(sn.Cumplido ? " (cumplido)" : " (" + (int)(sn.Progreso * 100) + " %)");
            }
            if (l1.Length == 0 && l2.Length == 0) return null;
            return l1 + "\n" + l2;
        }
    }
}
