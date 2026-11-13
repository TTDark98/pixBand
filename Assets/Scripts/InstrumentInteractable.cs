using System.Collections.Generic;
using UnityEngine;

namespace PixBand.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class InstrumentInteractable : MonoBehaviour
    {
        public enum InstrumentType
        {
            DrumKit,
            Keyboard,
            Guitar
        }

        [Header("Instrument Identity")]
        [SerializeField] private string instrumentName = "Instrument";
        [SerializeField] private InstrumentType instrumentType = InstrumentType.DrumKit;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip loopClip;
        [SerializeField] private List<AudioClip> oneShotClips = new List<AudioClip>();

        [Header("Spatial Audio Settings")]
        [Range(0f, 1f)] [SerializeField] private float spatialBlend = 1.0f;
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 25f;
        [Range(0f, 360f)] [SerializeField] private float spread = 45f;

        private AudioSource audioSource;
        private AudioListener currentListener;
        private bool isLoopPlaying = false;

        public string InstrumentName => instrumentName;
        public InstrumentType Type => instrumentType;
        public bool IsLoopPlaying => isLoopPlaying;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            ConfigureAudioSource();
        }

        private void Start()
        {
            if (loopClip != null && instrumentType == InstrumentType.DrumKit)
            {
                // Auto-start drum beat loop so spatial surround audio is immediately audible
                PlayLoop();
            }
        }

        public void ConfigureAudioSource()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = spatialBlend;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.dopplerLevel = 0.5f;
            audioSource.playOnAwake = false;
        }

        public void ToggleLoop()
        {
            if (isLoopPlaying)
            {
                StopLoop();
            }
            else
            {
                PlayLoop();
            }
        }

        public void PlayLoop()
        {
            if (loopClip == null) return;
            audioSource.clip = loopClip;
            audioSource.loop = true;
            audioSource.Play();
            isLoopPlaying = true;
            Debug.Log($"[{instrumentName}] Started loop playback at {transform.position}");
        }

        public void StopLoop()
        {
            if (audioSource.isPlaying && audioSource.loop)
            {
                audioSource.Stop();
            }
            isLoopPlaying = false;
            Debug.Log($"[{instrumentName}] Stopped loop playback");
        }

        public void PlayOneShot(int index)
        {
            if (oneShotClips == null || index < 0 || index >= oneShotClips.Count) return;
            var clip = oneShotClips[index];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 1.0f);
                Debug.Log($"[{instrumentName}] Played one-shot: {clip.name} from {transform.position}");
            }
        }

        public float GetDistanceToListener()
        {
            if (currentListener == null || !currentListener.isActiveAndEnabled)
            {
                currentListener = FindObjectOfType<AudioListener>();
            }
            if (currentListener != null)
            {
                return Vector3.Distance(transform.position, currentListener.transform.position);
            }
            return -1f;
        }
    }
}

