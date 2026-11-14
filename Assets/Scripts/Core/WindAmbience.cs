using UnityEngine;

namespace PixBand.Audio
{
    /// <summary>Loops the recorded forest wind and birds soundscape.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class WindAmbience : MonoBehaviour
    {
        public AudioClip ambienceClip;
        [Range(0f, 1f)] public float volume = 0.48f;
        [Header("Howling Gusts")]
        public AudioClip howlClip;
        [Range(0f, 1f)] public float howlVolume = 0.28f;
        [Min(1f)] public float firstHowlMinimumDelay = 7f;
        [Min(1f)] public float firstHowlMaximumDelay = 13f;
        [Min(1f)] public float minimumHowlInterval = 20f;
        [Min(1f)] public float maximumHowlInterval = 34f;

        private AudioSource howlSource;
        private float nextHowlTime;

        private void Awake()
        {
            AudioSource source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.priority = 150;
            source.dopplerLevel = 0f;
            source.clip = ambienceClip;
            if (ambienceClip != null &&
                ambienceClip.loadState == AudioDataLoadState.Unloaded)
                ambienceClip.LoadAudioData();
            if (ambienceClip == null)
                Debug.LogWarning("[PixBand Audio] Forest wind loop is not assigned.", this);

            // Keep the howling layer independent from the continuous forest
            // bed so the loop's pitch/volume never changes with a gust.
            howlSource = gameObject.AddComponent<AudioSource>();
            howlSource.playOnAwake = false;
            howlSource.loop = false;
            howlSource.spatialBlend = 0f;
            howlSource.volume = howlVolume;
            howlSource.priority = 125;
            howlSource.dopplerLevel = 0f;
            if (howlClip != null && howlClip.loadState == AudioDataLoadState.Unloaded)
                howlClip.LoadAudioData();
            if (howlClip == null)
                Debug.LogWarning("[PixBand Audio] Howling wind clip is not assigned.", this);
        }

        private void Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source.clip != null && !source.isPlaying) source.Play();
            ScheduleHowl(firstHowlMinimumDelay, firstHowlMaximumDelay);
        }

        private void Update()
        {
            if (howlClip == null || Time.time < nextHowlTime) return;
            howlSource.pitch = Random.Range(0.92f, 1.06f);
            howlSource.PlayOneShot(howlClip, Random.Range(0.82f, 1f));
            ScheduleHowl(minimumHowlInterval, maximumHowlInterval);
        }

        private void ScheduleHowl(float minimum, float maximum)
        {
            if (maximum < minimum) maximum = minimum;
            nextHowlTime = Time.time + Random.Range(minimum, maximum);
        }
    }
}
