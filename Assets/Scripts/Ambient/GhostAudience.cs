using UnityEngine;
using PixBand.Interaction;

namespace PixBand.Ambient
{
    /// <summary>
    /// Ghost silhouettes behind the mansion windows. They stay hidden until the
    /// player holds a rhythm streak, then fade in and sway gently.
    /// </summary>
    public class GhostAudience : MonoBehaviour
    {
        [Header("Trigger")]
        public DrumPerformance performance;
        public int comboThreshold = 6;
        public float fadeSpeed = 1.4f;

        [Header("Motion")]
        public float bobHeight = 0.035f;
        public float swayDegrees = 2.4f;
        [Range(0f, 1f)] public float maxOpacity = 0.72f;

        public Renderer[] silhouettes;

        private MaterialPropertyBlock block;
        private Vector3[] homePositions;
        private Quaternion[] homeRotations;
        private float[] phases;
        private float[] opacity;
        private Color[] baseColors;
        private float fade;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (silhouettes == null || silhouettes.Length == 0)
                silhouettes = GetComponentsInChildren<Renderer>(true);

            int count = silhouettes.Length;
            homePositions = new Vector3[count];
            homeRotations = new Quaternion[count];
            phases = new float[count];
            opacity = new float[count];
            baseColors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                Renderer renderer = silhouettes[i];
                if (renderer == null) continue;
                homePositions[i] = renderer.transform.localPosition;
                homeRotations[i] = renderer.transform.localRotation;
                phases[i] = i * 1.7f + 0.4f;
                baseColors[i] = renderer.sharedMaterial != null
                    ? renderer.sharedMaterial.color
                    : new Color(0.05f, 0.06f, 0.08f, 1f);
            }

            // Hide them before the first frame; otherwise the shared material
            // alpha shows every ghost for one frame at load.
            Apply();
        }

        private void Start()
        {
            if (performance == null) performance = FindObjectOfType<DrumPerformance>();
            Apply();
        }

        private void Update()
        {
            bool playingWell = performance != null && performance.combo >= comboThreshold;
            fade = Mathf.MoveTowards(fade, playingWell ? 1f : 0f, Time.deltaTime * fadeSpeed);

            if (silhouettes == null) return;

            for (int i = 0; i < silhouettes.Length; i++)
            {
                Renderer renderer = silhouettes[i];
                if (renderer == null) continue;

                float phase = phases[i];
                float bob = Mathf.Sin(Time.time * 1.15f + phase) * bobHeight;
                float sway = Mathf.Sin(Time.time * 0.62f + phase * 0.7f) * swayDegrees;

                Transform t = renderer.transform;
                t.localPosition = homePositions[i] + Vector3.up * bob;
                t.localRotation = homeRotations[i] * Quaternion.Euler(0f, 0f, sway);

                // Stagger the fade so the windows do not light up in lockstep.
                float stagger = Mathf.Clamp01(fade * 1.6f - (i % 4) * 0.18f);
                float target = maxOpacity * stagger;
                opacity[i] = Mathf.MoveTowards(opacity[i], target, Time.deltaTime * 0.9f);
            }

            Apply();
        }

        private void Apply()
        {
            if (silhouettes == null || block == null) return;
            for (int i = 0; i < silhouettes.Length; i++)
            {
                Renderer renderer = silhouettes[i];
                if (renderer == null) continue;

                Color color = baseColors != null && i < baseColors.Length
                    ? baseColors[i]
                    : Color.black;
                color.a = opacity != null && i < opacity.Length ? opacity[i] : 0f;
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
