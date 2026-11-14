using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixBand.Ambient
{
    /// <summary>
    /// The entrance cat. Touching it with a tracked hand (or clicking it on the
    /// desktop rig) plays a purr and one of the supplied meows. Petting is pure
    /// flavour: it never unlocks the skeletons or changes the drum performance.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PetableCat : MonoBehaviour
    {
        [Header("Audio")]
        public AudioClip purrClip;
        public AudioClip[] meows;
        [Range(0f, 1f)] public float purrVolume = 0.45f;
        [Range(0f, 1f)] public float meowVolume = 0.7f;

        [Header("Interaction")]
        public float petRange = 5.5f;
        public float cooldown = 2.4f;
        public float purrDuration = 3.4f;

        [Header("Idle Animation")]
        public Animation idleAnimation;
        public AnimationClip idleClip;

        [Header("Pet Reaction")]
        [Min(0.05f)] public float reactionInTime = 0.22f;
        [Min(0.05f)] public float reactionOutTime = 0.65f;
        [Range(0f, 60f)] public float idleGazePitch = 45f;
        [Min(0f)] public float watchRadius = 7f;
        [Range(30f, 360f)] public float bodyTurnSpeed = 180f;

        private AudioSource purrSource;
        private AudioSource voiceSource;
        private XRSimpleInteractable interactable;
        private float nextPetTime;
        private Camera viewCamera;
        private Transform headBone;
        private float reactionWeight;
        private float reactionVelocity;
        private float reactionUntil;

        private void Awake()
        {
            purrSource = GetComponent<AudioSource>();
            purrSource.playOnAwake = false;
            purrSource.loop = false;
            purrSource.spatialBlend = 0.55f;
            purrSource.rolloffMode = AudioRolloffMode.Linear;
            purrSource.minDistance = 0.6f;
            purrSource.maxDistance = 12f;
            purrSource.volume = purrVolume;
            purrSource.priority = 120;

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0.55f;
            voiceSource.rolloffMode = AudioRolloffMode.Linear;
            voiceSource.minDistance = 0.6f;
            voiceSource.maxDistance = 12f;
            voiceSource.volume = meowVolume;
            voiceSource.priority = 118;

            interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.selectEntered.AddListener(OnSelectEntered);
            }

            ConfigureIdleAnimation();
            FindFaceBones(transform);
        }

        private void FindFaceBones(Transform root)
        {
            Transform[] bones = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name == "j_head_08") headBone = bones[i];
            }
        }

        private void LateUpdate()
        {
            if (headBone == null) return;

            float target = Time.time < reactionUntil ? 1f : 0f;
            float duration = target > reactionWeight ? reactionInTime : reactionOutTime;
            reactionWeight = Mathf.SmoothDamp(reactionWeight, target, ref reactionVelocity,
                Mathf.Max(0.05f, duration));

            // The grooming clip leaves the cat staring at the ground. Correct the
            // gaze after the clip has evaluated: raised toward the player while
            // idle, and up at the player's face while being petted.
            if (viewCamera == null) viewCamera = Camera.main;

            // The cat is placed by the entrance, so players may approach from
            // either side of the doorway. Turn its whole seated body toward the
            // active player; otherwise the head can turn while the back still
            // faces them.
            if (viewCamera != null)
            {
                Vector3 toPlayer = viewCamera.transform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f && toPlayer.magnitude <= watchRadius + 2f)
                {
                    Vector3 direction = toPlayer.normalized;
                    float yaw = Mathf.Atan2(direction.z, -direction.x) * Mathf.Rad2Deg;
                    Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, facing,
                        bodyTurnSpeed * Time.deltaTime);
                }
            }

            // The imported mesh faces model-local -X; the cat root's forward
            // vector is not its muzzle direction because the source rig is
            // quarter-turned inside the FBX.
            Vector3 flat = transform.TransformDirection(Vector3.left).normalized;
            float pitch = idleGazePitch * Mathf.Deg2Rad;
            Vector3 idleDirection = (flat * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;
            Vector3 targetDirection = idleDirection;
            if (viewCamera != null)
            {
                Vector3 toPlayer = viewCamera.transform.position - headBone.position;
                float distance = toPlayer.magnitude;
                if (distance > 0.0001f)
                {
                    Vector3 playerDirection = toPlayer / distance;
                    // Watch the player as they approach, but do not spin the
                    // head around for someone standing behind the cat.
                    if (distance <= watchRadius && Vector3.Dot(flat, playerDirection) > 0.05f)
                        idleDirection = Vector3.Slerp(idleDirection, playerDirection, 0.8f);

                    targetDirection = reactionWeight > 0.001f
                        ? Vector3.Slerp(idleDirection, playerDirection, reactionWeight)
                        : idleDirection;
                }
            }

            Quaternion aimCorrection = Quaternion.FromToRotation(headBone.forward, targetDirection);
            headBone.rotation = Quaternion.Slerp(Quaternion.identity, aimCorrection, 0.95f) * headBone.rotation;
        }

        /// <summary>Loop the cat's grooming clip from the imported FBX.</summary>
        private void ConfigureIdleAnimation()
        {
            if (idleAnimation == null) idleAnimation = GetComponentInChildren<Animation>();
            if (idleAnimation == null) return;

            AnimationClip clip = idleClip != null ? idleClip : idleAnimation.clip;
            if (clip == null) return;

            if (idleAnimation.GetClip(clip.name) == null) idleAnimation.AddClip(clip, clip.name);
            clip.wrapMode = WrapMode.Loop;
            idleAnimation.clip = clip;
            idleAnimation.wrapMode = WrapMode.Loop;
            idleAnimation.Play(clip.name);
        }

        private void OnDestroy()
        {
            if (interactable == null) return;
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        private void Update()
        {
            // Desktop rig: a left click aimed at the cat pets it. The same click
            // is used for drumming, so only respond when the cat is under the
            // crosshair and within arm's reach.
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (!Input.GetMouseButtonDown(0)) return;

            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            Ray ray = viewCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, petRange, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider == null) return;
            PetableCat cat = hit.collider.GetComponentInParent<PetableCat>();
            if (cat == this) Pet();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            Pet();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Pet();
        }

        public void Pet()
        {
            if (Time.time < nextPetTime) return;
            nextPetTime = Time.time + cooldown;
            reactionUntil = Time.time + 1.15f;

            if (purrClip != null)
            {
                purrSource.Stop();
                purrSource.clip = purrClip;
                purrSource.volume = purrVolume;
                purrSource.Play();
                CancelInvoke(nameof(StopPurr));
                Invoke(nameof(StopPurr), purrDuration);
            }

            if (meows != null && meows.Length > 0)
            {
                CancelInvoke(nameof(PlayMeow));
                Invoke(nameof(PlayMeow), 0.22f);
            }
        }

        private void PlayMeow()
        {
            if (meows == null || meows.Length == 0) return;
            AudioClip clip = meows[Random.Range(0, meows.Length)];
            if (clip == null) return;
            voiceSource.pitch = Random.Range(0.94f, 1.06f);
            voiceSource.PlayOneShot(clip, meowVolume);
        }

        private void StopPurr()
        {
            if (purrSource != null) purrSource.Stop();
        }
    }
}
