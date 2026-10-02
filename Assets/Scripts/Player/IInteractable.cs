using UnityEngine;

namespace FrioAndino.Player
{
    /// <summary>
    /// Contrato de todo lo que Inti puede accionar con 'E'.
    /// Se usa una interfaz (y no una clase base) para que casas, fogon, matas de ichu
    /// y alpacas respondan igual sin compartir jerarquia.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto que ve el jugador en el cartel flotante (ej. "Cosechar ichu").</summary>
        string GetPrompt();

        /// <summary>True si en este momento se puede interactuar.</summary>
        bool CanInteract();

        /// <summary>Ejecuta la accion. Devuelve un mensaje para el log del HUD (o null).</summary>
        string Interact(GameObject actor);

        Transform GetTransform();
    }
}
