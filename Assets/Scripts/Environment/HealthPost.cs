using UnityEngine;
using FrioAndino.Player;
using FrioAndino.Economy;
using FrioAndino.Audio;
using FrioAndino.Core;

namespace FrioAndino.Environment
{
    /// <summary>
    /// Posta de salud comunal. Doña Tomasa prepara mates de muña y wira-wira:
    /// convierte hierbas en comuneros recuperados (mecanica directa del ODS 3, meta 3.9).
    /// </summary>
    public class HealthPost : MonoBehaviour, IInteractable
    {
        [Tooltip("Hierbas necesarias por cada comunero tratado.")]
        [SerializeField] private int herbsPerTreatment = 3;

        public string GetPrompt()
        {
            var hsm = HealthShelterManager.Instance;
            int sick = hsm != null ? hsm.SickCount : 0;
            if (sick <= 0) return "Dona Tomasa: \"Por ahora nadie esta enfermo, Inti\"";
            return $"Pedir a Dona Tomasa un mate de muna  [Hierbas x{herbsPerTreatment}]  ({sick} enfermo/s)";
        }

        public bool CanInteract() => true;

        public string Interact(GameObject actor)
        {
            var hsm = HealthShelterManager.Instance;
            var rm = ResourceManager.Instance;
            if (hsm == null || rm == null) return null;

            if (hsm.SickCount <= 0)
            {
                AudioDirector.Instance?.Pickup();
                return "Dona Tomasa: \"Todos estan sanos hoy. Recuerda: muna para calentar el estomago y wira-wira para los pulmones contra el frio.\"";
            }

            if (!rm.Has(ResourceType.Herbs, herbsPerTreatment))
                return $"Faltan hierbas ({herbsPerTreatment}). Busca muna y wira-wira en las laderas.";

            rm.Spend(ResourceType.Herbs, herbsPerTreatment);
            hsm.CureOne();
            hsm.AddMorale(5f);
            AudioDirector.Instance?.Pickup();
            UI.HUDController.Instance?.FloatingText(transform.position + Vector3.up * 2f, "+1 SANO (ODS 3)",
                                                    new Color32(110, 240, 160, 255));
            return "Un comunero se recupero con mate caliente de muna y wira-wira (ODS 3).";
        }

        public Transform GetTransform() => transform;
    }
}
