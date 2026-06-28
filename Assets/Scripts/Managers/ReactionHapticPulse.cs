using System.Collections.Generic;
using MolecularLab.Interaction;
using UnityEngine;
using UnityEngine.XR;

namespace MolecularLab.Managers
{
    /// <summary>
    /// Attach to any scene GameObject (e.g. LevelManager).
    /// Subscribes to ReactionChamber.RecipeReacted and fires a strong haptic
    /// pulse on both controllers — making the chamber explosion physically felt.
    /// </summary>
    public class ReactionHapticPulse : MonoBehaviour
    {
        [SerializeField] private ReactionChamber _chamber;
        [SerializeField] [Range(0f, 1f)] private float _amplitude = 0.80f;
        [SerializeField] private float _duration = 0.35f;

        private void Start()
        {
            if (_chamber == null)
                _chamber = FindFirstObjectByType<ReactionChamber>();

            if (_chamber != null)
                _chamber.RecipeReacted += OnRecipeReacted;
        }

        private void OnDestroy()
        {
            if (_chamber != null)
                _chamber.RecipeReacted -= OnRecipeReacted;
        }

        private void OnRecipeReacted(MolecularLab.Chemistry.ReactionRecipeSO _)
        {
            SendHapticsToAllControllers(_amplitude, _duration);
        }

        private static void SendHapticsToAllControllers(float amplitude, float duration)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
                devices
            );
            foreach (var device in devices)
                device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}
