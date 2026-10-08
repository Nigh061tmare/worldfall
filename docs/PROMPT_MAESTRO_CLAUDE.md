# PROMPT MAESTRO — EXPANSIÓN WORLDBOX (WORLDFALL + PECERA) CON CLAUDE OPUS

Copia TODO lo que está entre las líneas de separación y pégalo en un nuevo chat
de claude.ai (idealmente dentro de un Proyecto donde también subas la carpeta
`worldbox-expansions` del repo). Responde en español.

================================================================================
# Contexto del proyecto

Tengo **WorldBox 0.51.2 (build 719)** (Unity 2022.3.60f1, Mono/.NET 4.x) con el
mod loader **NeoModLoader (NML)** y **Harmony** para parchear. Uso C# que el NML
compila con un compilador compatible con **C# 5** (nada de records, init, switch
expressions, etc.).

Instalé un ecosistema de mods que funciona: Worldfall (primera persona 3D,
DLL cerrado del Steam Workshop), PeceraWB (mi mod de simulación social) y otros
15 mods (Worldsmith, PowerBox, AVB 3.01, ModernMod, FunBoost, Kimetsu-Box,
Plenty o Biomes, WB DLC, etc.), todos en la build 719.

# Objetivo

Quiero expandir "a lo bestia" **Worldfall** y **PeceraWB** con nuevas features,
PERO con una condición sagrada: **todo debe seguir funcionando cuando Worldfall
(o el juego) se actualice**. Por eso:

- **NUNCA toques `Worldfall.dll`** (es del Workshop, se autoactualiza). No lo
  copies a `streamingassets\mods`, no lo parchees, no lo reemplacees.
- **Toda expansión = un mod NML separado** con su propio GUID en `Mods\<Carpeta>\`,
  que LEE el mundo (`World.world`, `Actor`, `Kingdom`, `City`) en lugar de tocar
  las clases internas de Worldfall.
- Worldfall renderiza en 3D lo que ya hay en el mapa: cualquier criatura, poder,
  bioma o estructura que yo añada se verá automáticamente en primera persona.
  Esa es la vía de expansión: añadir contenido al mundo, no al DLL.

# Arquitectura (léela y respétala)

Un mod NML vive en `Mods\<Carpeta>\`:
- `mod.json` → name, author, version, `targetGameBuild: 719`, GUID, Dependencies.
- `Code\*.cs` → código (la clase principal hereda de `BasicMod<Main>` de
  `NeoModLoader.api`, con `OnModLoad()` que llama Init + hooks).
- `Code\Core\*.cs` → lógica pura sin referencias a Unity/el juego (se puede
  compilar aparte con `csc /langversion:5` y testear).
- `GameResources\*` → assets (sprites, iconos, sonidos).
- Persistencia SOLO en `C:\Users\<user>\AppData\LocalLow\mkarpenko\WorldBox\<Mod>\`
  (config.txt, mundos\*.jsonl). NUNCA escribir en los saves del juego.

Cada hook de Harmony va en su propia clase con try/catch, para que si una firma
del juego cambia solo falle ese hook, no todo el mod.

# API verificada de la build 719 (solo usa estas firmas u otras que verifiques)

Unidades: `actor.isAlive()`, `isSapient()`, `isAdult()`, `isKing()`, `getName()`,
`actor.data.id`, `hasLover()`, `lover`, `getChildren(false)` (IEnumerable<Actor>),
`hasFamily()`, `city`, `kingdom`, `setCity(null)`, `addRenown(n)`, `city.leader`,
`city.removeLeader()`.
Mundo: `World.world.units.getSimpleList()`, `World.world.getCurWorldTime()`,
`World.world.map_stats.name` / `.id`, `WorldTip.showNow(texto, false, "top", s)`.
Reinos: `kingdom.data.name`, `kingdom.wild`, `kingdom.king`,
`DiplomacyManager.startWar(atq, def)`.

Regla: si un método no aparece en mods de la build 719, NO lo uses; puede haber
cambiado y romper la compilación.

# Qué hace ya PeceraWB (base para expandir)

- Observa el mundo (parejas, vecinos, muertes, guerras) y deriva:
  afectos entre personajes, facciones, rumores que se distorsionan, juicios por
  rumores graves, cultura/credo del mundo, sueños personales, linaje (padres →
  hijos vía getChildren(false)), sucesión de reyes (con disputas) y mentoría.
- Nivel 3 (interruptor `nivel3=1`): consecuencias reales — destierros con
  `setCity(null)`, +renombre al heredero, rival desterrado tras disputa. Con
  tope de acciones por hora y log en `nivel3.log`.
- Voz con Ollama local (opcional): frases en eventos grandes.
- UI: etiqueta social en la ventana de unidad (casa, rasgos, amigo, rival, sueño).
- Config en `config.txt` (afectos, umbrales, nivel3, teclas F9/F10).

# Qué es el plugin worldfall-expansion (esqueleto a crecer)

Mod NML base con GUID `joseluis_worldfall_expansion`. Añade contenido al mundo
que Worldfall muestra en 3D. De momento solo tiene el Main y un HookHUD de ejemplo
(anuncia muertes de rey). Hay que poblarlo de features.

# Ideas de expansión (elige, combina o proponme las tuyas)

Para **Worldfall-expansion** (mods compañeros):
- Razas/criaturas nuevas visibles en 3D al acercarme (dragones montables, bestias).
- Poderes de dios nuevos que se vean en primera persona.
- HUD/UI en primera persona: brújula, minimapa ampliado, marcadores de quest.
- Eventos de mundo flotantes, tesoros, monturas domesticables.
- Búsquedas que un personaje pueda seguir caminando en 1ª persona.

Para **PeceraWB**:
- Mentoría ligada a profesiones reales del mundo.
- Herencias de bienes reales entre generaciones.
- Guerras civiles por facciones rivales (Nivel 3, con interruptor).
- Diplomacia entre reinos dirigida por los afectos de la pecera.
- Quests generadas por la crónica que se puedan seguir en primera persona.
- Burbujas de texto sobre personajes notables (render, delicado).
- Más rasgos de personalidad alimentando charlas y rumores.

# Forma de trabajar (importante)

1. Antes de escribir código, pídeme confirmación con un **plan** que indique:
   qué añades, qué archivos tocas/creas, qué firmas usas y cómo lo pruebas.
2. Cambios **pequeños e incrementales** (una feature por vez), código completo y
   listo para pegar en `Mods\<Carpeta>\Code\`.
3. Mantén el `Core\` (sin Unity) separado del adaptador.
4. Todo envuelto en try/catch; fallos con `Debug.LogWarning`, nunca tumbar el juego.
5. Lo que escriba en el mundo: detrás de interruptor en `config.txt` y con topes.
6. No uses `@ts-ignore`/trucos: el código debe compilar limpio con el NML (C# 5).

Empieza preguntándome qué feature quiero atacar primero, o propón la que creas
que da más valor y es más segura. Responde en español.
================================================================================