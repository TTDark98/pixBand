using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using PixBand.Core;

namespace PixBand.Interaction
{
    /// <summary>
    /// A drum stick the player can grab. Tracks tip speed (kinematic bodies report no
    /// usable velocity) and returns to its stand socket when released.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class DrumStick : MonoBehaviour
    {
        [Header("References")]
        public Transform tip;
        public Transform restSocket;

        [Header("Return")]
        public float returnDuration = 0.22f;

        private XRGrabInteractable grab;
        private Rigidbody body;
        private Vector3 lastTipPosition;
        private bool held;
        private bool externalHeld;
        private Transform externalHolder;
        private Coroutine returnRoutine;
        private float nextHandPulseTime;

        public float TipSpeed { get; private set; }
        public bool IsHeld { get { return held; } }
        public XRGrabInteractable Grab { get { return grab; } }

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            if (tip == null) tip = transform;

            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.selectMode = InteractableSelectMode.Multiple;
            grab.useDynamicAttach = false;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.snapToColliderVolume = false;
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);

            body.useGravity = false;
            body.isKinematic = true;

            lastTipPosition = tip.position;
        }

        private void OnDestroy()
        {
            if (grab == null) return;
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }

        private void LateUpdate()
        {
            if (externalHeld && externalHolder != null)
                transform.SetPositionAndRotation(externalHolder.position, externalHolder.rotation);
        }

        private void FixedUpdate()
        {
            if (!held)
            {
                TipSpeed = 0f;
                return;
            }

            float dt = Time.fixedDeltaTime;
            Vector3 p = tip.position;
            if (dt > 0.0001f) TipSpeed = Vector3.Distance(p, lastTipPosition) / dt;
            lastTipPosition = p;

            if (TipSpeed > 2f && Time.time >= nextHandPulseTime && grab.interactorsSelecting.Count > 0)
            {
                Component interactor = grab.interactorsSelecting[0] as Component;
                if (interactor != null)
                {
                    PlayerBodyVisuals[] visuals = FindObjectsOfType<PlayerBodyVisuals>();
                    for (int i = 0; i < visuals.Length; i++)
                        visuals[i].PulseHandFromInteractor(interactor.transform);
                    nextHandPulseTime = Time.time + 0.18f;
                }
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            held = true;
            lastTipPosition = tip.position;
            SetInteractorHandHeld(args.interactorObject, true);
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (externalHeld) return;
            if (grab != null && grab.interactorsSelecting.Count > 0)
            {
                held = true;
                return;
            }
            SetInteractorHandHeld(args.interactorObject, false);
            BeginReturn();
        }

        private static void SetInteractorHandHeld(IXRSelectInteractor interactor, bool isHeld)
        {
            Component component = interactor as Component;
            if (component == null) return;
            PlayerBodyVisuals[] visuals = FindObjectsOfType<PlayerBodyVisuals>();
            for (int i = 0; i < visuals.Length; i++)
                visuals[i].SetHandHeldFromInteractor(component.transform, isHeld);
        }

        /// <summary>Grab without an XR controller (desktop mouse/keyboard testing).</summary>
        public void ExternalGrab(Transform holder)
        {
            externalHeld = true;
            externalHolder = holder;
            held = true;
            lastTipPosition = tip.position;

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }

            if (grab != null) grab.enabled = false;
        }

        /// <summary>Release a desktop grab and send the stick back to its socket.</summary>
        public void ExternalRelease()
        {
            if (!externalHeld) return;
            externalHeld = false;
            externalHolder = null;
            if (grab != null) grab.enabled = true;
            BeginReturn();
        }

        /// <summary>Release the stick no matter how it is held. Used by reset / proximity exit.</summary>
        public void ForceRelease()
        {
            if (externalHeld)
            {
                ExternalRelease();
                return;
            }

            if (grab == null) return;
            while (grab.interactorsSelecting.Count > 0)
            {
                IXRSelectInteractor interactor = grab.interactorsSelecting[0];
                if (grab.interactionManager == null) break;
                grab.interactionManager.SelectExit(interactor, grab);
            }
        }

        private void BeginReturn()
        {
            held = false;
            TipSpeed = 0f;
            if (restSocket != null && isActiveAndEnabled)
                returnRoutine = StartCoroutine(ReturnToSocket());
        }

        private System.Collections.IEnumerator ReturnToSocket()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, returnDuration);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                transform.position = Vector3.Lerp(startPos, restSocket.position, e);
                transform.rotation = Quaternion.Slerp(startRot, restSocket.rotation, e);
                yield return null;
            }

            transform.SetPositionAndRotation(restSocket.position, restSocket.rotation);
            returnRoutine = null;
        }

        public XRBaseControllerInteractor GetGrabbingInteractor()
        {
            if (grab == null || grab.interactorsSelecting.Count == 0) return null;
            return grab.interactorsSelecting[0] as XRBaseControllerInteractor;
        }
    }
}
