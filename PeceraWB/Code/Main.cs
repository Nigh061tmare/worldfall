using System;
using HarmonyLib;
using NeoModLoader.api;
using UnityEngine;

namespace PeceraWB
{
    // Pecera para WorldBox. Mod NML independiente: no referencia ni parchea Worldfall,
    // asi que sobrevive a sus actualizaciones. Solo LEE el mundo (unidades, reinos, guerras).
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            try { Mundo.Inicia(); }
            catch (Exception e) { Debug.LogError("[PeceraWB] no arranca: " + e); return; }

            // Cada parche en su propia clase: si el juego cambia una firma, solo cae ese hook.
            Parchea(typeof(HookMuerte));
            Parchea(typeof(HookGuerra));
            Parchea(typeof(HookUnidad));
            Debug.Log("[PeceraWB] v" + Mundo.Version + " cargado. Datos en " + Mundo.Dir
                      + " (F9 informe, F10 voz on/off)");
        }

        static void Parchea(Type t)
        {
            try { new Harmony("joseluis.peceraWB." + t.Name).PatchAll(t); }
            catch (Exception e) { Debug.LogWarning("[PeceraWB] hook " + t.Name + " no se pudo instalar: " + e.Message); }
        }

        private void Update()
        {
            try { Mundo.Tick(); }
            catch (Exception e) { Mundo.Fallo("Tick", e); }
        }
    }
}
