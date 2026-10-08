using System;
using HarmonyLib;
using UnityEngine;

namespace WorldfallExpansion
{
    // EJEMPLO de hook seguro. Copia este patron para cada feature nueva:
    // clase propia, HarmonyPatch con try/catch, sin tocar clases de Worldfall.
    //
    // Este hook de ejemplo observa muertes de reyes y lo anuncia en pantalla
    // (visible en modo dios y en primera persona porque usa WorldTip).
    [HarmonyPatch]
    internal static class HookHUD
    {
        [HarmonyPatch(typeof(Actor), "die")]
        [HarmonyPrefix]
        static void Antes(Actor __instance)
        {
            try
            {
                if (__instance == null || !__instance.isSapient()) return;
                if (!__instance.isKing()) return;
                string reino = __instance.kingdom != null && __instance.kingdom.data != null
                    ? __instance.kingdom.data.name : "el mundo";
                WorldTip.showNow("Ha muerto el rey de " + reino, false, "top", 4f);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WorldfallExp] HookHUD: " + e.Message);
            }
        }
    }
}