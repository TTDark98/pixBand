using System.Collections;
using System.Collections.Generic;
using PixBand.Interaction;
using UnityEngine;

namespace PixBand.Core
{
    /// <summary>
    /// Desktop/laptop drumming fallback: E grabs or drops the nearest stick, the
    /// mouse aims, and left click strikes whatever pad is under the crosshair.
    /// </summary>
    public class DesktopStickGrabber : MonoBehaviour
    {
        [Header("References")]
        public Transform head;

        [Header("Grab")]
        public float grabRadius = 3.4f;
        public float strikeRange = 3.6f;
        public float strikeRadius = 0.22f;

        [Header("Stick pose")]
        // Desktop grips belong to the torso, not the pitched camera. Looking
        // down should bring the hands into view instead of tipping them away.
        public Vector3 holderOffset = new Vector3(0.28f, 1.12f, 0.42f);
        public float restPitch = 58f;
        public float strikePitch = 42f;
        public float strikeDuration = 0.16f;

        private readonly List<DrumStick> held = new List<DrumStick>(2);
        private readonly List<Transform> holders = new List<Transform>(2);
        private DrumProximityZone zone;
        private readonly float[] swings = new float[2];
        private readonly Coroutine[] swingRoutines = new Coroutine[2];
        private int nextStrikeHand;

        public bool IsHolding { get { return held.Count > 0; } }
        public int HeldStickCount { get { return held.Count; } }

        private void Awake()
        {
            if (head == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }

            for (int i = 0; i < 2; i++)
            {
                Transform holder = new GameObject("DesktopStickHolder_" + (i + 1)).transform;
                holder.SetParent(transform, false);
                float side = i == 0 ? -1f : 1f;
                holder.localPosition = new Vector3(side * Mathf.Abs(holderOffset.x), holderOffset.y, holderOffset.z);
                holder.localRotation = Quaternion.Euler(restPitch, 0f, side * 6f);
                holders.Add(holder);
            }

            PlayerBodyVisuals body = GetComponent<PlayerBodyVisuals>();
            if (body != null)
            {
                // Both hands use one consistent palms-down pose around the sticks.
                body.SetHandPose(
                    new Vector3(0.015f, -0.04f, -0.025f), new Vector3(180f, 0f, 90f),
                    new Vector3(0.035f, -0.09f, -0.055f), new Vector3(180f, 0f, -90f));
                body.SetHandTargets(holders[0], holders[1]);
            }
        }

        private void Update()
        {
            if (head == null) return;

            // The proximity zone releases the stick when the player walks away.
            if (held.Count > 0 && zone != null && !zone.IsActive)
                Drop();

            // Only play while the menu is closed (cursor is locked during play).
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (held.Count < 2) TryGrab();
                else Drop();
            }

            if (held.Count > 0 && Input.GetMouseButtonDown(0))
                Strike();

            for (int i = 0; i < holders.Count && i < swings.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                holders[i].localRotation = Quaternion.Euler(restPitch + swings[i], 0f, side * 6f);
            }
        }

        private void TryGrab()
        {
            if (zone == null) zone = FindObjectOfType<DrumProximityZone>();
            if (zone != null && !zone.IsActive) return;

            DrumStick[] sticks = FindObjectsOfType<DrumStick>();
            if (held.Count >= 2) return;

            DrumStick best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < sticks.Length; i++)
            {
                DrumStick stick = sticks[i];
                if (stick == null || stick.IsHeld || held.Contains(stick)) continue;

                float distance = Vector3.Distance(head.position, stick.transform.position);
                if (distance < bestDistance && distance <= grabRadius)
                {
                    best = stick;
                    bestDistance = distance;
                }
            }

            if (best == null) return;

            int hand = held.Count;
            held.Add(best);
            best.ExternalGrab(holders[hand]);
            PlayerBodyVisuals visuals = GetComponent<PlayerBodyVisuals>();
            if (visuals != null) visuals.SetHandHeld(hand, true);
        }

        private void Strike()
        {
            if (held.Count == 0 || head == null) return;

            int hand = nextStrikeHand % held.Count;
            nextStrikeHand++;
            if (swingRoutines[hand] != null) StopCoroutine(swingRoutines[hand]);
            swingRoutines[hand] = StartCoroutine(Swing(hand));
            PlayerBodyVisuals visuals = GetComponent<PlayerBodyVisuals>();
            if (visuals != null) visuals.PulseHand(hand);

            RaycastHit[] hits = Physics.SphereCastAll(
                head.position,
                strikeRadius,
                head.forward,
                strikeRange,
                ~0,
                QueryTriggerInteraction.Collide);

            DrumPad nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                DrumPad pad = hits[i].collider.GetComponentInParent<DrumPad>();
                if (pad == null || hits[i].distance >= nearestDistance) continue;
                nearest = pad;
                nearestDistance = hits[i].distance;
            }

            if (nearest != null) nearest.TryStrike(held[hand]);
        }

        private IEnumerator Swing(int hand)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, strikeDuration);
                float curve = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                swings[hand] = strikePitch * curve;
                yield return null;
            }

            swings[hand] = 0f;
            swingRoutines[hand] = null;
        }

        public void Drop()
        {
            for (int i = held.Count - 1; i >= 0; i--)
            {
                if (held[i] != null) held[i].ExternalRelease();
                PlayerBodyVisuals visuals = GetComponent<PlayerBodyVisuals>();
                if (visuals != null) visuals.SetHandHeld(i, false);
                held.RemoveAt(i);
                swings[i] = 0f;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < holders.Count; i++)
                if (holders[i] != null) Destroy(holders[i].gameObject);
        }
    }
}
