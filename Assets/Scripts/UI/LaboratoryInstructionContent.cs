using MolecularLab.Interaction;
using UnityEngine;

namespace MolecularLab.UI
{
    public static class LaboratoryInstructionContent
    {
        public const string WelcomeText = "Welcome to the Molecular Laboratory";

        public const string GameplayText =
            "Gameplay\n\n"
            + "- Look at the table on your right for the equation\n"
            + "- Pick required atoms from the Periodic Table\n"
            + "- Drop single atoms into the Small Chamber\n"
            + "- The Small Chamber builds the needed molecule\n"
            + "- Move completed molecules to the Big Chamber\n"
            + "- Placed molecules become locked and non-interactable\n"
            + "- Wrong atoms/molecules are rejected\n"
            + "- Hints guide you after inactivity and on level start\n"
            + "- Reset clears the current level; Next/Retry appears after reactions";

        public const string MouseControlsText =
            "Controls\n\n"
            + "- Mouse look aims the camera\n"
            + "- WASD moves, Space/C moves up/down, limited to lab height\n"
            + "- Left mouse grabs/selects atoms, molecules, and buttons\n"
            + "- Mouse wheel moves held atoms/molecules closer/farther\n"
            + "- Shift + WASD moves the held atom/molecule\n"
            + "- Y shows/hides info\n"
            + "- Z zooms, X deletes aimed atom";

        public const string XrControlsText =
            "Controls\n\n"
            + "- Right controller: grab/select atoms, molecules, and buttons\n"
            + "- Simulator: G = right grab/select\n"
            + "- Simulator: Q/E moves up/down, limited to lab height\n"
            + "- Left controller Y shows/hides info\n"
            + "- Simulator info: Left Shift + 2\n"
            + "- Left controller grip deletes a targeted atom\n"
            + "- Simulator delete: Left Shift + G\n"
            + "- Hold right B button to zoom\n"
            + "- Simulator zoom: 2";

        public static string GetActiveControlsText()
        {
            return IsMouseControlCameraActive() ? MouseControlsText : XrControlsText;
        }

        public static bool IsMouseControlCameraActive()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent<MouseControlCamera>(out var cameraController) &&
                cameraController.isActiveAndEnabled)
            {
                return true;
            }

            var activeController = Object.FindFirstObjectByType<MouseControlCamera>(FindObjectsInactive.Exclude);
            return activeController != null && activeController.isActiveAndEnabled;
        }
    }
}
