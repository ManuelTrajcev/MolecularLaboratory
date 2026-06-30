using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace MolecularLab.EditorTools
{
    /// <summary>
    /// The Meta XR Core SDK (com.meta.xr.sdk.core) does not compile for WebGL —
    /// its AIBlocks assembly references UnityEngine.Microphone, which Unity strips
    /// on that platform. This utility removes the package so a WebGL build can
    /// compile, then re-adds it for Quest. Run the menu items AROUND a web build:
    ///   1. Tools ▸ Web Build ▸ Remove Meta SDK (for WebGL)  — then WAIT for the
    ///      recompile to finish (watch the bottom-right spinner) before building.
    ///   2. Build the WebGL player.
    ///   3. Tools ▸ Web Build ▸ Restore Meta SDK (for Quest).
    /// PackageManager.Client edits the manifest and triggers resolve/recompile
    /// for us, so we never hand-edit manifest.json.
    /// </summary>
    internal static class MetaSdkWebBuildToggle
    {
        private const string PackageName = "com.meta.xr.sdk.core";
        private const string PackageVersion = "201.0.0";
        private const string MenuRemove = "Tools/Web Build/Remove Meta SDK (for WebGL)";
        private const string MenuRestore = "Tools/Web Build/Restore Meta SDK (for Quest)";

        private static Request _pending;

        [MenuItem(MenuRemove, priority = 100)]
        private static void Remove()
        {
            if (!Confirm(_pending))
                return;

            Debug.Log($"[MetaSdkWebBuildToggle] Removing {PackageName} … Unity will resolve and recompile. " +
                      "Wait for the spinner to clear before starting the WebGL build.");
            _pending = Client.Remove(PackageName);
            EditorApplication.update += PollRemove;
        }

        [MenuItem(MenuRestore, priority = 101)]
        private static void Restore()
        {
            if (!Confirm(_pending))
                return;

            Debug.Log($"[MetaSdkWebBuildToggle] Re-adding {PackageName}@{PackageVersion} for Quest …");
            _pending = Client.Add($"{PackageName}@{PackageVersion}");
            EditorApplication.update += PollAdd;
        }

        // Disable the menu items while a request is still in flight.
        [MenuItem(MenuRemove, validate = true)]
        private static bool RemoveValidate() => _pending == null || _pending.IsCompleted;

        [MenuItem(MenuRestore, validate = true)]
        private static bool RestoreValidate() => _pending == null || _pending.IsCompleted;

        private static void PollRemove()
        {
            if (_pending == null || !_pending.IsCompleted)
                return;

            EditorApplication.update -= PollRemove;
            var req = (RemoveRequest)_pending;
            if (req.Status == StatusCode.Success)
                Debug.Log($"[MetaSdkWebBuildToggle] Removed {req.PackageIdOrName}. Once the recompile finishes, build WebGL.");
            else
                Debug.LogError($"[MetaSdkWebBuildToggle] Remove failed: {req.Error?.message}");
            _pending = null;
        }

        private static void PollAdd()
        {
            if (_pending == null || !_pending.IsCompleted)
                return;

            EditorApplication.update -= PollAdd;
            var req = (AddRequest)_pending;
            if (req.Status == StatusCode.Success)
                Debug.Log($"[MetaSdkWebBuildToggle] Restored {req.Result.packageId}. Project is back to its Quest configuration.");
            else
                Debug.LogError($"[MetaSdkWebBuildToggle] Restore failed: {req.Error?.message}");
            _pending = null;
        }

        private static bool Confirm(Request inFlight)
        {
            if (inFlight != null && !inFlight.IsCompleted)
            {
                Debug.LogWarning("[MetaSdkWebBuildToggle] A package request is still in progress — wait for it to finish.");
                return false;
            }
            return true;
        }
    }
}
