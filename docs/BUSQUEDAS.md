# Crónica → Búsquedas (worldfall-expansion)

> Desde la v3.0: búsquedas también desde PeceraWB y brújula. Ver `WORLDFALL_EXPANSION_V3.md`.

El plugin observa el mundo y abre **búsquedas**: personajes concretos a los que seguir (en
modo dios o caminando en primera persona con Worldfall) porque les ha pasado algo importante.

**Solo lee el mundo.** No usa Harmony ni escribe en la partida. Toma una foto del mundo cada pocos
segundos y la compara con la anterior. Si WorldBox o Worldfall se actualizan, lo peor que puede
pasar es que falle la foto: verás un `[WorldfallExp] foto_...` en `Player.log` y el juego seguirá.

## Qué búsquedas hay

| Tipo | Cuándo se abre | Se cumple | Falla |
|---|---|---|---|
| **El trono de X** | Muere un rey con hijos adultos y el trono queda vacío | El heredero pasa a ser rey de X | El heredero muere, otro se corona o el reino cae |
| **El hijo de X** | Muere un rey o líder de ciudad con hijos pequeños | El niño llega a adulto | El niño muere |
| **Sin gobierno tras X** | Muere el líder de una ciudad y la ciudad queda sin líder | La ciudad tiene un líder nuevo | La ciudad desaparece |
| **Amantes separados** | Una pareja que vivía en el mismo reino acaba en reinos distintos (guerra, conquista, migración) | Vuelven a vivir en el mismo reino | Uno muere o dejan de ser pareja |
| **Venganza por X** (PeceraWB) | La pecera anota un regicidio | Muere el asesino | — |
| **El desterrado X** (PeceraWB) | La pecera destierra a alguien (Nivel 3) | Encuentra una ciudad nueva | Muere |
| **El pretendiente de X** (PeceraWB) | Sucesión disputada: el que perdió | Se corona en algún reino | Muere |
| **El heredero desposeído de X** (cadena) | Una búsqueda de trono falla porque otro se corona | Se corona en algún reino | Muere |
| **Caza: X** (poder Bestia legendaria) | Sueltas una bestia legendaria | La bestia muere | — |

Todas **caducan** a los `busquedas_caducidad_seg` segundos de mundo. Al cargar una partida, la
primera foto es la **línea base**: lo que ya estaba así antes no se convierte en búsqueda.

## Instalación

Copia **el contenido** de `worldfall-expansion/` (`mod.json` y `Code/`) sobre tu carpeta actual del
plugin en `Mods\`. **No copies `dev/`**: son las pruebas y no van al juego. NML recompila el mod al
arrancar el juego.

## Configuración

Fichero: `LocalLow\mkarpenko\WorldBox\WorldfallExpansion\config.txt`. Se crea al primer arranque
y las claves nuevas se añaden solas sin tocar tus valores.

```ini
activo=1
busquedas=1                  # interruptor general de la feature
busquedas_intervalo_seg=10   # segundos REALES entre fotos (mínimo 2)
busquedas_max_activas=5      # 1..20
busquedas_max_nuevas=2       # por foto (en una conquista no te llueven 40 avisos)
busquedas_caducidad_seg=900  # segundos de MUNDO
busquedas_avisos=1           # aviso arriba (WorldTip) al abrir, cumplir o fallar
busquedas_trono=1
busquedas_huerfanos=1
busquedas_ciudades=1
busquedas_amantes=1
busquedas_tecla=F8           # lista de búsquedas activas (F9/F10 son de PeceraWB)
```

Registro: `WorldfallExpansion\mundos\<mundo>\busquedas.jsonl`, con una línea por búsqueda abierta,
cumplida, fallida o caducada. Se rota al pasar de 2 MB.

## Cómo probarlo en tu partida (10 min)

1. **Haz una copia de la partida.** No hace falta porque el plugin no escribe en ella, pero es la regla.
2. Arranca el juego. En `Player.log` deben salir:
   - `[WorldfallExp] v0.2.0 cargado ... (F8 lista de busquedas)`
   - al entrar al mundo, `busquedas: mundo '...' (linea base en la proxima foto)`
   - ningún `error CS` al compilar el mod.
3. **Trono:** busca un rey con hijos adultos (pestaña de familia en su ventana) y mátalo con un
   poder. En menos de 10 s debe salir **«Nueva busqueda: El trono de …»**. Cuando el juego elija
   nuevo rey verás «cumplida» (si es el heredero) o «fallida» (si es otro).
4. **Ciudad:** mata al líder de una ciudad que no sea rey. Debe salir «Sin gobierno tras …»
   y cerrarse cuando la ciudad elija líder.
5. Pulsa **F8**: lista de búsquedas activas.
6. Abre `busquedas.jsonl` y comprueba que hay una línea por evento.
7. Pon `busquedas=0` y reinicia: no debe aparecer nada.
8. Si algo falla, mándame las líneas `[WorldfallExp]` de `Player.log`.

## Límites conocidos (v0.2.0)

- **Guardado entre sesiones (desde v4.0):** `busquedas_activas.jsonl`. Las de ciudad no se guardan, porque
  la ciudad no tiene un id estable. Las de trono se vuelven a enlazar por el nombre del reino.
- **Brújula:** desde la v3.0 (`current_position`, verificada en el binario de la build 719).
- **Cómo se detecta una muerte.** Una unidad cuenta como muerta cuando desaparece de
  `units.getSimpleList()` y su `isAlive()` es falso. Si una sigue viva fuera de la lista
  (por ejemplo, embarcada), se sigue contando como viva.
- **El aviso de la lista F8 usa saltos de línea.** No se ha verificado que `WorldTip` los muestre:
  en el peor caso sale todo en una línea.

## Para desarrolladores

```bash
dev/check.sh   # tests del Core + el mod entero en C# 5 contra stubs (requiere .NET SDK 8)
```

`dev/stubs/` declara **solo** las firmas verificadas de la build 719. Si el código usa algo que no
está ahí, la comprobación falla (`error CS1061`). Es la regla «prohibido adivinar firmas» puesta
como test. Lo mismo corre en GitHub Actions en cada push.
