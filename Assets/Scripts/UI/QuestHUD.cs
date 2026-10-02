using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FrioAndino.Core;
using FrioAndino.Environment;
using FrioAndino.Villagers;
using FrioAndino.Audio;

namespace FrioAndino.UI
{
    /// <summary>
    /// Libreta del Yachachiq: Panel interactivo de misiones comunitarias diarias.
    /// Comunica de forma transparente y atractiva las metas de ODS 13 y ODS 3,
    /// brindando feedback inmediato de progreso al jugador (estilo Kingdom Two Crowns / Frostpunk).
    /// </summary>
    public class QuestHUD : MonoBehaviour
    {
        public static QuestHUD Instance { get; private set; }

        private Canvas _canvas;
        private RectTransform _panel;
        private Font _font;
        private Text _titleText;
        private Text _task1Text, _task2Text, _task3Text, _task4Text;
        private Text _footerText;

        private bool _task1Done, _task2Done, _task3Done, _task4Done;

        private static readonly Color32 PanelBg = new Color32(20, 24, 36, 230);
        private static readonly Color32 BorderGold = new Color32(230, 195, 95, 255);
        private static readonly Color32 TextCream = new Color32(245, 240, 225, 255);
        private static readonly Color32 DoneGreen = new Color32(110, 235, 150, 255);
        private static readonly Color32 PendingGold = new Color32(240, 210, 130, 255);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _font = HUDController.GetGameFont();
        }

        public void Build(Transform canvasTransform)
        {
            // Panel de Misiones en la esquina superior derecha, justo bajo la barra superior
            var go = new GameObject("QuestPanel");
            go.transform.SetParent(canvasTransform, false);
            var prt = go.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(1f, 1f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.sizeDelta = new Vector2(430f, 220f);
            prt.anchoredPosition = new Vector2(-16f, -118f);

            var img = go.AddComponent<Image>();
            img.sprite = Art.PixelArtFactory.UIPanel(PanelBg, BorderGold);
            img.type = Image.Type.Sliced;
            _panel = prt;

            // Titulo
            _titleText = MakeLabel(go.transform, "LIBRETA DEL YACHACHIQ (Misiones)", 18, TextAnchor.UpperLeft,
                                   new Vector2(16f, -28f), new Vector2(-16f, -6f), BorderGold);

            // Tareas
            _task1Text = MakeLabel(go.transform, "1. Arrear alpacas al cobertizo (ODS 13)", 15, TextAnchor.MiddleLeft,
                                   new Vector2(16f, -66f), new Vector2(-16f, -38f), TextCream);

            _task2Text = MakeLabel(go.transform, "2. Acondicionar viviendas: Muro Trombe (ODS 13)", 15, TextAnchor.MiddleLeft,
                                   new Vector2(16f, -102f), new Vector2(-16f, -74f), TextCream);

            _task3Text = MakeLabel(go.transform, "3. Salud: Atender enfermos en Posta (ODS 3)", 15, TextAnchor.MiddleLeft,
                                   new Vector2(16f, -138f), new Vector2(-16f, -110f), TextCream);

            _task4Text = MakeLabel(go.transform, "4. Mantener fogon comunal encendido", 15, TextAnchor.MiddleLeft,
                                   new Vector2(16f, -174f), new Vector2(-16f, -146f), TextCream);

            // Pie con atajos
            _footerText = MakeLabel(go.transform, "Atajos: [Q] Pututo / Ayni   |   [F] Silbar a alpacas", 13, TextAnchor.LowerCenter,
                                    new Vector2(10f, -212f), new Vector2(-10f, -180f), new Color32(180, 205, 230, 255));
        }

        private Text MakeLabel(Transform parent, string txt, int size, TextAnchor anchor,
                               Vector2 offsetMin, Vector2 offsetMax, Color32 color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = txt;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return t;
        }

        private void Update()
        {
            var hsm = HealthShelterManager.Instance;
            var ttm = TimeTemperatureManager.Instance;
            var fire = Campfire.Instance;
            if (hsm == null || _task1Text == null) return;

            // 1. ODS 13: Alpacas resguardadas
            int sheltered = hsm.AlpacasSheltered;
            int totalAlpacas = hsm.AlpacaCount;
            int targetShelter = Mathf.Max(6, Mathf.RoundToInt(totalAlpacas * 0.6f));
            bool t1Now = sheltered >= targetShelter;
            _task1Text.text = $"{(t1Now ? "[✓]" : "[  ]")} 1. Cobertizo (ODS 13): {sheltered}/{targetShelter} alpacas";
            _task1Text.color = t1Now ? DoneGreen : PendingGold;
            if (t1Now && !_task1Done) { _task1Done = true; OnTaskComplete("¡Rebano a salvo en el cobertizo! (ODS 13)"); }

            // 2. ODS 13: Muros Trombe / Viviendas acondicionadas
            int trombeCount = CountTrombeHouses();
            bool t2Now = trombeCount >= 2;
            _task2Text.text = $"{(t2Now ? "[✓]" : "[  ]")} 2. Acondicionamiento: {trombeCount}/2 casas protegidas";
            _task2Text.color = t2Now ? DoneGreen : PendingGold;
            if (t2Now && !_task2Done) { _task2Done = true; OnTaskComplete("¡Casas protegidas con Muro Trombe! (ODS 13)"); }

            // 3. ODS 3: Salud comunitaria
            int sick = hsm.SickCount;
            int sanos = hsm.HumanPopulation - sick;
            bool t3Now = sick == 0;
            _task3Text.text = $"{(t3Now ? "[✓]" : "[  ]")} 3. Posta Medica (ODS 3): {sanos}/{hsm.HumanPopulation} sanos";
            _task3Text.color = t3Now ? DoneGreen : (sick > 0 ? new Color32(255, 140, 130, 255) : PendingGold);
            if (t3Now && !_task3Done) { _task3Done = true; OnTaskComplete("¡Toda la comunidad sana sin neumonias! (ODS 3)"); }

            // 4. Fogón Comunal
            float fuel = fire != null ? fire.FuelRatio : 0f;
            bool t4Now = fuel >= 0.70f;
            _task4Text.text = $"{(t4Now ? "[✓]" : "[  ]")} 4. Fogon Comunal: {Mathf.RoundToInt(fuel * 100f)}% combustible";
            _task4Text.color = t4Now ? DoneGreen : PendingGold;
            if (t4Now && !_task4Done) { _task4Done = true; OnTaskComplete("¡Fogon bien abastecido para la noche!"); }
        }

        private int CountTrombeHouses()
        {
            int count = 0;
            var buildings = FindObjectsOfType<ThermalBuilding>();
            foreach (var b in buildings)
            {
                if (b.Kind == BuildingKind.House && b.Level >= 1)
                    count++;
            }
            return count;
        }

        private void OnTaskComplete(string message)
        {
            AudioDirector.Instance?.Pickup();
            HUDController.Instance?.Banner(message, new Color32(40, 160, 90, 235));
            HUDController.Instance?.Log($"[Mision Lograda] {message}");
            HealthShelterManager.Instance?.AddMorale(10f);
        }
    }
}
