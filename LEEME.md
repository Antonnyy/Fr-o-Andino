# FRIO ANDINO: Sobrevivir a la Helada
### Proyecto Unity 2D completo y jugable — Universidad Continental

Ahora el juego usa **tu arte**: tu fondo del caserío, tu personaje Inti y tu menú.

---

## COMO ABRIRLO Y JUGAR

1. Descomprime la carpeta `FrioAndino`.
2. **Unity Hub → Add → Add project from disk →** elige la carpeta `FrioAndino`.
3. Si te pide versión del Editor, elige **cualquiera que ya tengas instalada** (2021.3,
   2022.3 LTS o Unity 6). Ese aviso sale con todo proyecto de Unity, no es un error.
4. Abre `Assets/Scenes/FrioAndino.unity` y presiona **PLAY**.

Sale primero **tu menú** → [ESPACIO] o clic en *Nueva Partida* → tutorial → a jugar.

> No hay nada que configurar: ni URP, ni Input System, ni TextMeshPro, ni importar sprites,
> ni armar la escena. Todo el caserío se monta solo al darle Play (`GameBootstrap.cs`).

---

## TU ARTE DENTRO DEL JUEGO

Las imágenes viven en `Assets/Resources/FrioAndino/` como archivos `.bytes` (PNG crudo).
Se cargan en runtime con filtro **Point** y sin compresión, así el pixel art se ve nítido
sin depender de la configuración del importador de Unity.

| Archivo | Qué es | Cómo se usa |
| :--- | :--- | :--- |
| `fondo.bytes` | Tu caserío (le quité el HUD dibujado y el comunero suelto del centro) | Escenario completo del juego |
| `inti.bytes` | Tu personaje, recortado del fondo y del pedestal | Jugador + comuneros (con variación de color) |
| `menu.bytes` | Tu pantalla de título | Menú principal, con zonas clicables encima |
| `vidrio.bytes` | El panel de vidrio recortado de tu propia casa Trombe | Se monta sobre las casas al construir el Muro Trombe |
| `ichu_techo.bytes` | La paja del techo, recortada de tu fondo | Se monta al sellar una vivienda con ichu |
| `alpaca.bytes` / `alpaca_cria.bytes` | Alpacas detalladas en pixel art | Rebaño que se arrea al cobertizo |
| `yachachiq_notebook.bytes` | Cuaderno del Yachachiq | Guía técnica y diagnóstico en vivo ([J]) |
| `termometro_hud.bytes` | Termómetro andino con hielo | Indicador dinámico de helada en el HUD |
| `menu_recursos.bytes` | Infografía de Recursos | Pestaña [1] en menú de sistemas |
| `menu_secundarios.bytes` | Clima y Pastoreo | Pestaña [2] en menú de sistemas |
| `menu_progresion.bytes` | Progresión Muro Trombe | Pestaña [3] en menú de sistemas |

**El mundo ES tu imagen**: cada objeto interactivo está colocado en las coordenadas en
píxeles de tu fondo, así que la casa que ves es exactamente la casa con la que interactúas.

| En tu fondo | En el juego |
| :--- | :--- |
| Casa de adobe de la izquierda | Casa de la familia Wari — nivel 1, hay que sellarla |
| Casa grande con pared de vidrio | Casa demostrativa de Inti — **ya tiene Muro Trombe**, es el modelo |
| Casa pequeña con ropa tendida | Casa de la familia Quispe — nivel 1 |
| Fogón central con la gente sentada | Fogón comunal (consume leña) + Doña Tomasa y Don Mariano |
| Cobertizo de piedra de la derecha | Cobertizo de alpacas — 3 niveles de mejora |
| Alpacas pastando a la izquierda | El rebaño que tienes que arrear |

---

## CONTROLES

| Tecla | Acción |
| :--- | :--- |
| `A` / `D` | Mover a Inti por el caserío |
| `Shift` | Correr (gasta ALIENTO — fatiga por los 4,200 m s.n.m.) |
| `Espacio` | Salto |
| `E` | Interactuar: cosechar, mejorar casas, avivar el fogón, pedir mates |
| `Q` | **Pututo**: convoca ronderos para las faenas (ayni) |
| `T` | Adelantar al anochecer cuando ya terminaste el día |
| `M` / `N` | Silenciar música / efectos |
| `R` | Reiniciar (en la pantalla final) |

---

## CÓMO SE JUEGA

**DE DÍA (120 s)**
1. Cosecha **ichu** en la puna, **leña** junto al almacén, **hierbas** en las laderas y
   **piedra** en las rocas del este.
2. Toca el **pututo `[Q]`**: los ronderos cercanos te siguen.
3. Párate frente a una casa y pulsa **`E`**. Con cuadrilla la faena va el triple de rápido.
4. Aviva el **fogón** con leña. Si se apaga, la moral se desploma y verás las cenizas frías.
5. **Antes del atardecer** arrea las alpacas hacia el cobertizo: camina hacia ellas y
   huyen en la dirección en que las empujas.

