using UnityEngine;
using PixBand.Interaction;

namespace PixBand.Ambient
{
    /// <summary>
    /// Drives the supplied anatomical skeleton as a loose marionette. Idle is a
    /// slow sway; drumming makes it bounce, nod and swing its arms. The bones
    /// are found by their imported names, so the same component works for both
    /// dancers without a humanoid retarget step.
    /// </summary>
    public class SkeletonModelDancer : MonoBehaviour
    {
        [Header("Groove")]
        public DrumPerformance performance;
        public float danceBlendSpeed = 5.5f;
        public float tempo = 2.65f;
        public float phaseOffset;
        [Range(0.2f, 2.2f)] public float amplitude = 1.45f;

        [Header("Bones")]
        public Transform hip;
        public Transform abdomen;
        public Transform chest;
        public Transform neck;
        public Transform head;
        public Transform shoulderLeft;
        public Transform shoulderRight;
        public Transform forearmLeft;
        public Transform forearmRight;
        public Transform thighLeft;
        public Transform thighRight;
        public Transform shinLeft;
        public Transform shinRight;

        private Transform root;
        private Vector3 rootHomePosition;
        private Quaternion rootHomeRotation;

        private Quaternion hipHome;
        private Quaternion abdomenHome;
        private Quaternion chestHome;
        private Quaternion neckHome;
        private Quaternion headHome;
        private Quaternion shoulderLeftHome;
        private Quaternion shoulderRightHome;
        private Quaternion forearmLeftHome;
        private Quaternion forearmRightHome;
        private Quaternion thighLeftHome;
        private Quaternion thighRightHome;
        private Quaternion shinLeftHome;
        private Quaternion shinRightHome;
        private Vector3 hipHomePosition;

        private float blend;
        private float hitGlow;

        /// <summary>Find the imported bones and remember their rest pose.</summary>
        public void Bind(Transform modelRoot)
        {
            root = modelRoot;
            hip = Find(modelRoot, "hip");
            abdomen = Find(modelRoot, "abdomen");
            chest = Find(modelRoot, "chest");
            neck = Find(modelRoot, "neck");
            head = Find(modelRoot, "head");
            shoulderLeft = Find(modelRoot, "lShldr");
            shoulderRight = Find(modelRoot, "rShldr");
            forearmLeft = Find(modelRoot, "lForeArm");
            forearmRight = Find(modelRoot, "rForeArm");
            thighLeft = Find(modelRoot, "lThigh");
            thighRight = Find(modelRoot, "rThigh");
            shinLeft = Find(modelRoot, "lShin");
            shinRight = Find(modelRoot, "rShin");

            if (root != null)
            {
                rootHomePosition = root.localPosition;
                rootHomeRotation = root.localRotation;
            }

            hipHome = Capture(hip);
            abdomenHome = Capture(abdomen);
            chestHome = Capture(chest);
            neckHome = Capture(neck);
            headHome = Capture(head);
            shoulderLeftHome = Capture(shoulderLeft);
            shoulderRightHome = Capture(shoulderRight);
            forearmLeftHome = Capture(forearmLeft);
            forearmRightHome = Capture(forearmRight);
            thighLeftHome = Capture(thighLeft);
            thighRightHome = Capture(thighRight);
            shinLeftHome = Capture(shinLeft);
            shinRightHome = Capture(shinRight);
            if (hip != null) hipHomePosition = hip.localPosition;
        }

        private void Start()
        {
            if (performance == null) performance = FindObjectOfType<DrumPerformance>();
            if (root == null) Bind(transform);
        }

