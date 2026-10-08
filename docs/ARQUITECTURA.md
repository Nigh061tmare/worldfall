# ARQUITECTURA — Cómo expandir Worldfall y la pecera sin romperlos

## 1. Modelo de carga de mods en WorldBox

WorldBox usa **NeoModLoader (NML)** para cargar mods. NML escanea `Mods\`
(carpetas y zips) + el Workshop de Steam, compila el código C# de cada mod y lo
carga con **Harmony** (parches) + **MonoMod** (parches en memoria).

```
worldbox.exe
  └─ NML (Mods\NeoModLoader.dll)
       ├─ Mods\*.zip / carpetas  → cada mod se compila y carga
       ├─ Workshop (Steam)       → Worldfall vive aquí
       └─ StreamingAssets\mods   → copias "vanilla" (NO usar para plugins)
```

**Worldfall es un mod precompilado del Workshop**: NML lo detecta como
`precompiled` y lo salta en la fase de compilación. Se actualiza por Steam.

## 2. Por qué los plugins sobreviven a las actualizaciones de Worldfall

Worldfall **no expone una API de addons** (DLL cerrado, parchea el juego con
MonoMod). La vía compatible es un **mod compañero NML** que:

- Tiene su **propio GUID** y carpeta en `Mods\`.
- **Lee el mundo** (`World.world.units`, `Actor`, `Kingdom`, `City`) en lugar de
  tocar las clases internas de Worldfall.
- Añade contenido que funciona en **modo dios** (cámara alta) y que **Worldfall
  renderiza automáticamente en 3D** al acercarte (porque el 3D se construye del
  mismo mapa: árboles, tejados, colinas, criaturas).

Ejemplo probado: **PeceraWB** (en `pecera/`) añade una simulación social entera
(facciones, rumores, juicios, linaje) sin tocar `Worldfall.dll`, y funciona.
Cuando Worldfall se actualice, PeceraWB seguirá cargando igual.

## 3. Ciclo de vida de un mod NML (lo que compila y carga)

```
OnModLoad()          → se llama al iniciar el juego
  ├─ Init            → registrar recursos, config
  └─ Post-Init       → enganchar hooks de Harmony
Update()             → cada frame (solo si el juego está en marcha)
```

Cada mod vive en `Mods\<Carpeta>\`:
```
mod.json              → nombre, autor, version, targetGameBuild (719), GUID
Code\*.cs             → código (Main hereda de BasicMod<Main>)
Code\Core\*.cs        → lógica pura sin dependencias del juego (se puede testear)
GameResources\*       → assets (sprites, sonidos, iconos)
default_config.json   → configuración por defecto
```

## 4. API real de la build 719 (verificada contra otros mods)

Estas firmas **existen** y las usan Worldsmith/PowerBox/Kimetsu/AVB:

```csharp
// Unidades
actor.isAlive(); actor.isSapient(); actor.isAdult(); actor.isKing();
actor.getName(); actor.data.id;            // id numérico
actor.hasLover(); actor.lover;             // pareja
actor.getChildren(false);                  // hijos vivos (IEnumerable<Actor>)
actor.hasFamily();                         // ¿tiene familia?
actor.city; actor.kingdom;                 // contexto
actor.setCity(null);                       // destierro (Nivel 3)
actor.addRenown(n);                        // prestigio (Nivel 3)
city.leader; city.removeLeader();          // liderazgo

// Mundo
World.world.units.getSimpleList();         // todas las unidades
World.world.getCurWorldTime();             // tiempo de mundo (segundos)
World.world.map_stats.name / id            // identificador de mundo (por partida)
WorldTip.showNow(texto, false, "top", s);  // aviso en pantalla

// Reino/guerra
kingdom.data.name; kingdom.wild; kingdom.king;
DiplomacyManager.startWar(atq, def);       // declarar guerra (parcheable)
```

**Regla**: si un método no aparece en los mods de la build 719, **no lo uses**.
Puede haber cambiado de firma (los mods antiguos fallan al recompilar con `error CS`).

## 5. Hooks con Harmony (el patrón seguro)

Cada hook va en su **propia clase** para que, si una firma del juego cambia, solo
falle ese hook y no todo el mod:

```csharp
[HarmonyPatch]
internal static class HookMuerte
{
    [HarmonyPatch(typeof(Actor), "die")]
    [HarmonyPrefix]
    static void Antes(Actor __instance) { /* tu lógica, envuelta en try/catch */ }
}
```

Nunca dejes que una excepción del mod salga al juego: envuelve TODO en try/catch
y registra con `Debug.LogWarning`.

## 6. Persistencia (datos del mod, no del juego)

Los mods NML escriben en su propia carpeta:
```
C:\Users\<usuario>\AppData\LocalLow\mkarpenko\WorldBox\<ModCarpeta>\
```
PeceraWB guarda ahí `config.txt`, `mundos\<mundo>\*.jsonl` (crónica, fichas,
afectos, linaje, memoria). Así **nunca escribe en los saves del juego** y las
partidas no se corrompen.

## 7. Configuración de PeceraWB (referencia)

```ini
# PeceraWB\config.txt
activo=1
dia_segundos=2          # segundos de mundo = 1 "día" de simulación
muestra_n=60            # unidades observadas por pasada
voz=0                   # voz LLM local (Ollama); 1 = todas, F10 alterna
voz_grandes=1           # aun con voz=0: guerras, facciones grandes
olvido_factor=3         # el afecto se enfría 3x más lento
faccion_umbral=0.10     # afecto mínimo para ser aliado
silencio_seg=90         # sin anuncios al cargar un mundo
nivel3=1                # consecuencias reales (destierros, disputas)
nivel3_max_hora=6       # tope de acciones reales por hora de mundo
juicios=1
informe_tecla=F9        # escribe informe.md
voz_tecla=F10
```

## 8. Cómo probar los cambios

1. Con el juego **cerrado**, edita `Mods\<Mod>\Code\*.cs`.
2. Lanza WorldBox → NML recompila el mod automáticamente (el log muestra
   `Compile Mod X` y `error CS` si algo falla).
3. Verifica en `Player.log` (`LocalLow\mkarpenko\WorldBox\Player.log`) que
   `Init Mod`/`Post-Init Mod` salen sin errores.
4. Si es un cambio de UI, entra a un mundo y compruébalo en vivo.
5. Guarda siempre una copia de la partida antes de probar cambios que escriban en el mundo.