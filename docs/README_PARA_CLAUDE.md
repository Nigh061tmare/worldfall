# Mods fuente de WorldBox — para Claude Opus

Estos son los **archivos fuente reales** de los dos mods instalados en
WorldBox (build 719), sacados de la instalación del juego. Sin DLLs, sin binarios.

## Contenido

- `PeceraWB/` — el mod de simulación social instalado y funcionando.
  - `mod.json` (GUID `joseluis_peceraWB`, targetGameBuild 719)
  - `Code/*.cs` — adaptadores (Mundo, Muestreo, Hooks, Nivel3, VozLlm, UnidadUi, Informe, Main)
  - `Code/Core/*.cs` — lógica pura sin Unity (C# 5, compila con csc /langversion:5)
- `worldfall-expansion/` — plugin esqueleto para expandir Worldfall.
  - `mod.json` (GUID `joseluis_worldfall_expansion`, targetGameBuild 719)
  - `Code/Main.cs` + `Code/HookHUD.cs` (ejemplo de hook seguro)
- `docs/` — ARQUITECTURA (API verificada build 719), PLAN_CLAUDE_OPUS y PROMPT_MAESTRO.

## Firmas verificadas de la build 719 (confirmadas contra Worldsmith/PowerBox/AVB)

Unidades: isAlive() isSapient() isAdult() isKing() getName() data.id hasLover()
lover getChildren(false) hasFamily() city kingdom setCity(null) addRenown(n)
city.leader city.removeLeader()
Mundo: World.world.units.getSimpleList() World.world.getCurWorldTime()
World.world.map_stats.name / .id  WorldTip.showNow(texto,false,"top",s)
Reinos: kingdom.data.name kingdom.wild kingdom.king DiplomacyManager.startWar(atq,def)

## Notas sobre lo que pidió Claude Opus

1. `BasicMod<T>` es un MonoBehaviour: su Update() se llama cada frame. Ver
   `PeceraWB/Code/Main.cs` línea `private void Update() { Mundo.Tick(); }`.
   (Ya funciona así en el juego desde hace días.)
2. Para recorrer reinos SIN acceder a una lista interna: se puede deducir desde
   las unidades con `actor.kingdom` — pero cuidado con reinos vacíos. La pecera
   usa exactamente ese enfoque en `Muestreo.cs` y `Hooks.cs`.
3. `World.world.map_stats.name` funciona (usado en `Mundo.cs` ClaveMundo()).
4. La config de PeceraWB vive en `LocalLow\mkarpenko\WorldBox\PeceraWB\config.txt`
   (formato clave=valor, se autogenera al primer arranque). El parser está en
   `Code/Mundo.cs` (clase Cfg).

## Estado de PeceraWB

v0.1.0 funcional: afectos, facciones, rumores, juicios, sueños, linaje, sucesión,
mentoría, voz (Ollama opcional), UI en ventana de unidad (F9 informe, F10 voz).
Nivel 3 (destierros/disputas) detrás de `nivel3=1` en config.

## Para Claude Opus

La primera feature acordada: **«Crónica → Búsquedas»** en worldfall-expansion,
modo solo lectura, sin Harmony (snapshots comparados), usando SOLO las firmas
verificadas de arriba. Implementar sobre ESTE código real, respetando namespace
`WorldfallExpansion`, GUID `joseluis_worldfall_expansion` y targetGameBuild 719.