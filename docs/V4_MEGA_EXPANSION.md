# Mega expansión v4.0 — Worldfall Expansion 4.0.0 + PeceraWB 0.2.0

Todo sigue siendo **mods NML separados**. `Worldfall.dll` no se toca y no se parchea ninguna clase
de Worldfall. La expansión crece **añadiendo contenido al juego**, y Worldfall lo usa solo porque
lee los catálogos del juego (comprobado en su código: `Crafting.LoadRecipes`, `Hud`, `Creatures`,
`Dialogue.GameLine`).

## Qué hay nuevo

### Fase 1 — Arsenal y rasgos (`contenido=1`)
- **13 armas y armaduras** que puedes fabricar en primera persona con **I** usando recursos del
  mundo: espada de obsidiana, espada del rey caído, hacha de guerra enana, lanza del cazador,
  martillo del trueno, arco élfico, arco de hueso de dragón, armadura de escamas de dragón,
  coraza juramentada, capucha del rastreador, yelmo del heredero, botas del peregrino y grebas
  de obsidiana.
- **3 anillos y amuletos** (del obelisco, de la pecera y del luto) en el panel de regalos de Worldfall.
- **12 rasgos** que se pueden dar: Juramentado, Maldito, Bendecido por el Obelisco, Chismoso,
  Vengador, Desterrado, Pretendiente, Corazón roto, Peregrino, Cazador de bestias, Mentor y Voz
  del pueblo.
- Los nombres están en `Locales/es.json` y `en.json`. NML los carga y los vuelve a aplicar si
  cambias de idioma.

### Fase 2 — La pecera en primera persona
- **HUD social** (`social=1`): bajo la brújula ves lo que la pecera sabe de quien tienes más
  cerca: casa, rasgos, amigo, rival, sueño y el último rumor. PeceraWB lo exporta cada 10 s
  (`exporta_social=1`).
- **Búsquedas 2.0:**
  - se guardan al cerrar el juego y siguen al volver;
  - forman cadenas: un trono usurpado abre **«El heredero desposeído»**;
  - **recompensas reales** de renombre y rasgos (`busquedas_recompensas=0` por defecto; escribe
    en el mundo, con tope por hora y `recompensas.log`).
- **99 frases de recuerdos** (33 tipos × 3, en español e inglés) que los PNJ de Worldfall dicen en sus charlas («Háblame de tu vida»).
  Nunca sustituyen a las del juego.

### Fase 3 — Poderes y bestias (`poderes=1`)
Hay una pestaña nueva, **«Worldfall Expansion»**, en la barra de poderes:

| Poder | Efecto |
|---|---|
| Bestia legendaria | Suelta un lobo, oso, cocodrilo, rinoceronte, serpiente, hiena, escorpión o búfalo **gigante y con nombre propio** («El Lobo Blanco de las Colinas»). Lo ves con el modelo 3D de Worldfall y se abre una búsqueda de caza |
| Bendición del Obelisco | Rasgo «Bendecido» y +5 de renombre |
| Maldición | Rasgo «Maldito» |
| Juramento | Rasgo «Juramentado» |
| Destierro divino | Pierde su ciudad y recibe el rasgo «Desterrado» (nunca a un rey) |
| Sembrar discordia | En la pecera: se pelea con su mejor amigo |
| Reconciliar | En la pecera: hace las paces con su rival |

Cada poder queda anotado en la crónica de la pecera (`intervenciones=1` en PeceraWB).

**Por qué bestias y no especies nuevas:** Worldfall solo tiene modelo 3D para las especies que
conoce. Una especie clonada se vería en vóxeles, peor que las de ahora, y clonar especies después
de que el juego haya cargado puede romper partidas. Las bestias legendarias usan especies con
modelo.

### Fase 4 — Nivel 3 avanzado de la pecera
Todo escribe en el mundo: exige `nivel3=1` y además su interruptor propio, que viene apagado.

