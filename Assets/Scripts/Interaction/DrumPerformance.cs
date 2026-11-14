using UnityEngine;

namespace PixBand.Interaction
{
    /// <summary>
    /// Shared rhythm state for the drum kit. Pads report every strike; the
    /// skeletons and the window ghosts read the streak to decide how alive the
    /// mansion should look.
    /// </summary>
    public class DrumPerformance : MonoBehaviour
    {
        [Header("Timing")]
        [Min(0.2f)] public float comboWindow = 0.95f;
        [Min(0.5f)] public float playingWindow = 2.6f;

        [Header("State")]
        public int combo;
        public int totalHits;
        public float lastHitTime = -100f;
        public float lastStrength;

        /// <summary>0..1 pulse that decays after every hit; drives accents.</summary>
        public float Pulse
        {
            get
            {
                float age = Time.time - lastHitTime;
                if (age < 0f || age > 1.2f) return 0f;
                return Mathf.Exp(-age * 5.5f);
            }
        }

        public bool IsPlaying { get { return Time.time - lastHitTime < playingWindow; } }

        public void RegisterHit(float strength)
        {
            float gap = Time.time - lastHitTime;
            combo = gap <= comboWindow ? combo + 1 : 1;
            lastHitTime = Time.time;
            lastStrength = Mathf.Clamp01(strength);
            totalHits++;
        }

        private void Update()
        {
            if (!IsPlaying && combo != 0) combo = 0;
        }
    }
}
