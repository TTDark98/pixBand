using System.Collections;
using PixBand.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixBand.Core
{
    /// <summary>
    /// Owns the fade overlay and the emergency reset. The demo is a single scene,
    /// so reset teleports the player back to the forest spawn instead of loading.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        [Header("Spawn")]
        public Transform spawnPoint;

        [Header("Fade")]
        public float fadeOutDuration = 0.3f;
        public float fadeInDuration = 0.65f;

        private InputAction resetAction;
        private bool transitioning;
        private FadeOverlay overlay;

        public bool IsTransitioning { get { return transitioning; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("[SceneFlowManager]");
            go.AddComponent<SceneFlowManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (spawnPoint == null)
            {
                GameObject spawn = GameObject.Find("SpawnPoint");
                if (spawn != null) spawnPoint = spawn.transform;
            }

            resetAction = new InputAction("ResetToForest", InputActionType.Button);
            resetAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            resetAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            resetAction.AddBinding("<Keyboard>/r");
            resetAction.performed += OnResetPerformed;
            resetAction.Enable();
        }

        private void OnDestroy()
        {
            if (resetAction != null)
            {
                resetAction.performed -= OnResetPerformed;
                resetAction.Disable();
                resetAction.Dispose();
            }
        }

        private void OnResetPerformed(InputAction.CallbackContext ctx)
        {
            Respawn();
        }

        private void Update()
        {
            if (overlay != null) return;
            overlay = FadeOverlay.Create();
            overlay.SetAlpha(0f);
        }

        public void Respawn()
        {
            if (transitioning) return;
            StartCoroutine(DoRespawn());
        }

        private IEnumerator DoRespawn()
        {
            transitioning = true;

            if (overlay == null) overlay = FadeOverlay.Create();
            yield return overlay.FadeTo(1f, fadeOutDuration);

            DropHeldSticks();
            MoveRigsToSpawn();

            // One frame so the new camera position is applied before fading in.
            yield return null;

            if (overlay != null) yield return overlay.FadeTo(0f, fadeInDuration);
            transitioning = false;
        }

        private void DropHeldSticks()
        {
            DrumStick[] sticks = FindObjectsOfType<DrumStick>();
            for (int i = 0; i < sticks.Length; i++)
                if (sticks[i] != null) sticks[i].ForceRelease();

            DesktopStickGrabber grabber = FindObjectOfType<DesktopStickGrabber>();
            if (grabber != null) grabber.Drop();
        }

        private void MoveRigsToSpawn()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[SceneFlowManager] No spawn point; reset does nothing.");
                return;
            }

            DualRigManager rigs = FindObjectOfType<DualRigManager>();
            if (rigs != null)
            {
                rigs.Respawn(spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                GameObject fp = GameObject.Find("FirstPersonRig");
                if (fp != null) fp.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
                GameObject xr = GameObject.Find("XROriginRig");
                if (xr != null) xr.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            DesktopRigController desktop = FindObjectOfType<DesktopRigController>();
            if (desktop != null) desktop.ResetView();
        }
    }

    /// <summary>
    /// A black quad parented to the active camera. Created once; works on desktop
    /// and in stereo.
    /// </summary>
    public class FadeOverlay : MonoBehaviour
    {
        private const float Distance = 0.5f;

        private MeshRenderer meshRenderer;
        private Material material;
        private Color color = new Color(0f, 0f, 0f, 0f);

        public static FadeOverlay Create()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "[FadeOverlay]";

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            FadeOverlay overlay = go.AddComponent<FadeOverlay>();
            overlay.Initialize();
            return overlay;
        }

        private void Initialize()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            material = new Material(shader);
            material.color = color;
            material.renderQueue = 4000;
            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            transform.localScale = new Vector3(4f, 4f, 1f);
            AttachToCamera();
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null && transform.parent != cam.transform)
                AttachToCamera();
        }

        private void AttachToCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            transform.SetParent(cam.transform, false);
            transform.localPosition = new Vector3(0f, 0f, Distance);
            transform.localRotation = Quaternion.identity;
        }

        public void SetAlpha(float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            if (material != null) material.color = color;
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            float start = color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, t / Mathf.Max(0.01f, duration)));
                yield return null;
            }

            SetAlpha(target);
        }
    }
}
