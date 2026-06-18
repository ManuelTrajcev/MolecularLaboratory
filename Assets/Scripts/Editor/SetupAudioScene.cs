#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MolecularLab.Managers;

namespace MolecularLab.Editor
{
    [InitializeOnLoad]
    public static class SetupAudioScene
    {
        static SetupAudioScene()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            // Also run a check on editor compile/startup
            EditorApplication.delayCall += () => ConfigureAudio(EditorSceneManager.GetActiveScene());
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ConfigureAudio(scene);
        }

        [MenuItem("Molecular Lab/Setup Audio in Current Scene")]
        public static void ForceConfigureAudio()
        {
            ConfigureAudio(EditorSceneManager.GetActiveScene(), forceLog: true);
        }

        public static void ConfigureAudio(Scene scene, bool forceLog = false)
        {
            // Only execute inside the Laboratory or Laboratory - Updated scenes
            if (scene.name != "Laboratory - Updated" && scene.name != "Laboratory")
            {
                if (forceLog)
                {
                    Debug.LogWarning($"[SetupAudioScene] Audio setup can only be executed in 'Laboratory' or 'Laboratory - Updated' scenes. Current scene is '{scene.name}'.");
                }
                return;
            }

            var levelManagerGo = GameObject.Find("LevelManager");
            if (levelManagerGo == null)
            {
                if (forceLog)
                {
                    Debug.LogError("[SetupAudioScene] Could not find 'LevelManager' GameObject in scene hierarchy!");
                }
                return;
            }

            var audioManager = levelManagerGo.GetComponent<AudioManager>();
            bool changed = false;
            if (audioManager == null)
            {
                audioManager = levelManagerGo.AddComponent<AudioManager>();
                changed = true;
                Debug.Log("[SetupAudioScene] Attached AudioManager component to LevelManager GameObject.");
            }

            // Assign clips from Assets/Audio
            if (audioManager.ambientClip == null)
            {
                audioManager.ambientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Science Ambient.mp3");
                if (audioManager.ambientClip != null) changed = true;
            }
            if (audioManager.teleportClip == null)
            {
                audioManager.teleportClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/trading_nation-transition-futuristic-teleport-121420.mp3");
                if (audioManager.teleportClip != null) changed = true;
            }
            if (audioManager.displayUpdateClip == null)
            {
                audioManager.displayUpdateClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/display-update.mp3");
                if (audioManager.displayUpdateClip != null) changed = true;
            }
            if (audioManager.placeDownClip == null)
            {
                audioManager.placeDownClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/place-down.mp3");
                if (audioManager.placeDownClip != null) changed = true;
            }

            // Load metal hit clips if list is empty or incomplete
            if (audioManager.metalHitClips == null || audioManager.metalHitClips.Length == 0)
            {
                var clips = new AudioClip[6];
                clips[0] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-90-200426.mp3");
                clips[1] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-91-200421.mp3");
                clips[2] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-92-200420.mp3");
                clips[3] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-94-200422.mp3");
                clips[4] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-95-200424.mp3");
                clips[5] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/floraphonic-metal-hit-97-200427.mp3");

                audioManager.metalHitClips = clips;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(audioManager);
                EditorUtility.SetDirty(levelManagerGo);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[SetupAudioScene] Successfully configured and wired AudioManager in scene: {scene.name}");
            }
            else if (forceLog)
            {
                Debug.Log("[SetupAudioScene] AudioManager is already correctly configured in this scene.");
            }
        }
    }
}
#endif
