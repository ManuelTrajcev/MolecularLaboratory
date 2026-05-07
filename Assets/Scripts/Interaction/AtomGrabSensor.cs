using MolecularLab.Chemistry;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Interaction
{
    [RequireComponent(typeof(Atom))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class AtomGrabSensor : MonoBehaviour
    {
        private Atom _atom;
        private XRGrabInteractable _grab;

        private void Awake()
        {
            _atom = GetComponent<Atom>();
            _grab = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            BondManager.Instance?.Register(_atom);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            BondManager.Instance?.Unregister(_atom);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            BondManager.Instance?.TryFormBondsAround(_atom);
        }
    }
}
