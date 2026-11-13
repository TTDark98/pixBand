using UnityEngine;

namespace PixBand.Core
{
    /// <summary>Keeps world-space labels facing the player (yaw only).</summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