| Clave | Qué hace |
|---|---|
| `nivel3_guerras=1` | Dos reyes que se odian (sentimiento ≤ −`nivel3_guerra_odio`) entran en guerra |
| `nivel3_guerras_civiles=1` | Si la facción del rey choca con otra cuyo líder manda una ciudad que no es la capital, esa ciudad se alza como reino propio y empieza una rebelión |
| `nivel3_rasgos=1` | La pecera marca a la gente con rasgos del Arsenal: desterrado, pretendiente, corazón roto |

Todas comparten el tope `nivel3_max_hora` y quedan registradas en `nivel3.log`.

## Instalación (con el juego cerrado)

1. Haz una **copia de tu partida.**
2. Copia el zip **worldfall-expansion** (`mod.json`, `Code/`, `Traducciones/`, `Locales/`) sobre tu
   carpeta del plugin en `Mods\`.
3. Copia el zip **PeceraWB** (`mod.json`, `Code/`) sobre `Mods\PeceraWB\`.
4. Arranca. En `Player.log` deben aparecer:
   - `[WorldfallExp] contenido: 16/16 objetos y 12/12 rasgos en el juego`
   - `[WorldfallExp] poderes: 7/7 registrados` y `pestana «Worldfall Expansion» creada`
   - `[WorldfallExp] frases: N frases de recuerdos nuevas`
   - `[PeceraWB] v0.2.0 cargado`
   - **ningún** `error CS` al compilar ninguno de los dos mods.

## Prueba en partida (unos 30 min)

| # | Qué hacer | Qué debe pasar |
|---|---|---|
| 1 | En primera persona pulsa **I** | Recetas nuevas (espada de obsidiana, arco élfico…) con su coste |
| 2 | Recoge 8 de piedra y 1 gema y fabrica la espada de obsidiana | Aparece en tu mano con el sprite de la espada de acero |
| 3 | Abre la pestaña «Worldfall Expansion» y usa **Bestia legendaria** | Aviso «X despierta…» y búsqueda «Caza: X». Entra en primera persona y la brújula te lleva hasta ella |
| 4 | Usa **Bendición** sobre alguien | Tiene el rasgo «Bendecido por el Obelisco»; en el informe de la pecera (F9) aparece «Los dioses bendijeron a…» |
| 5 | Camina cerca de gente con PeceraWB activa | Bajo la brújula aparecen su casa, amigos y rumores |
| 6 | Habla con un PNJ: «Háblame de tu vida» | Alguna de las frases nuevas («¿Por qué tú y no yo?»…) |
| 7 | Cierra y vuelve a abrir la partida | `busquedas: N guardadas de la sesion anterior` y las búsquedas siguen |
| 8 | **Opcional (escribe):** `nivel3=1` y `nivel3_guerras=1` en PeceraWB | Con el tiempo, guerras entre reyes que se odian; queda en `nivel3.log` |

Si algo falla, mándame las líneas `[WorldfallExp]` y `[PeceraWB]` de `Player.log`.

## Lo que no está probado en partida (y su riesgo)

- **Objetos clonados.** Se clonan como el propio juego clona los suyos. Riesgo: que el
  sprite en la mano no se cargue en alguno; en ese caso el objeto funciona igual, pero sin dibujo.
- **Partidas guardadas.** Si una partida guarda unidades con objetos o rasgos de la expansión y
  luego quitas el mod, el juego tendrá que tratar ids que no conoce. **No quites el mod de una
  partida que lo haya usado** sin una copia.
- **La pestaña de poderes** usa la API de NML para pestañas, que es la que usan otros mods de la 719.
- **Guerras civiles.** Llaman por reflexión a dos métodos internos del juego. Si el juego cambia,
  la función se apaga sola, pero es la parte más delicada: pruébala con una copia de la partida.
- **Las fuentes de Worldfall** y las tildes de los textos nuevos.

## Comprobado fuera del juego

- **Tests:** `dev/check.sh` pasa 180 comprobaciones del plugin y 25 de PeceraWB.
- **Compilación:** **los dos mods** compilan en C# 5 contra stubs que contienen solo firmas leídas
  de los binarios de la build 719. Si alguien usa una firma no verificada, la compilación falla.
- **GitHub Actions** lo ejecuta en cada push.
