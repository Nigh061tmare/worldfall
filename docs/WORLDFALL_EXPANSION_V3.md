# Worldfall Expansion 3.0

Mod NML compañero de **Worldfall** (beta 0.9.2) para WorldBox build 719.
**No toca `Worldfall.dll`.** Todo lo que lee de Worldfall o de Unity va por reflexión o por la API
pública, y cada pieza falla por separado. Si Worldfall se actualiza, lo peor que puede pasar es
que algo vuelva al inglés o que la brújula funcione solo en modo dios.

## Qué trae

| Función | Qué hace | Escribe en el mundo | Interruptor |
|---|---|---|---|
| **Worldfall en español** | Traduce 2.230 textos de Worldfall: diálogos, misiones, consejo real, mapa de guerra, noticias, ajustes, ayuda de teclas, habilidades… | No | `traduccion=1` |
| **Crónica → Búsquedas** | El mundo abre búsquedas a seguir: heredero al trono, hijo de un difunto, ciudad sin líder, amantes separados (ver `BUSQUEDAS.md`) | No | `busquedas=1` |
| **Puente con PeceraWB** | Los regicidios, destierros y sucesiones disputadas de la pecera abren búsquedas: venganza, desterrado, pretendiente | No | `pecera_busquedas=1` |
| **Brújula** | Línea arriba de la pantalla con la búsqueda que sigues, la distancia y el rumbo. En primera persona dice hacia dónde girar | No | `brujula=1` |

Teclas: **F8** lista las búsquedas y **F7** cambia la búsqueda que sigue la brújula; las dos se
pueden cambiar en `config.txt`. F9 y F10 son de PeceraWB y F1, F2 y F5–F9 son de Worldfall en
primera persona. Si F7 o F8 chocan con algo, cámbialas.

## Instalación

1. Cierra el juego.
2. Copia `worldfall-expansion/mod.json`, `Code/` y **`Traducciones/`** sobre tu carpeta del plugin en
   `Mods\`. `dev/` no se copia: son las pruebas.
3. Arranca. En `Player.log` deben aparecer:
   - `[WorldfallExp] traduccion: 1550 frases y 678 plantillas de 1 fichero(s); 4/4 puertas de texto parcheadas`
   - `[WorldfallExp] puente pecera: PeceraWB encontrada`
   - `[WorldfallExp] v3.0.0 cargado ...`
   - al entrar en primera persona por primera vez: `[WorldfallExp] puente: Worldfall encontrado (Worldfall ...)`

## Cómo funciona la traducción, y por qué sobrevive a las actualizaciones

Worldfall escribe sus textos en inglés dentro del código y los pinta con el IMGUI de Unity. El
plugin parchea las **cuatro puertas públicas de Unity** por las que pasan esos textos, no clases
de Worldfall:

- `GUI.Label(Rect, string, GUIStyle)`
- `GUI.Button(Rect, string, GUIStyle)`
- `new GUIContent(string)`
- `GUIContent.text`

Las dos últimas sirven para que Worldfall mida el ancho con el texto ya traducido.

Para cada texto busca primero la frase exacta y, si no, una **plantilla**:
`The army of {0} gathers: {1} of {2}.` se convierte en `El ejército de {0} se reúne: {1} de {2}.`.
Los huecos pueden cambiar de orden y lo que va dentro se traduce también (por ejemplo, el rumbo
«North-east» sale como «noreste»). Lo que no está en el fichero sale en inglés tal cual.

- **Tus correcciones:** escríbelas en `LocalLow\mkarpenko\WorldBox\WorldfallExpansion\traduccion_extra.txt`,
  con el mismo formato `Inglés => Español`. Mandan sobre el fichero del mod.
- **Qué falta por traducir:** pon `traduccion_registro=1` y juega un rato. En
  `traduccion_faltan.txt` aparecerá cada texto que salió en inglés, listo para rellenar.
- **Cuando Worldfall se actualice:** `dev/herramientas/textos_nuevos.py` lista los textos nuevos
  (las instrucciones están dentro del script).
- **Lo que no cambia:** los textos de WorldBox los traduce el propio juego si lo tienes en español.
  Las ventanas normales de WorldBox usan otra tecnología (uGUI), y la traducción no las toca.

## Brújula

```
[1/3] El trono de Norte · HIJO: 42 casillas al noreste · delante a la izquierda
```

- **En primera persona:** lee de Worldfall `WorldBoxMod.Instance.IsFirstPerson`, `.Host` y
  `.ViewYaw`. Son propiedades públicas que ya existen en Worldfall 0.9.2, leídas por reflexión.
- **En modo dios:** mide desde el centro de la cámara.
- **Si izquierda y derecha salen al revés:** pon `brujula_invertir=1`. El convenio se ha deducido
  del código de Worldfall, pero no se ha visto en pantalla.
- **Altura de la línea:** `brujula_altura`, de 0 (arriba) a 1.

## config.txt (claves nuevas)

```ini
pecera_busquedas=1     # hitos de PeceraWB -> búsquedas
brujula=1
brujula_tecla=F7
brujula_altura=0.07
brujula_invertir=0
traduccion=1
traduccion_registro=0
```

## Qué está probado y qué no

- **Comprobado aquí:**
  - 89 comprobaciones del Core (búsquedas, brújula, lector de la pecera, traductor y el fichero
    real de traducciones: las 680 plantillas resuelven sin choques);
  - el mod entero compila en C# 5 contra stubs que contienen solo firmas leídas del binario de
    la build 719;
  - todo corre en GitHub Actions en cada push.
- **Leído en los binarios** (descompilados en la sesión, nunca subidos):
  - `BaseSimObject.current_position`, `World.world` (un `MapBox`), `MapBox.map_stats`
    (`internal`) y `WorldTip.showNow` con todos sus parámetros;
  - `BasicMod.GetDeclaration().FolderPath` de NML;
  - la API pública de Worldfall y sus llamadas a IMGUI.
- **No probado en partida:**
  - que IMGUI de Unity 2022.3 acepte los cuatro parches. Se espera que sí, porque son métodos
    públicos estándar;
  - que las fuentes de Worldfall muestren tildes y la «ñ»;
  - el lado izquierda/derecha de la brújula;
  - el rendimiento con mundos muy grandes.

### Prueba en partida (15 min)

1. **Arranque:** los mensajes de log de arriba.
2. **Traducción:** entra en primera persona. La ayuda (F1), los ajustes (F2) y las charlas (E)
   deben salir en español. Si ves cuadrados en lugar de tildes, dímelo: es la fuente.
3. **Búsqueda y brújula:** mata a un rey con hijos adultos y aparece la búsqueda. Entra en primera
   persona: la brújula debe decir distancia y giro. Camina hacia donde dice y comprueba que la
   distancia baja.
4. **Pecera:** con `nivel3=1` en PeceraWB, cuando haya un destierro debe abrirse «El desterrado …».
5. **Registro:** pon `traduccion_registro=1`, juega 10 min y mándame `traduccion_faltan.txt`.
