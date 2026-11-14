using UnityEngine;
using PixBand.Core;

namespace PixBand.Audio
{
    /// <summary>
    /// Distance-paced footfalls using recorded grass and mansion-floor foley.
    /// The ground collider selects the surface; audio clips are project assets.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RecordedFootsteps : MonoBehaviour
    {
        [Header("Movement")]
        public Transform movementOrigin;
        [Min(0.15f)] public float stepDistance = 0.62f;
        [Min(0.1f)] public float minimumSpeed = 0.25f;
        [Min(1f)] public float groundRayDistance = 3.5f;
        [Range(0f, 0.2f)] public float stepLead = 0.09f;

        [Header("Recorded Footsteps")]
        [Range(0f, 1f)] public float volume = 0.5f;
        [Range(0f, 1f)] public float spatialBlend = 0f;
        public AudioClip[] grassSteps;
        public AudioClip[] hardFloorSteps;

        [Header("Feel")]
        public bool hapticTick = true;
        [Range(0f, 1f)] public float hapticAmplitude = 0.12f;
        public float hapticDuration = 0.02f;

        private AudioSource source;
        private PlayerBodyVisuals bodyVisuals;
        private Vector3 lastPosition;
        private float walkedDistance;
        private int nextGrassClip;
        private RaycastHit[] groundHits;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            bodyVisuals = GetComponent<PlayerBodyVisuals>();
            if (movementOrigin == null)
            {
                Camera camera = GetComponentInChildren<Camera>(true);
                if (camera != null) movementOrigin = camera.transform;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.volume = volume;
            source.priority = 150;
            source.dopplerLevel = 0f;
            // Keep the transient centered on the listener instead of sounding
            // like a distant follow-camera cue attached to the rig's feet.
            source.spatialBlend = 0f;
            source.minDistance = 0f;
            source.maxDistance = 1f;
            groundHits = new RaycastHit[16];

            // The CC0 one-shots are small, but explicitly load them before the
            // first walk so streaming/import latency cannot swallow that step.
            Preload(grassSteps);
            Preload(hardFloorSteps);
        }

        private void Start()
        {
            if (bodyVisuals == null) bodyVisuals = GetComponent<PlayerBodyVisuals>();
            if (bodyVisuals != null)
            {
                bodyVisuals.stepDistance = stepDistance;
                bodyVisuals.FootLanded += OnFootLanded;
            }
        }

        private void OnEnable()
        {
            if (movementOrigin != null) lastPosition = movementOrigin.position;
            walkedDistance = 0f;
        }

        private void Update()
        {
            // In the body-aware rigs, audio is triggered by the animated foot
            // reaching its landing point, not by an independent camera timer.
            if (bodyVisuals != null) return;
            if (movementOrigin == null) return;

            Vector3 position = movementOrigin.position;
            Vector3 delta = position - lastPosition;
            lastPosition = position;
            delta.y = 0f;

            float distance = delta.magnitude;
            float speed = Time.deltaTime > 0.0001f ? distance / Time.deltaTime : 0f;
            if (distance > 1.5f || speed < minimumSpeed) return;

            walkedDistance += distance;
            if (walkedDistance < stepDistance - stepLead) return;

            walkedDistance = Mathf.Max(0f, walkedDistance - stepDistance);
            PlayStepForSurface(movementOrigin.position);
        }

        private void OnFootLanded(Vector3 footPosition)
        {
            PlayStepForSurface(footPosition);
        }

        private void PlayStepForSurface(Vector3 footPosition)
        {
            int count = Physics.RaycastNonAlloc(
                footPosition + Vector3.up * 0.35f,
                Vector3.down,
                groundHits,
                groundRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            RaycastHit closest = default(RaycastHit);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.collider == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;
                if (!found || hit.distance < closest.distance)
                {
                    closest = hit;
                    found = true;
                }
            }

            if (!found) return;

            AudioClip[] surfaceClips;
            if (IsSoftGround(closest.collider))
            {
                surfaceClips = grassSteps;
            }
            else if (IsPartOfMansion(closest.transform))
            {
                surfaceClips = hardFloorSteps;
            }
            else
            {
                return;
            }

            if (surfaceClips == null || surfaceClips.Length == 0) return;
            int index = IsSoftGround(closest.collider)
                ? nextGrassClip++ % surfaceClips.Length
                : Random.Range(0, surfaceClips.Length);

            AudioClip clip = surfaceClips[index];
            if (clip == null) return;

            bool softGround = IsSoftGround(closest.collider);
            source.pitch = Random.Range(0.97f, 1.03f) * (softGround ? 1f : 0.98f);
            source.PlayOneShot(clip, Random.Range(0.90f, 1f));
            SendHapticTick();
        }

        private static bool IsSoftGround(Collider ground)
        {
            return ground != null &&
                   (ground.name == "Forest_Terrain" || ground.name == "Path");
        }

        private static void Preload(AudioClip[] clips)
        {
            if (clips == null) return;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && !clips[i].preloadAudioData &&
                    clips[i].loadState == AudioDataLoadState.Unloaded)
                    clips[i].LoadAudioData();
            }
        }

        private void SendHapticTick()
        {
            if (!hapticTick) return;

            try
            {
                UnityEngine.XR.InputDevice left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                if (left.isValid) left.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);

                UnityEngine.XR.InputDevice right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                if (right.isValid) right.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
            }
            catch (System.Exception)
            {
                // No XR input in the desktop player; footsteps still play.
            }
        }

        private static bool IsPartOfMansion(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (current.name == "Mansion") return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (bodyVisuals != null) bodyVisuals.FootLanded -= OnFootLanded;
        }
    }
}
