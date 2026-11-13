using UnityEngine;

namespace PixBand.Core
{
    /// <summary>Cheap candle-style flicker for lantern point lights.</summary>
    [RequireComponent(typeof(Light))]
    public class LanternFlicker : MonoBehaviour
    {
        public float baseIntensity = 1.3f;
        public float amount = 0.18f;
        public float speed = 6f;

        private Light source;
        private float seed;

        private void Awake()
        {
            source = GetComponent<Light>();
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            source.intensity = baseIntensity + (n - 0.5f) * 2f * amount;
        }
    }
}
