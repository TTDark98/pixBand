using UnityEngine;

namespace PixBand.Audio
{
    /// <summary>
    /// Generates a looping wind bed at runtime so the project needs no external
    /// ambience asset. Filtered noise with slow amplitude movement.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WindAmbience : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.35f;
        public float loopSeconds = 8f;
        public float cutoff = 0.015f;

        private void Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            source.clip = Generate();
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.playOnAwake = false;
            source.Play();
        }

        private AudioClip Generate()
        {
            const int sampleRate = 22050;
            int length = Mathf.Max(1, Mathf.RoundToInt(sampleRate * loopSeconds));
            float[] data = new float[length];

            System.Random random = new System.Random(20261113);
            float low = 0f;
            float low2 = 0f;

            for (int i = 0; i < length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += cutoff * (white - low);
                low2 += cutoff * 0.35f * (low - low2);

                float t = (float)i / sampleRate;
                float gust = 0.55f + 0.45f * Mathf.Sin(t * 0.55f + Mathf.Sin(t * 0.17f) * 2f);
                float fade = Mathf.Min(1f, i / (sampleRate * 0.5f));
                float fadeOut = Mathf.Min(1f, (length - i) / (sampleRate * 0.5f));

                // Bring the filtered wind up to an audible level on small headset
                // speakers while retaining plenty of headroom for drum hits.
                data[i] = Mathf.Clamp(low2 * 14.0f * gust * fade * fadeOut, -0.72f, 0.72f);
            }

            AudioClip clip = AudioClip.Create("ProceduralWind", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
