using System;
using HarmonyLib;
using NeoModLoader.api;
using UnityEngine;

namespace WorldfallExpansion
{
    // Plugin companero de Worldfall.
    // NO toca Worldfall.dll: lee el mundo (que Worldfall renderiza en 3D) y anade
    // contenido que funciona en modo dios y en primera persona. Sobrevive a updates.
    //
    // ESTE ES EL PUNTO DE PARTIDA. Expandelo con nuevas clases (una por feature):
    //   - CriaturasNuevas.cs   (razas/animales que se ven en 3D)
    //   - Poderes3D.cs         (poderes de dios visibles en primera persona)
    //   - HUD.cs               (UI en primera persona: brujula, marcadores, quests)
    //   - Eventos.cs           (crónicas flotantes, tesoros, monturas)
    //
    // Antes de escribir codigo, lee docs/ARQUITECTURA.md (API real de la build 719).
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            try
            {
                Init();
                Debug.Log("[WorldfallExp] v" + Estado.Version + " cargado. Worldfall sigue siendo el del Workshop. Datos en " + Estado.Dir
                          + " (" + Estado.Cfg.Str("busquedas_tecla") + " lista de busquedas)");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WorldfallExp] no arranca: " + e.Message);
            }
        }

        static void Init()
        {
            // Config propia (LocalLow\mkarpenko\WorldBox\WorldfallExpansion\config.txt)
            Estado.Inicia();
            if (!Estado.Activo) { Debug.Log("[WorldfallExp] activo=0: inerte"); return; }
            // Cronica -> Busquedas: solo lectura, sin Harmony (compara fotos del mundo).
            Busquedas.Inicia();
            // Parchea hooks propios (mismo patron que PeceraWB/Code/Hooks.cs)
            // Parchea(typeof(HookHUD));
            // Parchea(typeof(HookCriaturas));
        }

        static void Parchea(Type t)
        {
            try { new Harmony("joseluis.wfexp." + t.Name).PatchAll(t); }
            catch (Exception e) { Debug.LogWarning("[WorldfallExp] hook " + t.Name + " no instalado: " + e.Message); }
        }

        void Update()
        {
            // Tick del plugin (solo si hay mundo cargado y el mod esta activo)
            try { Busquedas.Tick(); }
            catch (Exception e) { Estado.Fallo("Tick", e); }
        }
    }
}