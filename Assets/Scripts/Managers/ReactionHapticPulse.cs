using MolecularLab.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MolecularLab.Managers
{
    /// <summary>
    /// Attach to any scene GameObject (e.g. LevelManager).
    /// Subscribes to ReactionChamber.RecipeReacted and fires a strong haptic
    /// pulse on both Touch controllers via XRI's ActionBasedController —
    /// works with OpenXR + Meta Quest feature group.
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
            var controllers = FindObjectsByType<ActionBasedController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
                c.SendHapticImpulse(_amplitude, _duration);
        }
    }
}
