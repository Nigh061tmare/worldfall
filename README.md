# WorldBox Expansions — Worldfall + Pecera

Ecosistema de expansiones para **WorldBox 0.51.2 (build 719)** que coexisten con
**Worldfall** (primera persona 3D) y **PeceraWB** (simulación social), **sin tocar
ninguno de sus binarios** para que las actualizaciones de Steam/Workshop no las rompan.

## Cómo funciona (en una frase)

Cada expansión es un **mod NML independiente** con su propio GUID en `Mods\`.
Worldfall y la pecera leen el **mundo** (no sus DLLs), así que un plugin que añade
criaturas, poderes, biomas o UI funciona igual en modo dios y en primera persona.

## Contenido

| Carpeta | Qué es | Estado |
|---|---|---|
| `worldfall-expansion/` | Plugin NML para expandir Worldfall. **v3.0.0**: Worldfall en español, búsquedas (mundo + PeceraWB) y brújula en primera persona (ver `docs/WORLDFALL_EXPANSION_V3.md`) | Probado fuera del juego; pendiente de probar en partida |
| `PeceraWB/` | PeceraWB completo: afectos, facciones, rumores, juicios, linaje, sucesión, mentoría, nivel 3 | Funcional en tu partida |
| `dev/` | Tests del Core y comprobación C# 5 contra stubs con solo la API verificada (`dev/check.sh`). **No se copia al juego** | CI en cada push |

## Reglas de oro (léelas antes de expandir)

1. **NUNCA modifiques `Worldfall.dll`** ni lo copies a `StreamingAssets\mods`.
   Es del Workshop y se actualiza solo.
2. **Toda expansión va como mod NML separado** (`Mods\Expansion\` con su `mod.json`).
3. **`targetGameBuild` debe ser `719`** (tu versión) para que NML no falle.
4. Solo escribe en el mundo vía API probada por otros mods de la build 719
   (`setCity`, `addRenown`, `getChildren(false)`, etc.) — nunca adivines firmas.
5. Con `Experimental Mode` apagado tras un update de WorldBox, reactívalo en Ajustes.

## Estado del juego

- **Worldfall**: beta 0.9.2 (última), activa vía Workshop + respaldo en `Worldfall_backup`.
- **PeceraWB**: v0.1.0, activa, con nivel 3 (consecuencias reales) configurable.
- **15 mods** coexistiendo: Westactions, AVB 3.01, PowerBox 1.5.2, Worldsmith,
  ModernMod, FunBoost, Kimetsu-Box, Natural Traits, Isekai, KingBox, WARRIOR,
  UnlockAll, Plenty o Biomes, WB DLC, PeceraWB + Worldfall.

Ver `docs/ARQUITECTURA.md` para el detalle técnico y `docs/PLAN_CLAUDE_OPUS.md`
para la guía de expansión con Claude Opus.