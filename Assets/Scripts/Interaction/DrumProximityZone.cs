using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixBand.Interaction
{
    /// <summary>
    /// Keeps the kit asleep until the player is close. Enables grabbing, shows the
    /// prompt, and starts the room tone. Force-releases held sticks when leaving.
    /// </summary>
    public class DrumProximityZone : MonoBehaviour
    {
        [Header("Detection")]
        public Transform player;
        public float radius = 3.2f;
        public float exitRadius = 3.8f;
        public float checkInterval = 0.15f;

        [Header("Content")]
        public DrumStick[] sticks;
        public GameObject promptRoot;
        public AudioSource roomTone;
        public Renderer[] highlightRenderers;
        public Color highlightColor = new Color(0.35f, 0.9f, 0.7f);

        private bool active;
        private float timer;
        private MaterialPropertyBlock block;

        public bool IsActive { get { return active; } }

        private void Start()
        {
            if (player == null)
            {
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener != null) player = listener.transform;
            }

            SetActive(false);
            timer = checkInterval;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            if (player == null)
            {
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener != null) player = listener.transform;
                if (player == null) return;
            }

            Vector3 a = player.position;
            Vector3 b = transform.position;
            a.y = 0f;
            b.y = 0f;
            float distance = Vector3.Distance(a, b);
            // Separate enter/exit thresholds prevent rapid enable/force-release
            // cycles as the player stands on the boundary or the XR head bobs.
            bool inside = active ? distance <= Mathf.Max(radius, exitRadius) : distance <= radius;

            if (inside != active) SetActive(inside);
        }

        public void SetActive(bool on)
        {
            active = on;

            if (sticks != null)
            {
                for (int i = 0; i < sticks.Length; i++)
                {
                    DrumStick stick = sticks[i];
                    if (stick == null) continue;

                    if (!on)
                    {
                        stick.ForceRelease();
                    }

                    if (stick.Grab != null) stick.Grab.enabled = on;
                }
            }

            if (promptRoot != null) promptRoot.SetActive(on);

            if (roomTone != null)
            {
                if (on)
                {
                    if (!roomTone.isPlaying) roomTone.Play();
                }
                else if (roomTone.isPlaying)
                {
                    roomTone.Stop();
                }
            }

            ApplyHighlight(on);
        }

        private void ApplyHighlight(bool on)
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0) return;
            if (block == null) block = new MaterialPropertyBlock();

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                Renderer r = highlightRenderers[i];
                if (r == null) continue;

                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", on ? highlightColor : Color.black);
                r.SetPropertyBlock(block);
            }
        }
    }
}
