using UnityEngine;

namespace PixBand.Audio
{
    /// <summary>
    /// Lightweight procedural horror bed: a sub-bass drone with dissonant, slowly
    /// moving overtones and an occasional distant descending wail. No external
    /// sound asset or runtime network access is required.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class HauntedAmbience : MonoBehaviour
    {
        [Range(0f, 1f)] public float droneVolume = 0.52f;
        [Range(0f, 1f)] public float wailVolume = 0.42f;
        public float minimumWailInterval = 13f;
        public float maximumWailInterval = 27f;

        private AudioSource drone;
        private AudioSource wails;
        private AudioClip wailClip;
        private float nextWail;

        private void Start()
        {
            drone = GetComponent<AudioSource>();
            drone.clip = CreateDrone();
            drone.loop = true;
            drone.playOnAwake = false;
            drone.spatialBlend = 0f;
            drone.volume = droneVolume;
            drone.priority = 170;
            drone.Play();

            GameObject eventObject = new GameObject("Distant_Wail");
            eventObject.transform.SetParent(transform, false);
            wails = eventObject.AddComponent<AudioSource>();
            wails.playOnAwake = false;
            wails.loop = false;
            wails.spatialBlend = 0f;
            wails.volume = wailVolume;
            wails.priority = 180;
            wailClip = CreateWail();
            ScheduleWail();
        }

        private void Update()
        {
            if (Time.time < nextWail) return;
            if (wails != null && wailClip != null)
            {
                wails.pitch = Random.Range(0.88f, 1.12f);
                wails.PlayOneShot(wailClip, Random.Range(0.55f, 1f));
            }
            ScheduleWail();
        }

        private void ScheduleWail()
        {
            nextWail = Time.time + Random.Range(minimumWailInterval, maximumWailInterval);
        }

        private static AudioClip CreateDrone()
        {
            const int rate = 22050;
            const float seconds = 24f;
            int count = Mathf.RoundToInt(rate * seconds);
            float[] samples = new float[count];
            System.Random random = new System.Random(31017);
            float filteredNoise = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float slow = Mathf.Sin(2f * Mathf.PI * 0.035f * t);
                float rumble = Mathf.Sin(2f * Mathf.PI * (37f + 0.8f * slow) * t) * 0.38f;
                rumble += Mathf.Sin(2f * Mathf.PI * 41.2f * t + 0.7f) * 0.26f;
                rumble += Mathf.Sin(2f * Mathf.PI * 55f * t + 1.4f) * 0.12f;

                float breath = (float)(random.NextDouble() * 2.0 - 1.0);
                filteredNoise += 0.007f * (breath - filteredNoise);
                float overtone = Mathf.Sin(2f * Mathf.PI * (173f + 2.5f * slow) * t) * 0.075f;
                float tremolo = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.09f * t + 1.1f);
                float edge = Mathf.Min(1f, i / (rate * 1.2f)) * Mathf.Min(1f, (count - i) / (rate * 1.2f));

                samples[i] = (rumble + overtone + filteredNoise * 2.4f) * tremolo * edge * 0.19f;
            }

            AudioClip clip = AudioClip.Create("PixBand_Haunted_Drone", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateWail()
        {
            const int rate = 22050;
            const float seconds = 5.2f;
            int count = Mathf.RoundToInt(rate * seconds);
            float[] samples = new float[count];
            System.Random random = new System.Random(8802);
            float noise = 0f;
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float progress = t / seconds;
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * progress), 1.35f);
                float frequency = Mathf.Lerp(420f, 78f, Mathf.SmoothStep(0f, 1f, progress));
                frequency += 18f * Mathf.Sin(t * 2.3f) + 7f * Mathf.Sin(t * 6.1f);
                phase += 2f * Mathf.PI * frequency / rate;

                noise += 0.035f * ((float)(random.NextDouble() * 2.0 - 1.0) - noise);
                float voice = Mathf.Sin(phase) * 0.62f + Mathf.Sin(phase * 2.02f + 0.4f) * 0.22f;
                voice += Mathf.Sin(phase * 0.5f) * 0.15f + noise * 0.45f;
                samples[i] = voice * envelope * 0.25f;
            }

            AudioClip clip = AudioClip.Create("PixBand_Distant_Wail", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
