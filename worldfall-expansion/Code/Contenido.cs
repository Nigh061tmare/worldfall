using System;
using System.Collections.Generic;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Registra en el JUEGO el Arsenal y los rasgos de Core/Catalogo.cs. Worldfall los usa sin saber de
    // nosotros: recorre AssetManager.items para sus recetas (Crafting.LoadRecipes, al abrir I) y
    // AssetManager.items / traits para su panel de regalos.
    //
    // Como el propio juego (ItemLibrary.init): se CLONA un objeto existente y se cambian coste y stats.
    // El clon hereda icono, sprite en la mano y tipo; BaseStats es ICloneable, asi que el original no se
    // toca. Lo que el juego rellena en post_init/linkAssets (ya pasados cuando carga un mod) se copia a
    // mano: sprites, modificadores y coste en monedas.
    //
    // Se hace en el primer Update con los catalogos ya cargados (no depende del orden de carga de NML).
    // Interruptor: contenido=1. Firmas leidas de Assembly-CSharp build 719 (ver docs/ARQUITECTURA.md 4.3).
    internal static class Contenido
    {
        static bool hecho;
        static float tIntento = -100f;
        static int intentos;

        public static void Tick()
        {
            if (hecho || !Estado.Cfg.Bool("contenido")) return;
            float real = Time.unscaledTime;
            if (real - tIntento < 2f) return;
            tIntento = real;
            if (++intentos > 300) { hecho = true; Debug.LogWarning("[WorldfallExp] contenido: el juego nunca cargo sus catalogos"); return; }
            if (AssetManager.items == null || AssetManager.traits == null || AssetManager.resources == null) return;
            if (AssetManager.items.list.Count == 0 || AssetManager.traits.list.Count == 0) return;
            hecho = true;

            foreach (string e in Catalogo.Valida()) Debug.LogWarning("[WorldfallExp] catalogo: " + e);
            int obj = 0, ras = 0;
            foreach (ObjetoDef o in Catalogo.Objetos) if (Objeto(o)) obj++;
            foreach (RasgoDef r in Catalogo.Rasgos) if (Rasgo(r)) ras++;
            Textos();
            Debug.Log("[WorldfallExp] contenido: " + obj + "/" + Catalogo.Objetos.Length + " objetos y " + ras + "/"
                      + Catalogo.Rasgos.Length + " rasgos en el juego (fabricacion I y regalos en Worldfall)");
        }

        static bool Objeto(ObjetoDef o)
        {
            try
            {
                if (AssetManager.items.has(o.Id)) return true;   // ya registrado (recarga de mods)
                EquipmentAsset src = AssetManager.items.get(o.Base);
                if (src == null) { Debug.LogWarning("[WorldfallExp] contenido: no existe la base " + o.Base); return false; }
                EquipmentAsset t = AssetManager.items.clone(o.Id, o.Base);
                t.translation_key = Catalogo.ClaveObjeto(o);
                t.setCost(0, o.Res1, o.Cant1, o.Res2, o.Res2 == "none" ? 0 : o.Cant2);
                t.equipment_value = o.Valor;
                t.cost_coins_resources = Monedas(o.Res1) + (o.Res2 == "none" ? 0 : Monedas(o.Res2));
                t.gameplay_sprites = src.gameplay_sprites;
                t.item_modifiers = src.item_modifiers;
                foreach (var kv in o.Stats) Stat(t.base_stats, kv.Key, kv.Value, o.Id);
                return true;
            }
            catch (Exception e) { Estado.Fallo("objeto " + o.Id, e); return false; }
        }

        static bool Rasgo(RasgoDef r)
        {
            try
            {
                if (AssetManager.traits.has(r.Id)) return true;
                var t = new ActorTrait
                {
                    id = r.Id,
                    path_icon = "ui/Icons/actor_traits/" + r.Icono,
                    group_id = r.Grupo,
                    rarity = Rarity.R1_Rare,
                    type = r.Tipo == TipoRasgo.Positivo ? TraitType.Positive : (r.Tipo == TipoRasgo.Negativo ? TraitType.Negative : TraitType.Other),
                    can_be_given = true,
                    needs_to_be_explored = false,
                    has_description_2 = false,
                };
                AssetManager.traits.add(t);
                foreach (var kv in r.Stats) Stat(t.base_stats, kv.Key, kv.Value, r.Id);
                return true;
            }
            catch (Exception e) { Estado.Fallo("rasgo " + r.Id, e); return false; }
        }

        static void Stat(BaseStats b, string k, float v, string quien)
        {
            try { b[k] = v; }
            catch (Exception e) { Estado.Fallo("stat " + quien + "." + k, e); }
        }

        static int Monedas(string recurso)
        {
            try { ResourceAsset r = AssetManager.resources.get(recurso); return r != null ? r.money_cost : 0; }
            catch (Exception) { return 0; }
        }

        // Los nombres vienen de Locales/es.json y en.json (NML los carga y los reaplica al cambiar de idioma).
        // Por si NML no lo hiciera, se anaden aqui las claves que falten, en el idioma activo.
        public static void Textos()
        {
            try
            {
                bool es = LocalizedTextManager.current_language != null && LocalizedTextManager.current_language.id == "es";
                foreach (var kv in Catalogo.Textos(es))
                    if (!LocalizedTextManager.stringExists(kv.Key)) LocalizedTextManager.add(kv.Key, kv.Value, false, "", false);
            }
            catch (Exception e) { Estado.Fallo("contenido_textos", e); }
        }
    }
}
