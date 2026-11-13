using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixBand.Interaction
{
    /// <summary>
    /// A single drum head / cymbal. Reacts to a grabbed stick touching it with enough
    /// speed: spatial one-shot, controller haptic, visual recoil, dust.
    /// </summary>
    public class DrumPad : MonoBehaviour
    {
        [Header("Audio")]
        public AudioSource source;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

        [Header("Hit response")]
        public float minTipSpeed = 0.6f;
        public float cooldown = 0.05f;
        public float hapticAmplitude = 0.25f;
        public float hapticDuration = 0.05f;

        [Header("Visuals")]
        public Transform head;
        public float recoilDepth = 0.006f;
        public ParticleSystem dust;

        private Vector3 headHome;
        private float lastHitTime = -10f;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            if (head != null) headHome = head.localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            DrumStick stick = other.GetComponentInParent<DrumStick>();
            if (stick == null || !stick.IsHeld) return;
            if (stick.TipSpeed < minTipSpeed) return;
            TryStrike(stick);
        }

        /// <summary>Hit the pad from code (desktop testing) with the same cooldown as a real swing.</summary>
        public bool TryStrike(DrumStick stick)
        {
            if (stick == null || !stick.IsHeld) return false;
            if (Time.time - lastHitTime < cooldown) return false;

            lastHitTime = Time.time;
            Hit(stick);
            return true;
        }

        private void Hit(DrumStick stick)
        {
            if (source != null && clip != null)
            {
                source.pitch = Random.Range(pitchRange.x, pitchRange.y);
                source.PlayOneShot(clip, volume);
            }

            XRBaseControllerInteractor interactor = stick.GetGrabbingInteractor();
            if (interactor != null && interactor.xrController != null)
                interactor.xrController.SendHapticImpulse(hapticAmplitude, hapticDuration);

            if (dust != null)
            {
                dust.transform.position = transform.position + Vector3.up * 0.02f;
                dust.Emit(6);
            }

            if (head != null && isActiveAndEnabled)
                StartCoroutine(Recoil());
        }

        private System.Collections.IEnumerator Recoil()
        {
            float t = 0f;
            const float down = 0.05f;
            const float up = 0.11f;

            while (t < down)
            {
                t += Time.deltaTime;
                head.localPosition = Vector3.Lerp(headHome, headHome + Vector3.down * recoilDepth, t / down);
                yield return null;
            }

            t = 0f;
            while (t < up)
            {
                t += Time.deltaTime;
                head.localPosition = Vector3.Lerp(headHome + Vector3.down * recoilDepth, headHome, t / up);
                yield return null;
            }

            head.localPosition = headHome;
        }
    }
}
