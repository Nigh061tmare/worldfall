using System;
using HarmonyLib;
using NeoModLoader.api;
using UnityEngine;

namespace WorldfallExpansion
{
    // Plugin companero de Worldfall.
    // NO toca Worldfall.dll: lee el mundo (que Worldfall renderiza en 3D) y anade contenido que funciona
    // en modo dios y en primera persona. Sobrevive a updates: cada feature falla por separado.
    //
    // Features (v3):
    //   Busquedas.cs      Cronica -> Busquedas (fotos del mundo, solo lectura)
    //   PuentePecera.cs   busquedas a partir de la cronica de PeceraWB (lee su fichero)
    //   HudBrujula.cs     brujula hacia el objetivo (primera persona con PuenteWorldfall.cs)
    //   Traduccion.cs     Worldfall en espanol (Traducciones/worldfall_es.txt)
    //
    // Antes de escribir codigo, lee docs/ARQUITECTURA.md (API real de la build 719).
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            try
            {
                Arranca();
                Debug.Log("[WorldfallExp] v" + Estado.Version + " cargado. Worldfall sigue siendo el del Workshop. Datos en " + Estado.Dir
                          + " (" + Estado.Cfg.Str("busquedas_tecla") + " lista de busquedas, " + Estado.Cfg.Str("brujula_tecla") + " cambia la brujula)");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WorldfallExp] no arranca: " + e.Message);
            }
        }

        // (Antes se llamaba Init: ocultaba BasicMod.Init(), que NML usa en su carga por fases.)
        static void Arranca()
        {
            // Config propia (LocalLow\mkarpenko\WorldBox\WorldfallExpansion\config.txt)
            Estado.Inicia();
            if (!Estado.Activo) { Debug.Log("[WorldfallExp] activo=0: inerte"); return; }
            Paso("traduccion", Traduccion.Inicia);
            Paso("busquedas", Busquedas.Inicia);
            Paso("pecera", PuentePecera.Inicia);
            // Parchea hooks propios (mismo patron que PeceraWB/Code/Hooks.cs)
            // Parchea(typeof(HookHUD));
        }

        // Cada feature arranca por separado: si una falla, las demas siguen.
        static void Paso(string nombre, Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogWarning("[WorldfallExp] " + nombre + " no arranca: " + e.Message); }
        }

        static void Parchea(Type t)
        {
            try { new Harmony("joseluis.wfexp." + t.Name).PatchAll(t); }
            catch (Exception e) { Debug.LogWarning("[WorldfallExp] hook " + t.Name + " no instalado: " + e.Message); }
        }

        void Update()
        {
            if (!Estado.Activo) return;
            try { Contenido.Tick(); } catch (Exception e) { Estado.Fallo("TickContenido", e); }
            try { Busquedas.Tick(); } catch (Exception e) { Estado.Fallo("Tick", e); }
            try { PuentePecera.Tick(); } catch (Exception e) { Estado.Fallo("TickPecera", e); }
            try { HudBrujula.Tick(); } catch (Exception e) { Estado.Fallo("TickBrujula", e); }
            try { HudSocial.Tick(); } catch (Exception e) { Estado.Fallo("TickSocial", e); }
            try { FrasesPNJ.Tick(); } catch (Exception e) { Estado.Fallo("TickFrases", e); }
            try { Traduccion.Tick(); } catch (Exception e) { Estado.Fallo("TickTraduccion", e); }
        }

        void OnGUI()
        {
            if (!Estado.Activo) return;
            HudBrujula.Dibuja();
            HudSocial.Dibuja();
        }
    }
}
