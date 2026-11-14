using UnityEngine;

namespace PixBand.Ambient
{
    /// <summary>
    /// Keeps the mansion interior clear: suppresses the global outdoor distance
    /// fog while the player is inside, and fades the huge world-space mist cards
    /// out before their billboards can reach the walls or drift through the door.
    /// </summary>
    public class MansionInteriorFog : MonoBehaviour
    {
        [Min(0f)] public float halfWidth = 4.45f;
        [Min(0f)] public float halfDepth = 4.35f;
        [Min(0f)] public float height = 5.6f;

        [Header("Outdoor mist containment")]
        [Min(0f)] public float mistPadding = 0.35f;
        [Min(0.1f)] public float mistFadeBand = 2.5f;

        private static readonly string[] MistNames =
        {
            "Low_Forest_Mist_A",
            "Low_Forest_Mist_B",
            "High_Forest_Haze"
        };

        private bool originalFog;
        private bool fogCaptured;
        private ParticleSystem[] mistSystems;
        private Color[] mistColors;
        private ParticleSystem.Particle[][] mistBuffers;

        private void Awake()
        {
            mistSystems = new ParticleSystem[MistNames.Length];
            mistColors = new Color[MistNames.Length];
            mistBuffers = new ParticleSystem.Particle[MistNames.Length][];
            for (int i = 0; i < MistNames.Length; i++)
            {
                GameObject go = GameObject.Find(MistNames[i]);
                if (go == null) continue;
                ParticleSystem system = go.GetComponent<ParticleSystem>();
                if (system == null) continue;
                mistSystems[i] = system;
                mistColors[i] = system.main.startColor.color;
            }
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) camera = FindActiveCamera();
            if (camera == null) return;

            if (!fogCaptured)
            {
                originalFog = RenderSettings.fog;
                fogCaptured = true;
            }

            Vector3 local = transform.InverseTransformPoint(camera.transform.position);
            bool inside = Mathf.Abs(local.x) <= halfWidth &&
                          Mathf.Abs(local.z) <= halfDepth &&
                          local.y >= -0.1f && local.y <= height;
            RenderSettings.fog = inside ? false : originalFog;

            ContainMist();
        }

        /// <summary>
        /// The forest mist uses single billboards several metres across. Even a
        /// card centred outside the walls reaches into the rooms, so fade each
        /// particle by how close its visible edge is to the mansion volume and
        /// retire the ones that would otherwise sit inside.
        /// </summary>
        private void ContainMist()
        {
            if (mistSystems == null) return;

            for (int s = 0; s < mistSystems.Length; s++)
            {
                ParticleSystem system = mistSystems[s];
                if (system == null) continue;

                int count = system.particleCount;
                if (count == 0) continue;

                if (mistBuffers[s] == null || mistBuffers[s].Length < count)
                    mistBuffers[s] = new ParticleSystem.Particle[count];
                ParticleSystem.Particle[] particles = mistBuffers[s];
                system.GetParticles(particles);

                for (int i = 0; i < count; i++)
                {
                    Vector3 local = transform.InverseTransformPoint(particles[i].position);
                    float dx = Mathf.Max(0f, Mathf.Abs(local.x) - (halfWidth + mistPadding));
                    float dz = Mathf.Max(0f, Mathf.Abs(local.z) - (halfDepth + mistPadding));
                    float dy = Mathf.Max(0f, Mathf.Max(-0.1f - local.y, local.y - height));
                    float distance = Mathf.Sqrt(dx * dx + dz * dz + dy * dy);

                    // A billboard only counts as a hazard once its own radius
                    // reaches the house, not just its centre.
                    float edgeDistance = Mathf.Max(0f, distance - particles[i].GetCurrentSize(system) * 0.45f);
                    float fade = Mathf.Clamp01(edgeDistance / mistFadeBand);

                    Color color = mistColors[s];
                    color.a *= fade;
                    particles[i].startColor = color;

                    if (fade <= 0.001f) particles[i].remainingLifetime = 0f;
                }

                system.SetParticles(particles, count);
            }
        }

        private static Camera FindActiveCamera()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled) return cameras[i];
            }
            return null;
        }

        private void OnDisable()
        {
            if (fogCaptured) RenderSettings.fog = originalFog;
        }
    }
}
