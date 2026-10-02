using UnityEngine;
using System;
using System.Collections.Generic;

namespace FrioAndino.Economy
{
    public enum ResourceType { Ichu, Firewood, Herbs, Stone }

    /// <summary>
    /// Inventario comunal. Servicio unico (singleton ligero) porque en este juego
    /// los recursos son de la comunidad, no de un jugador individual — parte del mensaje del ayni.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Stock inicial")]
        [SerializeField] private int startIchu = 0;
        [SerializeField] private int startFirewood = 10;
        [SerializeField] private int startHerbs = 0;
        [SerializeField] private int startStone = 0;

        private readonly Dictionary<ResourceType, int> _stock = new Dictionary<ResourceType, int>();

        /// <summary>Se dispara con cualquier cambio de stock para refrescar el HUD.</summary>
        public event Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _stock[ResourceType.Ichu] = startIchu;
            _stock[ResourceType.Firewood] = startFirewood;
            _stock[ResourceType.Herbs] = startHerbs;
            _stock[ResourceType.Stone] = startStone;
        }

        public int Get(ResourceType type) => _stock.TryGetValue(type, out var v) ? v : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            _stock[type] = Get(type) + amount;
            OnChanged?.Invoke();
        }

        public bool Has(ResourceType type, int amount) => Get(type) >= amount;

        public bool Spend(ResourceType type, int amount)
        {
            if (!Has(type, amount)) return false;
            _stock[type] = Get(type) - amount;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Cobro atomico de varios recursos (para mejoras bioclimaticas).</summary>
        public bool SpendAll(IEnumerable<KeyValuePair<ResourceType, int>> cost)
        {
            foreach (var c in cost) if (!Has(c.Key, c.Value)) return false;
            foreach (var c in cost) _stock[c.Key] = Get(c.Key) - c.Value;
            OnChanged?.Invoke();
            return true;
        }

        public bool CanAfford(IEnumerable<KeyValuePair<ResourceType, int>> cost)
        {
            foreach (var c in cost) if (!Has(c.Key, c.Value)) return false;
            return true;
        }

        public static string DisplayName(ResourceType t)
        {
            switch (t)
            {
                case ResourceType.Ichu: return "Ichu";
                case ResourceType.Firewood: return "Lena";
                case ResourceType.Herbs: return "Hierbas";
                case ResourceType.Stone: return "Piedra";
            }
            return t.ToString();
        }
    }
}
