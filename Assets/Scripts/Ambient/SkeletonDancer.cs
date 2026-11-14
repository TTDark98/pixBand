using UnityEngine;
using PixBand.Interaction;

namespace PixBand.Ambient
{
    /// <summary>
    /// Drives a mansion skeleton as a loose marionette groove. Idle is a slow
    /// sway; once the player starts drumming the skeleton bounces, nods and
    /// swings its arms, with an accent on every strike.
    /// </summary>
    public class SkeletonDancer : MonoBehaviour
    {
        [Header("Rig")]
        public Transform hips;
        public Transform spine;
        public Transform neck;
        public Transform shoulderLeft;
        public Transform shoulderRight;
        public Transform elbowLeft;
        public Transform elbowRight;
        public Transform hipLeft;
        public Transform hipRight;
        public Transform kneeLeft;
        public Transform kneeRight;

        [Header("Groove")]
        public DrumPerformance performance;
        public float danceBlendSpeed = 3.2f;
        public float tempo = 1.85f;
        public float phaseOffset;

        private Quaternion hipsHome;
        private Quaternion spineHome;
        private Quaternion neckHome;
        private Quaternion shoulderLeftHome;
        private Quaternion shoulderRightHome;
        private Quaternion elbowLeftHome;
        private Quaternion elbowRightHome;
        private Quaternion hipLeftHome;
        private Quaternion hipRightHome;
        private Quaternion kneeLeftHome;
        private Quaternion kneeRightHome;
        private Vector3 hipsHomePosition;

        private float blend;
        private float hitGlow;

        private void Awake()
        {
            hipsHome = Capture(hips);
            spineHome = Capture(spine);
            neckHome = Capture(neck);
            shoulderLeftHome = Capture(shoulderLeft);
            shoulderRightHome = Capture(shoulderRight);
            elbowLeftHome = Capture(elbowLeft);
            elbowRightHome = Capture(elbowRight);
            hipLeftHome = Capture(hipLeft);
            hipRightHome = Capture(hipRight);
            kneeLeftHome = Capture(kneeLeft);
            kneeRightHome = Capture(kneeRight);
            if (hips != null) hipsHomePosition = hips.localPosition;
        }

        private void Start()
        {
            if (performance == null) performance = FindObjectOfType<DrumPerformance>();
        }

        private void Update()
        {
            bool playing = performance != null && performance.IsPlaying;
            blend = Mathf.MoveTowards(blend, playing ? 1f : 0f, Time.deltaTime * danceBlendSpeed);
            hitGlow = performance != null ? performance.Pulse : 0f;

            float t = Time.time * tempo + phaseOffset;
            float sway = Mathf.Sin(t * 0.5f);
            float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
            float accent = hitGlow * 1.4f;

            // Hips only yaw and bob, so the feet stay planted on the floor.
            Set(hips, hipsHome, new Vector3(0f, Mathf.Lerp(2.4f * sway, 9f * Mathf.Sin(t), blend), 0f));
            if (hips != null)
            {
                Vector3 local = hipsHomePosition;
                local.y += Mathf.Lerp(0f, -0.032f, blend) * bounce;
                hips.localPosition = local;
            }

            float spineRoll = Mathf.Lerp(-1.1f * sway, -6f * Mathf.Sin(t + 0.9f), blend);
            float spinePitch = Mathf.Lerp(1.4f * sway, 4.5f * bounce + accent * 4f, blend);
            Set(spine, spineHome, new Vector3(spinePitch, 0f, spineRoll));

            float headPitch = Mathf.Lerp(2.2f * Mathf.Sin(t * 0.8f), 10f * Mathf.Sin(t * 2f) + accent * 9f, blend);
            float headYaw = Mathf.Lerp(4.5f * Mathf.Sin(t * 0.37f), 8f * Mathf.Sin(t * 0.7f), blend);
            Set(neck, neckHome, new Vector3(headPitch, headYaw, -spineRoll * 0.5f));

            // Arms: alternate a loose swing, elbows keep a marionette lag.
            float armSwing = Mathf.Sin(t * Mathf.PI) * 34f * blend;
            float armAccent = accent * 10f;
            Set(shoulderLeft, shoulderLeftHome, new Vector3(-armSwing - armAccent, 0f, -8f * blend + 4f * sway));
            Set(shoulderRight, shoulderRightHome, new Vector3(armSwing - armAccent, 0f, 8f * blend - 4f * sway));

            float elbowLag = Mathf.Sin(t * Mathf.PI - 0.8f) * 20f * blend;
            Set(elbowLeft, elbowLeftHome, new Vector3(-18f * blend + elbowLag, 0f, 0f));
            Set(elbowRight, elbowRightHome, new Vector3(-18f * blend - elbowLag, 0f, 0f));

            // Legs: a small stepping bounce, knees absorbing the drop.
            float step = Mathf.Sin(t * Mathf.PI) * 12f * blend;
            Set(hipLeft, hipLeftHome, new Vector3(step, 0f, 2f * blend));
            Set(hipRight, hipRightHome, new Vector3(-step, 0f, -2f * blend));
            Set(kneeLeft, kneeLeftHome, new Vector3(-Mathf.Abs(step) * 0.8f, 0f, 0f));
            Set(kneeRight, kneeRightHome, new Vector3(-Mathf.Abs(step) * 0.8f, 0f, 0f));
        }

        private static Quaternion Capture(Transform t)
        {
            return t == null ? Quaternion.identity : t.localRotation;
        }

        private static void Set(Transform t, Quaternion home, Vector3 euler)
        {
            if (t == null) return;
            t.localRotation = home * Quaternion.Euler(euler);
        }
    }
}
