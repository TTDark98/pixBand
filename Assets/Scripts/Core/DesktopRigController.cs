using UnityEngine;

namespace PixBand.Core
{
    /// <summary>
    /// Non-VR fallback rig: WASD + mouse look with a CharacterController.
    /// Used by the Linux desktop build and for fast editor iteration.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopRigController : MonoBehaviour
    {
        public Transform head;
        public float moveSpeed = 2.0f;
        public float mouseSensitivity = 2.0f;
        public float gravity = -12f;
        public bool lockCursorOnStart = true;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            if (head == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }
            if (lockCursorOnStart) SetCursorLocked(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
            if (Input.GetMouseButtonDown(0)) SetCursorLocked(true);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = transform.right * h + transform.forward * v;
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                transform.Rotate(0f, mouseX, 0f, Space.Self);
                pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
                if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>Clear accumulated pitch and vertical velocity after a respawn.</summary>
        public void ResetView()
        {
            pitch = 0f;
            verticalVelocity = 0f;
            if (head != null) head.localRotation = Quaternion.identity;
        }

        public static void SetCursor(bool locked)
        {
            SetCursorLocked(locked);
        }
    }
}
