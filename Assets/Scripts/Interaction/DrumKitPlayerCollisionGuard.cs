using UnityEngine;

namespace PixBand.Interaction
{
    /// <summary>
    /// Player capsules must not be depenetrated by the held/resting stick grab
    /// volumes. The kit is approached closely, and CharacterController overlap
    /// resolution can otherwise produce a sudden shove on Quest.
    /// </summary>
    public class DrumKitPlayerCollisionGuard : MonoBehaviour
    {
        private Collider[] kitColliders;
        private CharacterController[] playerControllers;
        private float refreshTimer;

        private void Awake()
        {
            kitColliders = GetComponentsInChildren<Collider>(true);
            playerControllers = FindObjectsOfType<CharacterController>(true);
            IgnorePlayerKitCollisions();
        }

        private void LateUpdate()
        {
            // IgnoreCollision state is cleared when a rig GameObject is disabled.
            // Reapply infrequently so switching between desktop and XR stays safe.
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < 0.5f) return;
            refreshTimer = 0f;
            IgnorePlayerKitCollisions();
        }

        private void IgnorePlayerKitCollisions()
        {
            if (kitColliders == null || playerControllers == null) return;
            for (int i = 0; i < kitColliders.Length; i++)
            {
                Collider kitCollider = kitColliders[i];
                if (kitCollider == null || kitCollider.isTrigger) continue;

                for (int j = 0; j < playerControllers.Length; j++)
                {
                    CharacterController player = playerControllers[j];
                    if (player != null && player.gameObject.activeInHierarchy && player.enabled)
                        Physics.IgnoreCollision(player, kitCollider, true);
                }
            }
        }
    }
}
