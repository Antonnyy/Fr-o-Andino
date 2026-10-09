# Presentación Académica del Proyecto: Frío Andino — Sobrevivir a la Helada
**Asignatura:** Diseño y Desarrollo de Videojuegos / Metodologías Ágiles  
**Institución:** Universidad Continental  
**Objetivos de Desarrollo Sostenible (ODS):** ODS 13 (Acción por el Clima) y ODS 3 (Salud y Bienestar)  
**Tablero Ágil:** [Trello Oficial del Proyecto](https://trello.com/b/ESfDSfJP/agil-proyecto-management-game-dev-elada-andina)

---

## Estructura General de Diapositivas (Modelo de Evaluación 5 Slides)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  SLIDE 1: Portada y Alineación ODS (Título, Logotipo, ODS 13 y 3, Equipo)    │
├──────────────────────────────────────────────────────────────────────────────┤
│  SLIDE 2: Concepto del Videojuego (Problemática, Propósito, Core Loop)      │
├──────────────────────────────────────────────────────────────────────────────┤
│  SLIDE 3: Mecánicas de Juego (6 Cajas Estructuradas según GDD)              │
├──────────────────────────────────────────────────────────────────────────────┤
│  SLIDE 4: Historia y Mundo (Narrativa, Lore Andino, Paleta de Color)        │
├──────────────────────────────────────────────────────────────────────────────┤
│  SLIDE 5: Herramientas y Desarrollo (Stack Tecnológico, Enlaces, Trello)     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Diapositiva 1: Portada del Proyecto
*Referencia Visual:* `media_1790913791421.png`

### 1. Elementos Visuales y Distribución
- **Fondo:** Puna andina al amanecer con gradiente cálido sobre la cordillera nevada y textura de escarcha sutil.
- **Lado Izquierdo / Central:**
  - **Título Principal:** `FRÍO ANDINO` (Tipografía Bold, estilo rústico moderno andino).
  - **Subtítulo:** `Sobrevivir a la Helada` (Tipografía sans-serif limpia, color dorado Inti `#E5A93C`).
  - **Lema del Videojuego:** *"Protege tu comunidad, abriga a tu rebaño, vence a la helada con el poder del Ayni".*
  - **Badges Oficiales ODS:**
    - **ODS 13:** Acción por el Clima *(Mitigación, adaptación bioclimática y resiliencia andina)*.
    - **ODS 3:** Salud y Bienestar *(Prevención de neumonía e hipotermia mediante medicina tradicional)*.
- **Lado Derecho:**
  - Ilustración de **Inti** (Yachachiq comunero con chullo tradicional, poncho y pututo en mano).
- **Pie de Página:**
  - **Universidad:** Universidad Continental.
  - **Integrantes:** [Nombres de los integrantes del equipo].
  - **Docente / Asignatura:** Diseño y Desarrollo de Videojuegos.
  - **Fecha:** Octubre 2026.

---

## Diapositiva 2: Concepto del Videojuego
*Referencia Visual:* `media_1790913823144.png`

### 1. Columna Izquierda: Problemática, Propósito y Referentes
- **Problemática Real:**
  - En las zonas altoandinas del Perú (sobre los 4,000 m s.n.m., como Puno y Huancavelica), las olas de helada extrema descienden por debajo de los **-15 °C**.
  - Provocan alta mortalidad en alpacas y crías (principal sustento económico) y graves cuadros de neumonía e hipotermia en niños y adultos mayores.
- **Propósito y Propuesta de Valor:**
  - Sensibilizar y educar a través de una experiencia lúdica inmersiva 2D donde el jugador aplica **soluciones bioclimáticas reales** (Muros Trombe, sellado de techos con ichu, cobertizos térmicos) y el principio ancestral del **Ayni** (reciprocidad comunitaria).
- **Inspiraciones y Referentes:**
  - **Frostpunk:** Gestión de calor, microgestión de supervivencia y dilemas comunitarios ante el frío extremo.
  - **Kingdom Two Crowns:** Exploración lateral 2D en scroll continuo, liderazgo activo y defensa territorial sin combate directo de armas.
  - **Terraria / Starbound:** Recolección ágil de recursos naturales y construcción modular contextual.

### 2. Columna Derecha: Escenario y Diagrama del Core Loop
- **Escenario:**
  - Caserío altoandino a 4,200 m s.n.m. Viviendas de piedra y adobe, corrales rústicos y pastizales de ichu expuestos a ventiscas heladas.
- **Diagrama del Core Loop (Bucle de Juego):**
```
      ┌──────────────────────────────────────────────────────────┐
      │               FASE DIURNA: PREPARACIÓN Y AYNI            │
      │  Explorar Caserío ➔ Recolectar (Ichu/Leña) ➔ Construir   │
      │  Muros Trombe ➔ Arrear Alpacas con Silbido al Cobertizo  │
      └────────────────────────────┬─────────────────────────────┘
                                   │ (Cae la noche a las 18:00)
                                   ▼
      ┌──────────────────────────────────────────────────────────┐
      │             FASE NOCTURNA: RESISTENCIA TÉRMICA           │
      │  Vientos a -15°C ➔ Alimentar Fogones ➔ Posta Médica:     │
      │  Repartir Mates de Muña ➔ Proteger Vidas Comunales       │
      └────────────────────────────┬─────────────────────────────┘
                                   │ (Amanecer a las 06:00)
                                   ▼
      ┌──────────────────────────────────────────────────────────┐
      │                 EVALUACIÓN DEL AMANECER                  │
      │  Cálculo de Supervivencia: Salud Comunal y Ganado Vivo   │
      └──────────────────────────────────────────────────────────┘
```

---

## Diapositiva 3: Mecánicas de Juego
*Referencia Visual:* `media_1790913837275.png` (Las 6 Cajas del GDD)

| Mecánica | Descripción Detallada | Teclas / Input |
|---|---|---|
| **1. Movimiento del Jugador** | Desplazamiento lateral 2D suave. Sprint con penalización por fatiga y altitud (4,200 m s.n.m.). Reducción de agilidad al descender la temperatura corporal. | `A` / `D` (Caminar)<br>`Shift` (Trote/Sprint)<br>`Espacio` (Salto breve) |
| **2. Sistema de Interacción** | Radio de acción contextual para recolección de materiales, activación de obras bioclimáticas, encendido de fogones y toque del **Pututo** para convocar la faena comunal. | `E` (Interactuar / Recoger / Construir)<br>`Q` (Silbido / Arreo alpacas)<br>`F` (Toque de Pututo) |
| **3. Sistema de Progresión** | Árbol de mejoras de resiliencia comunal: Cabaña básica ➔ Techo aislado con Ichu ➔ **Muro Trombe Solar Pasivo** (+8°C interior) ➔ Cobertizo térmico para ganado. | Niveles I, II y III de Aislamiento Térmico |
| **4. Mecánica de Recursos** | Economía ecológica comunitaria: **Ichu** (aislante térmico), **Leña** (combustible de fogón), **Piedras** (masa térmica Trombe) y **Muña / Eucalipto** (medicina preventiva). | Inventario comunal en HUD superior |
| **5. Condición de Victoria / Derrota** | **Victoria:** Superar la ola de helada extrema manteniendo con vida a todos los comuneros vulnerables y al 80%+ de las alpacas.<br>**Derrota:** Muerte por hipotermia de un anciano o pérdida masiva del rebaño. | Amanecer a las 06:00 AM con balance positivo |
| **6. Mecánicas Secundarias** | **Libreta del Yachachiq:** Guía interactiva de tareas comunitarias y recomendaciones bioclimáticas.<br>**Física Térmica Dinámica:** Simulación en tiempo real de $T_{ext}$ vs $T_{int}$. | `Tab` (Abrir Libreta)<br>Indicador de Termómetro |

---

## Diapositiva 4: Historia y Mundo
*Referencia Visual:* `media_1790913849905.png`

### 1. Sinopsis Narrativa
> *"En las alturas de Huancavelica, los sabios ancianos leen las señales del cielo: se aproxima 'El Abrazo Blanco', una helada polar implacable. Inti, un joven elegido como Yachachiq (el que enseña y guía), debe recuperar la fuerza ancestral del Ayni para movilizar a su pueblo. No se trata de huir, sino de aprender a convivir con el frío mediante ciencia comunitaria y sabiduría andina."*

### 2. Lore y Cultura Andina
- **El Ayni y la Minka:** La supervivencia no es individual, es colectiva. La comunidad aporta mano de obra solidaria.
- **Cosmovisión y Fitoterapia:** Don Zenón y Doña Rosa representan la memoria viva de la comunidad, utilizando mates de muña y eucalipto para despejar los pulmones ante la amenaza de la neumonía.

### 3. Dirección de Arte y Paleta de Color
- **Paleta Diurna (Faena y Calidez):**
  - Ocre Andino (`#C28243`), Ichu Dorado (`#D4A247`), Terracota (`#8C3827`), Azul Celeste Puna (`#5B8FB9`).
- **Paleta Nocturna (Peligro y Aislamiento):**
  - Azul Glaciar (`#1A2849`), Violeta Helada (`#2B1A4A`), Cian Escarcha (`#7CE8FF`), Naranja Fuego Hogar (`#FF6B35`).
- **Estilo Visual:** Pixel art / 2D estilizado con iluminación dinámica 2D, viñetas de escarcha en bordes de pantalla y partículas de ventisca.

---

## Diapositiva 5: Herramientas y Desarrollo
*Referencia Visual:* `media_1790913945286.png`

### 1. Stack Tecnológico de Desarrollo
- **Motor de Videojuego:** Unity Engine 2022.3 LTS (Pipeline 2D, Universal Render Pipeline / 2D Renderer).
- **Lenguaje de Programación:** C# (.NET Framework) con patrones Singleton, Observer y máquinas de estado finito.
- **Gestión Ágil y Scrum:** Trello (Metodología ágil con sprints semanales, historias de usuario y tableros temáticos).
- **Diseño de UI/UX y GDD:** Figma / FigJam para wireframes, arquitectura de información y biblia de diseño.
- **Arte y Animación 2D:** Photoshop / Aseprite para spritesheets y panoramas de fondo en capas múltiples.
- **Diseño Sonoro y Mezcla:** Audacity y DAWs para paisajes sonoros andinos y efectos Fx acústicos.

### 2. Enlaces del Proyecto
- **Tablero Ágil Trello:** `https://trello.com/b/ESfDSfJP/agil-proyecto-management-game-dev-elada-andina`
- **GDD FigJam / Figma:** `[Enlace a FigJam del Proyecto]`
- **Repositorio GitHub:** `https://github.com/[usuario]/FrioAndino` *(Listo para carga final)*
- **Build Jugable:** Disponible en ejecutable local y WebGL.

### 3. Captura del Tablero Trello en Vivo
*(Insertar en el recuadro derecho de la diapositiva la captura oficial generada: `trello_board_frio_andino_final.png`)*
- Muestra las 6 listas activas:
  1. *Lista de tareas - Arte 2D* (6 tarjetas con checklists)
  2. *Lista de tareas - Sonido* (4 tarjetas de audio y ambiente)
  3. *Lista de tareas - Desarrollo* (7 tarjetas de mecánicas en C#)
  4. *Pendiente* (4 tarjetas de optimización y balance)
  5. *En proceso* (3 tarjetas de pulido e integración)
  6. *Terminado* (7 tarjetas completadas: Marcos Figma, GameManager, AudioManager, Cámara)
