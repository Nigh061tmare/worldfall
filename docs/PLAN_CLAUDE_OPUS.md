# PLAN DE EXPANSIÓN CON CLAUDE OPUS

Guía para que **Claude Opus** (en claude.ai / API) expanda Worldfall y la pecera
usando este repositorio. Léela completa antes de pedir nada.

## Contexto que Opus necesita saber

- Juego: **WorldBox 0.51.2, build 719** (Unity 2022.3.60f1, Mono/.NET 4.x, C# con
  Harmony + NML). OJO: el NML compila con un compilador **C# 5** y referencias a
  las DLL del juego; no uses features modernas (records, init, etc.).
- Mod loader: **NeoModLoader (NML)** — ver `docs/ARQUITECTURA.md` §1.
- **Worldfall**: DLL cerrado del Workshop, **no tocar**. Expansiones = mods
  compañeros NML separados.
- **PeceraWB**: ya es un mod compañero funcional (simulación social). Es la base
  sobre la que expandir "a lo bestia".

## Archivos de referencia (los más importantes)

1. `docs/ARQUITECTURA.md` — modelo de carga, API real de la build 719, hooks, persistencia.
2. `pecera/Code/Mundo.cs` — estado global, config, tick, cronica, guardado.
3. `pecera/Code/Muestreo.cs` — observación del mundo (parejas, charlas, facciones, juicios).
4. `pecera/Code/Hooks.cs` — hooks de muerte/guerra + sucesión (patrón de parche seguro).
5. `pecera/Code/UnidadUi.cs` — etiqueta social en la ventana de unidad (referencia UI).
6. `pecera/Code/VozLlm.cs` — voz con Ollama local (referencia hilo en segundo plano).
7. `pecera/Code/Nivel3.cs` — consecuencias reales (destierros, disputas) — cómo se escribe en el mundo.
8. `pecera/Code/Core/*.cs` — lógica pura sin Unity (Afectos, Sociedad, Secretos, Linaje...).

## Ideas de expansión (elige, pide nuevas o combina)

### Para Worldfall (mods compañeros, sin tocar el DLL)
- **Criaturas y razas nuevas** que se rendericen en 3D al acercarte.
- **Poderes de dios nuevos** visibles en primera persona (Worldfall ya muestra
  poderes en 3D: hongo atómico, meteoros...).
- **HUD/UI en primera persona**: minimapa ampliado, brújula, marcadores de quest.
- **Eventos de mundo que hablan**: crónicas flotantes, búsquedas del tesoro.
- **Animales de montura** o bestias domesticables controlables en 1ª persona.

### Para PeceraWB (expandir lo que ya hay)
- **Mentoría completa** ligada a las profesiones reales (guerrero, alquimista...).
- **Herencias de bienes reales** (intercambio de recursos entre generaciones).
- **Guerras civiles por facciones rivales** (Nivel 3, con interruptor).
- **Diplomacia entre reinos** dirigida por afectos de la pecera.
- **Quest generadas por la crónica** que el jugador pueda seguir en primera persona.
- **Más rasgos de personalidad** alimentando charlas y rumores.
- **Burbujas de texto** sobre personajes notables (render, delicado).

## Reglas innegociables para Opus

1. **Nunca** tocar `Worldfall.dll`, `NeoModLoader.dll`, `NCMS_memload.dll`,
   `streamingassets\mods\*` ni `settings.txt` de Worldfall.
2. `targetGameBuild: 719` en todo mod nuevo.
3. C# compatible con el compilador del NML (sin syntax de C# 7+; usar `var`, no
   `out var` inline raro, evitar `?.` si hay dudas — se puede pero con cuidado).
4. Toda API usada debe estar en `docs/ARQUITECTURA.md` §4 o verificada contra los
   mods de la build 719 del propio repositorio. **Prohibido adivinar firmas**.
5. Todo el código envuelto en try/catch; los fallos se loguean con `Debug.LogWarning`
   y jamás tumban el juego.
6. Persistencia SOLO en la carpeta propia del mod (`LocalLow\mkarpenko\WorldBox\<Mod>`),
   nunca en los saves del juego.
7. Los cambios que escriban en el mundo van detrás de un interruptor en `config.txt`
   (ej. `nivel3=0/1`) con topes de acción.
8. Antes de tocar, leer `mod.json` y `Code/Main.cs` del mod a expandir para respetar
   su estructura.

## Flujo de trabajo recomendado

1. **Pide un plan** antes de código: "describe qué vas a añadir, qué archivos tocas,
   qué firmas usas y cómo lo pruebas". Revisa que respete las reglas.
2. **Cambios pequeños e incrementales** (un feature por PR/commit).
3. **Prueba en el juego** con la partida respaldada; el log `Player.log` es la fuente de verdad.
4. Mantén el `Core\` (sin Unity) separado del adaptador, para poder testear la lógica.
5. Actualiza este `PLAN_CLAUDE_OPUS.md` y `ARQUITECTURA.md` cuando añadas features.

## Archivos del repo que NO se suben a GitHub

- `Worldfall.dll` (propiedad de su autor; el repo solo referencia el ZIP/dónde obtenerlo).
- Saves de WorldBox ni datos de la pecera (`mundos\*`).
- Los zips de Plenty o Biomes / WB DLC (se descargan de Nexus).

## Preguntas iniciales que conviene hacer a Opus

- "Diseña la estructura de carpetas para añadir 3 features nuevos a PeceraWB sin romper lo que hay."
- "¿Cómo haría un mod compañero de Worldfall que añada una raza de dragones montables visibles en 3D, respetando las reglas?"
- "Expande el Nivel 3 para añadir guerras civiles por facciones con interruptor de seguridad."