**DE NOCHE (25 s)** — el juego calcula, casa por casa:

```
T_interior = T_exterior + (Rt x 26) + calor del fogón
```

Bajo 0 °C los comuneros enferman (IRA). Bajo −6 °C un enfermo puede fallecer. Las alpacas
fuera del cobertizo mueren por congelamiento — **las crías primero**, como en la realidad
altoandina.

**AL AMANECER** sale el *Reporte del Amanecer* con el balance ODS 3 / ODS 13.

### Curva térmica

| Día | Día | Noche | Foco |
| :-- | :-- | :-- | :-- |
| 1 | +8 °C | −3 °C | Cosechar ichu y sellar la casa de los Wari |
| 2 | +6 °C | −6 °C | Primeros resfríos; techar el cobertizo |
| 3 | +4 °C | −10 °C | Rebaño en riesgo; empezar un Muro Trombe |
| 4 | +2 °C | −14 °C | Racionar leña y curar enfermos |
| 5 | 0 °C | **−20 °C** | **LA GRAN HELADA** |

### Victoria y derrota
- **Victoria**: amanecer el Día 6 con **≥60 % del rebaño** y **cero muertes humanas**
  → *"Comunidad Altoandina Resiliente"*.
- **Derrota**: 3 fallecimientos (ODS 3), perder más del 50 % del rebaño (ODS 13), o moral en 0 %.

### Mejoras bioclimáticas
| Estructura | Nivel 1 | Nivel 2 | Nivel 3 |
| :--- | :--- | :--- | :--- |
| Vivienda | Adobe sin sellar (Rt 0.15) | Sellado con ichu (Rt 0.48) | **Muro Trombe** (Rt 0.88) |
| Cobertizo | Pirca abierta (0 alpacas) | Techo y cama de ichu (9) | Módulo cerrado para crías (20) |

Los tres puntitos sobre cada construcción indican su nivel.

---

## ESTRUCTURA DEL CÓDIGO

```
Assets/
├── Resources/FrioAndino/     ← TU ARTE (.bytes = PNG crudo)
└── Scripts/
    ├── Core/
    │   ├── GameBootstrap.cs          ← monta el caserío sobre tu fondo al dar Play
    │   ├── GameManager.cs            ← 5 días, victoria/derrota ODS
    │   ├── TimeTemperatureManager.cs ← reloj solar + curva térmica
    │   ├── HealthShelterManager.cs   ← balance nocturno: salud (ODS 3) y ganado (ODS 13)
    │   └── WeatherFX.cs              ← tinte día/noche, nieve, escarcha
    ├── Art/
    │   ├── GameArt.cs                ← carga tu arte y convierte píxeles del fondo a mundo
    │   └── PixelArtFactory.cs        ← iconos y props generados por código
    ├── Player/       PlayerController2D · PlayerInteraction · CameraFollow2D · IInteractable
    ├── Environment/  ThermalBuilding · Campfire · HealthPost
    ├── Economy/      ResourceManager · ResourcePickup
    ├── Villagers/    VillagerAI · VillagerTaskSystem · AlpacaAI
    ├── UI/           HUDController · MainMenuController
    └── Audio/        AudioSynth (música andina sintetizada) · AudioDirector
```

### Decisiones técnicas (por si el profesor pregunta)
- **Sin dependencias externas**: no usa URP, Input System, TextMeshPro ni Cinemachine, para
  que abra y corra en cualquier instalación de Unity sin configurar paquetes.
- **Tu arte manda la escala**: `GameArt` define el mapeo `píxel del fondo → unidad de mundo`
  y una escala "cover" que hace que el caserío llene la pantalla en 16:9, 21:9 o 4:3.
- **Audio procedural**: `AudioSynth` sintetiza quena, zampoña, charango, bombo y el pututo
  con osciladores — cero archivos de audio que descargar.
- **La mecánica ES el mensaje**: el ODS 13 se juega arreando alpacas contra reloj y el ODS 3
  se juega decidiendo si gastas el ichu en tu casa o en la del vecino. La fórmula térmica
  es visible en el reporte de cada amanecer.

---

## AJUSTAR LA DIFICULTAD

Con el juego en Play, selecciona `== FRIO ANDINO == > Managers` en la jerarquía:
- `TimeTemperatureManager`: duración del día/noche y las curvas de temperatura.
- `GameManager`: `maxHumanDeaths`, `minHerdRatio`, `victoryHerdRatio`.
- `HealthShelterManager`: `sickThreshold`, `lethalThreshold`, `alpacaLethalTemp`.

---

## BUILD PARA ENTREGAR

**File → Build Settings → PC, Mac & Linux Standalone → Windows → Build.**
La escena `Assets/Scenes/FrioAndino.unity` ya está en la lista de build.
