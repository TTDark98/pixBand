using UnityEngine;

namespace PixBand.Audio
{
    /// <summary>Plays a licensed recorded howl as a rare distant event.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class HauntedAmbience : MonoBehaviour
    {
        public AudioClip howlClip;
        [Range(0f, 1f)] public float howlVolume = 0.62f;
        [Min(1f)] public float firstHowlMinimumDelay = 22f;
        [Min(1f)] public float firstHowlMaximumDelay = 36f;
        [Min(1f)] public float minimumHowlInterval = 65f;
        [Min(1f)] public float maximumHowlInterval = 100f;

        private AudioSource howlSource;
        private float nextHowlTime;

        private void Awake()
        {
            howlSource = GetComponent<AudioSource>();
            howlSource.playOnAwake = false;
            howlSource.loop = false;
            howlSource.spatialBlend = 0f;
            howlSource.volume = howlVolume;
            howlSource.priority = 160;
            howlSource.dopplerLevel = 0f;
            if (howlClip != null && howlClip.loadState == AudioDataLoadState.Unloaded)
                howlClip.LoadAudioData();
            if (howlClip == null)
                Debug.LogWarning("[PixBand Audio] Distant howl clip is not assigned.", this);
        }

        private void Start()
        {
            ScheduleNextHowl(firstHowlMinimumDelay, firstHowlMaximumDelay);
        }

        private void Update()
        {
            if (howlClip == null || Time.time < nextHowlTime) return;

            howlSource.pitch = Random.Range(0.88f, 1.04f);
            howlSource.PlayOneShot(howlClip, Random.Range(0.9f, 1f));
            ScheduleNextHowl(minimumHowlInterval, maximumHowlInterval);
        }

        private void ScheduleNextHowl(float minimum, float maximum)
        {
            if (maximum < minimum) maximum = minimum;
            nextHowlTime = Time.time + Random.Range(minimum, maximum);
        }
    }
}
