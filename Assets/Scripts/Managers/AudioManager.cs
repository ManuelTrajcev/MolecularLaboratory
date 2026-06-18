using System.Collections;
using UnityEngine;
using MolecularLab.Chemistry;

namespace MolecularLab.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] public AudioClip ambientClip;
        [SerializeField] public AudioClip teleportClip;
        [SerializeField] public AudioClip displayUpdateClip;
        [SerializeField] public AudioClip placeDownClip;
        [SerializeField] public AudioClip[] metalHitClips;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.75f;
        
        [Header("Pitch Variation (VR Fatigue Prevention)")]
        [SerializeField] private float minPitchOffset = 0.9f;
        [SerializeField] private float maxPitchOffset = 1.1f;

        [Header("Teleport SFX Timing Optimization")]
        [Tooltip("Skips the quiet rise or silent lead-in of the futuristic teleport clip for instant feedback")]
        [SerializeField, Min(0f)] private float teleportStartOffset = 0.22f;

        private AudioSource _ambientSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAmbientSource();
        }

        private void Start()
        {
            // Pre-load all audio clip data to eliminate decompression latency on first play
            PreloadAudioData();

            // Decoupled subscription to BondManager bond formation
            if (BondManager.Instance != null)
            {
                BondManager.Instance.BondFormed += OnBondFormed;
            }
            else
            {
                StartCoroutine(DeferredBondManagerSubscription());
            }
        }

        private void OnDestroy()
        {
            if (BondManager.Instance != null)
            {
                BondManager.Instance.BondFormed -= OnBondFormed;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private IEnumerator DeferredBondManagerSubscription()
        {
            // In case BondManager is still initializing or instantiated later,
            // we poll briefly to establish the connection defensively.
            while (BondManager.Instance == null)
            {
                yield return null;
            }
            BondManager.Instance.BondFormed += OnBondFormed;
        }

        private void PreloadAudioData()
        {
            if (teleportClip != null) teleportClip.LoadAudioData();
            if (displayUpdateClip != null) displayUpdateClip.LoadAudioData();
            if (placeDownClip != null) placeDownClip.LoadAudioData();
            if (ambientClip != null) ambientClip.LoadAudioData();
            if (metalHitClips != null)
            {
                for (int i = 0; i < metalHitClips.Length; i++)
                {
                    if (metalHitClips[i] != null)
                        metalHitClips[i].LoadAudioData();
                }
            }
        }

        private void SetupAmbientSource()
        {
            if (ambientClip == null) return;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.clip = ambientClip;
            _ambientSource.loop = true;
            _ambientSource.volume = ambientVolume;
            _ambientSource.spatialBlend = 0.0f; // 2D Stereo for background music
            _ambientSource.playOnAwake = true;
            _ambientSource.Play();
        }

        private void OnBondFormed(Bond bond)
        {
            if (bond == null || metalHitClips == null || metalHitClips.Length == 0) return;

            // Pick a random metal hit sound
            int randIndex = Random.Range(0, metalHitClips.Length);
            AudioClip clip = metalHitClips[randIndex];

            // Play in 3D space at the center of the bond or the bond object position
            Vector3 spawnPos = bond.transform.position;
            if (bond.A != null && bond.B != null)
            {
                spawnPos = (bond.A.transform.position + bond.B.transform.position) * 0.5f;
            }

            PlayClipAtPointWithPitch(clip, spawnPos, sfxVolume, minPitchOffset, maxPitchOffset);
        }

        public void PlayElementChosen(Vector3 position)
        {
            if (teleportClip == null) return;

            // Teleport clip gets slightly wider pitch variations to sound alive and reduce fatigue
            float minTeleportPitch = Mathf.Clamp(minPitchOffset - 0.05f, 0.7f, 1.0f);
            float maxTeleportPitch = Mathf.Clamp(maxPitchOffset + 0.05f, 1.0f, 1.3f);

            PlayClipAtPointWithPitch(teleportClip, position, sfxVolume * 0.8f, minTeleportPitch, maxTeleportPitch, teleportStartOffset);
        }

        public void PlayDisplayUpdate(Vector3 position)
        {
            if (displayUpdateClip == null) return;
            PlayClipAtPointWithPitch(displayUpdateClip, position, sfxVolume * 0.95f, minPitchOffset, maxPitchOffset);
        }

        public void PlayPlaceDown(Vector3 position)
        {
            if (placeDownClip == null) return;
            PlayClipAtPointWithPitch(placeDownClip, position, sfxVolume * 0.85f, minPitchOffset, maxPitchOffset);
        }

        /// <summary>
        /// Instantiates a temporary 3D audio source to play a spatialized sound with a random pitch offset and custom start offset.
        /// </summary>
        public void PlayClipAtPointWithPitch(AudioClip clip, Vector3 position, float volume, float minPitch, float maxPitch, float timeOffset = 0f)
        {
            if (clip == null) return;

            GameObject tempGO = new GameObject("TempSpatialAudio_" + clip.name);
            tempGO.transform.position = position;

            AudioSource source = tempGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = Random.Range(minPitch, maxPitch);

            // Configure premium 3D Spatial Audio settings for VR comfort
            source.spatialBlend = 1.0f; // 100% 3D spatialized sound
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 0.5f;
            source.maxDistance = 12f;
            source.dopplerLevel = 0f; // Disable doppler to avoid pitch distortion during teleport/motion

            if (timeOffset > 0f && timeOffset < clip.length)
            {
                source.time = timeOffset;
            }

            source.Play();

            // Destroy the temporary object once playback completes
            Destroy(tempGO, Mathf.Max(0.1f, clip.length - timeOffset + 0.2f));
        }
    }
}
