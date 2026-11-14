using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixBand.Interaction
{
    /// <summary>
    /// A single drum head / cymbal. Reacts to a grabbed stick touching it with
    /// enough speed: spatial one-shot, controller haptic, visible recoil, an
    /// expanding impact ring, dust, and a rhythm report to DrumPerformance.
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
        public float recoilDepth = 0.010f;
        public bool cymbal;
        public Color impactTint = new Color(1f, 0.74f, 0.44f);
        public Material ringMaterial;
        public ParticleSystem dust;

        private Vector3 headHome;
        private Quaternion headHomeRotation;
        private float lastHitTime = -10f;
        private DrumPerformance performance;
        private Transform ring;
        private Renderer ringRenderer;
        private MaterialPropertyBlock ringBlock;
        private float ringAge = 10f;
        private Coroutine motion;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();

            // The kit lives in a prefab, and prefab-instance field edits are not
            // guaranteed to survive into the built scene, so recover the visual
            // head from the hierarchy when it was not serialized.
            if (head == null)
            {
                Renderer visual = GetComponentInParent<Renderer>();
                if (visual != null) head = visual.transform;
            }

            if (head != null)
            {
                headHome = head.localPosition;
                headHomeRotation = head.localRotation;
            }

            if (!cymbal &&
                gameObject.name.IndexOf("Circle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                cymbal = true;

            performance = GetComponentInParent<DrumPerformance>();
            BuildRing();
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

            float strength = Mathf.Clamp01(stick.TipSpeed / 2.4f);

            if (dust != null)
            {
                dust.transform.position = transform.position + Vector3.up * 0.02f;
                dust.Emit(Mathf.RoundToInt(4f + strength * 10f));
            }

            if (performance != null) performance.RegisterHit(strength);

            ringAge = 0f;
            if (ring != null)
            {
                ring.position = transform.position + Vector3.up * 0.005f;
                ring.rotation = Quaternion.FromToRotation(Vector3.up, head != null ? head.up : Vector3.up);
                ring.gameObject.SetActive(true);
            }

            if (head != null && isActiveAndEnabled)
            {
                // A new strike owns the recoil: without this, overlapping
                // coroutines fight over head.localPosition and the dip stutters.
                if (motion != null) StopCoroutine(motion);
                motion = StartCoroutine(Motion(strength));
            }
        }

        private void Update()
        {
            if (ring == null || ringAge > 0.4f) return;

            ringAge += Time.deltaTime;
            float t = Mathf.Clamp01(ringAge / 0.32f);
            float eased = 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(0.16f, 0.72f, eased);
            ring.localScale = new Vector3(scale, 1f, scale);

            Color color = impactTint;
            color.a = 0.70f * (1f - t);
            ringBlock.SetColor("_Color", color);
            // Sprite shader uses _Color, the legacy particle shader _TintColor;
            // setting the missing one is harmless.
            ringBlock.SetColor("_TintColor", color);
            if (ringRenderer != null) ringRenderer.SetPropertyBlock(ringBlock);

            if (t >= 1f) ring.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator Motion(float strength)
        {
            // Damped dip: fast attack, springy overshoot back to rest.
            float t = 0f;
            float depth = recoilDepth * (0.65f + strength * 0.7f);
            const float duration = 0.24f;

            if (!cymbal)
            {
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float offset = depth * Mathf.Exp(-t * 19f) * Mathf.Cos(t * 44f);
                    head.localPosition = headHome + Vector3.down * offset;
                    yield return null;
                }
            }
            else
            {
                // Cymbals wobble around their mount instead of dipping.
                float wobble = 4.2f * (0.7f + strength * 0.6f);
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    float angle = wobble * Mathf.Exp(-t * 6.5f) * Mathf.Sin(t * 34f);
                    head.localRotation = headHomeRotation * Quaternion.Euler(angle, 0f, angle * 0.35f);
                    head.localPosition = headHome + Vector3.down * (depth * 0.35f * Mathf.Exp(-t * 16f));
                    yield return null;
                }
                head.localRotation = headHomeRotation;
            }

            head.localPosition = headHome;
        }

        private void BuildRing()
        {
            if (ringMaterial == null) ringMaterial = RuntimeRingMaterial();
            if (ringMaterial == null) return;

            GameObject go = new GameObject("Impact_Ring");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = RingMesh();

            ringRenderer = go.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = ringMaterial;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            ring = go.transform;
            ring.localScale = Vector3.zero;
            go.SetActive(false);

            ringBlock = new MaterialPropertyBlock();
            ringBlock.SetColor("_Color", impactTint);
            ringBlock.SetColor("_TintColor", impactTint);
            ringRenderer.SetPropertyBlock(ringBlock);
        }

        private static Material runtimeRingMaterial;
        private static Texture2D runtimeRingTexture;

        /// <summary>
        /// Fallback for scenes where the serialized ring material was lost with
        /// a prefab override: build the soft ring in code at startup.
        /// </summary>
        private static Material RuntimeRingMaterial()
        {
            if (runtimeRingMaterial != null) return runtimeRingMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) return null;

            runtimeRingMaterial = new Material(shader);
            runtimeRingMaterial.name = "Drum_Impact_Ring_Runtime";
            runtimeRingMaterial.mainTexture = RuntimeRingTexture();
            runtimeRingMaterial.color = Color.white;
            runtimeRingMaterial.hideFlags = HideFlags.DontSave;
            return runtimeRingMaterial;
        }

        private static Texture2D RuntimeRingTexture()
        {
            if (runtimeRingTexture != null) return runtimeRingTexture;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Drum_Impact_Ring_Runtime";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.74f) / 0.26f);
                    float inner = Mathf.Clamp01(1f - d) * 0.25f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring * ring + inner));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            runtimeRingTexture = texture;
            return texture;
        }

        private static Mesh ringMesh;

        private static Mesh RingMesh()
        {
            if (ringMesh != null) return ringMesh;

            const int segments = 40;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            Color[] colors = new Color[segments + 1];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            colors[0] = Color.white;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * 0.5f;
                float y = Mathf.Sin(angle) * 0.5f;
                vertices[i + 1] = new Vector3(x, 0f, y);
                uvs[i + 1] = new Vector2(x + 0.5f, y + 0.5f);
                colors[i + 1] = Color.white;

                int next = (i + 1) % segments;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
            }

            ringMesh = new Mesh();
            ringMesh.name = "ImpactRing";
            ringMesh.vertices = vertices;
            ringMesh.uv = uvs;
            ringMesh.colors = colors;
            ringMesh.triangles = triangles;
            ringMesh.RecalculateNormals();
            return ringMesh;
        }
    }
}
