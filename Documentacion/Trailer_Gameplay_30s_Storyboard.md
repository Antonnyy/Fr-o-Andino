# Modelo y Storyboard Técnico: Tráiler Gameplay de 30 Segundos
**Proyecto:** Frío Andino — Sobrevivir a la Helada  
**Formato de Video:** 1080p Full HD (1920x1080), 60 FPS, Relación de aspecto 16:9  
**Duración Exacta:** 30.00 segundos  
**Estilo de Edición:** Rápido, cinematográfico, con cortes en el pulso musical y textos de alto impacto  

---

## Estructura Temporal del Tráiler

```
00:00          00:05               00:12                00:20               00:26          00:30
  │              │                   │                    │                   │              │
  ▼              ▼                   ▼                    ▼                   ▼              ▼
┌──────────────┬───────────────────┬────────────────────┬───────────────────┬──────────────┐
│  1. EL GANCHO│  2. LA AMENAZA    │  3. EL AYNI DIURNO │ 4. EL CLÍMAX      │ 5. LOGO Y    │
│  (Hook &     │  (Descenso térmico│  (Recolección,     │    NOCTURNO       │    LLAMADO A │
│   Atmósfera) │   y vulnerabilidad)  Muro Trombe, Arreo) (Ventisca & Sol)  │    LA ACCIÓN │
└──────────────┴───────────────────┴────────────────────┴───────────────────┴──────────────┘
```

---

## Guion Técnico Segundo a Segundo (00:00 a 00:30)

| Tiempo | Video / Acción de Gameplay en Pantalla | Audio y Efectos Sonoros (SFX) | Texto Superpuesto (Titulares) |
|---|---|---|---|
| **00:00 - 00:03** | Pantalla en negro. Aparece una fecha en letras blancas sutiles. Se disuelve a un plano cerrado del termómetro del juego en -12°C con escarcha en los bordes. | Rugido sordo de viento gélido andino (`viento.wav`). Sonido de respiración pesada. | `A MÁS DE 4,200 M S.N.M.` |
| **00:03 - 00:06** | Corte rápido: Don Zenón tiritando frente a su cabaña. Una cría de alpaca tiembla en el corral expuesta al frío. | Sonido de tos seca (`tos.wav`) seguido por un balido suave de alpaca (`alpaca.wav`). | `UNA HELADA HISTÓRICA SE APROXIMA.` |
| **00:06 - 00:11** | Corte al centro del caserío: Inti se pone en marcha corriendo (`inti_run.png`). Recolecta rápidamente ichu y leña en el suelo. El HUD muestra los recursos aumentando. | Entra ritmo enérgico de charango y quena (`tema_dia.mp3`). Sonido de recolección (`pickup.wav`). | `ORGANIZA EL AYNI COMUNAL.` |
| **00:11 - 00:16** | Inti se detiene y sopla el Pututo (`inti_pututo.png`). Las alpacas corren en rebaño hacia el interior del cobertizo térmico. | Toque solemne y resonante de caracola (`pututo.wav`) con eco en los cerros. | `PROTEGE A TU REBAÑO.` |
| **00:16 - 00:21** | Inti se acerca a la cabaña y presiona `E`: Se construye rápidamente el **Muro Trombe** con efecto de partículas. El interior de la casa se ilumina con luz cálida. | Sonido de martilleo y ensamble de madera/vidrio (`construir.wav`). Crepitar acogedor de fuego (`fuego.wav`). | `CONSTRUYE MUROS TROMBE.` *(Tecnología Solar Pasiva)* |
| **00:21 - 00:25** | La iluminación del juego vira súbitamente a noche azulada profunda. Ráfagas de nieve cruzan la pantalla a gran velocidad. El caserío resiste: las ventanas brillan de naranja. | La música se vuelve intensa y percusiva. Rugido potente de ventisca. Al fondo, suena el suspiro de alivio de los comuneros. | `RESISTE LA NOCHE POLAR.` |
| **00:25 - 00:27** | El reloj marca las 06:00 AM. El cielo cambia a dorado amanecer (`amanecer.wav`). Sale el Sol sobre los nevados y los comuneros salen sonrientes. | Acorde triunfal de zampoña y campana de victoria. El viento se apaga por completo. | `EL SOL VUELVE A BRILLAR.` |
| **00:27 - 00:30** | Corte a placa final con fondo de la cordillera andina: Logotipo oficial de **FRÍO ANDINO: Sobrevivir a la Helada**. Badges de ODS 13 y ODS 3 en la parte inferior. | Remate musical andino y silencio ambiental sereno. | **FRÍO ANDINO**<br>*Sobrevivir a la Helada*<br>`ODS 13 • ODS 3 \| JUEGA LA DEMO` |

---

## Guía Paso a Paso para la Grabación y Edición

### 1. Grabación del Gameplay en Unity (Captura sin lag)
1. **Resolución en Unity:** En la ventana `Game`, fija la resolución en `1920x1080 (16:9)`.
2. **Software de Grabación:** Usa **OBS Studio** (o `SimpleScreenRecorder` / grabador de pantalla integrado) configurado a 60 FPS, tasa de bits de 10,000 Kbps.
3. **Tomas a Grabar (Gameplay Real):**
   - **Toma A (5 seg):** Personaje quieto o caminando con baja temperatura, mostrando a Don Zenón y alpacas.
   - **Toma B (8 seg):** Inti corriendo, recogiendo ichu (`E`) y sonando el pututo (`F`).
   - **Toma C (8 seg):** Construcción del Muro Trombe y entrada a la choza cálida.
   - **Toma D (8 seg):** Noche con ventisca activa y posterior transición al amanecer de las 06:00 AM.

### 2. Software de Edición Recomendado
- **Canva Video** (ideal para plantillas rápidas y textos animados con estilo)
- **CapCut / DaVinci Resolve / Premiere Pro** (para transiciones dinámicas y sincronización rítmica)

### 3. Pautas de Tipografía y Color para los Textos
- **Fuente recomendada:** Montserrat ExtraBold, Anton o Bebas Neue (letras mayúsculas, legibles y de impacto).
- **Color de Texto Principal:** Blanco puro (`#FFFFFF`) con sombra negra paralela o contorno sutil (`#101010`) para máxima legibilidad sobre la nieve.
- **Color de Énfasis:** Amarillo Dorado Inti (`#FFB800`) para palabras clave como *"AYNI"*, *"MUROS TROMBE"* y *"RESISTE"*.

### 4. Transiciones entre Tomas
- Cortes directos en seco (*Hard Cuts*) sincronizados con los golpes de bombo o rasgueo de charango.
- De 00:25 a 00:27: Transición tipo *Flash Blanco* o *Fade to Warm* para simular la salida del Sol andino.