        private void Update()
        {
            bool playing = performance != null && performance.IsPlaying;
            blend = Mathf.MoveTowards(blend, playing ? 1f : 0f, Time.deltaTime * danceBlendSpeed);
            hitGlow = performance != null ? performance.Pulse : 0f;

            float t = Time.time * tempo + phaseOffset;
            float sway = Mathf.Sin(t * 0.5f);
            float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
            float accent = hitGlow * 2.2f;
            float a = amplitude;

            // A little whole-body sway keeps the silhouette alive even between
            // drum strikes; the bone layer adds the marionette detail.
            if (root != null)
            {
                float yaw = Mathf.Lerp(2.2f * sway, 12f * Mathf.Sin(t * 0.85f), blend) * a;
                float roll = Mathf.Lerp(0.8f * sway, 7f * Mathf.Sin(t * 0.6f), blend) * a;
                root.localRotation = rootHomeRotation * Quaternion.Euler(0f, yaw, roll);
                Vector3 p = rootHomePosition;
                p.y += Mathf.Lerp(0f, 0.055f, blend) * bounce * a;
                root.localPosition = p;
            }

            Set(hip, hipHome, new Vector3(0f, Mathf.Lerp(1.8f * sway, 16f * Mathf.Sin(t), blend) * a,
                Mathf.Lerp(0f, 7f * Mathf.Sin(t * 0.65f), blend) * a));
            if (hip != null)
            {
                Vector3 p = hipHomePosition;
                p.y += Mathf.Lerp(0f, 0.045f, blend) * bounce * a;
                hip.localPosition = p;
            }

            Set(abdomen, abdomenHome,
                new Vector3(Mathf.Lerp(0.5f * sway, 8f * bounce + accent * 5f, blend) * a,
                    0f, Mathf.Lerp(-0.8f * sway, -10f * Mathf.Sin(t + 0.9f), blend) * a));
            Set(chest, chestHome,
                new Vector3(Mathf.Lerp(0.6f * sway, 7f * bounce + accent * 4f, blend) * a,
                    Mathf.Lerp(0f, 8f * Mathf.Sin(t * 0.7f), blend) * a,
                    Mathf.Lerp(0.7f * sway, 8f * Mathf.Sin(t + 0.4f), blend) * a));

            Set(neck, neckHome,
                new Vector3(Mathf.Lerp(1.5f * Mathf.Sin(t * 0.8f), 7f * Mathf.Sin(t * 2f) + accent * 7f, blend) * a,
                    Mathf.Lerp(3f * Mathf.Sin(t * 0.37f), 7f * Mathf.Sin(t * 0.7f), blend) * a,
                    Mathf.Lerp(-0.8f * sway, -2.5f * Mathf.Sin(t), blend) * a));
            Set(head, headHome,
                new Vector3(Mathf.Lerp(1.2f * Mathf.Sin(t * 0.6f), 5f * Mathf.Sin(t * 2.2f) + accent * 5f, blend) * a,
                    Mathf.Lerp(2f * Mathf.Sin(t * 0.45f), 5f * Mathf.Sin(t * 0.9f), blend) * a, 0f));

            float armSwing = Mathf.Sin(t * Mathf.PI) * 46f * blend * a;
            float armAccent = accent * 13f * a;
            Set(shoulderLeft, shoulderLeftHome, new Vector3(8f * Mathf.Sin(t * 1.2f) * blend * a,
                0f, -armSwing - armAccent));
            Set(shoulderRight, shoulderRightHome, new Vector3(-8f * Mathf.Sin(t * 1.2f) * blend * a,
                0f, armSwing - armAccent));

            float elbowLag = Mathf.Sin(t * Mathf.PI - 0.8f) * 28f * blend * a;
            Set(forearmLeft, forearmLeftHome, new Vector3(0f, 0f, -12f * blend - elbowLag));
            Set(forearmRight, forearmRightHome, new Vector3(0f, 0f, 12f * blend + elbowLag));

            float step = Mathf.Sin(t * Mathf.PI) * 25f * blend * a;
            Set(thighLeft, thighLeftHome, new Vector3(step, 0f, 0f));
            Set(thighRight, thighRightHome, new Vector3(-step, 0f, 0f));
            Set(shinLeft, shinLeftHome, new Vector3(-Mathf.Abs(step) * 1.15f, 0f, 0f));
            Set(shinRight, shinRightHome, new Vector3(-Mathf.Abs(step) * 1.15f, 0f, 0f));
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
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
