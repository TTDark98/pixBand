using System;
using UnityEngine;

namespace PixBand.Core
{
    /// <summary>
    /// Lightweight first-person limbs. Feet follow the player's horizontal
    /// motion and emit a landing event used by RecordedFootsteps; hand visuals
    /// follow the tracked controllers (or desktop stick holders).
    /// </summary>
    public class PlayerBodyVisuals : MonoBehaviour
    {
        [Header("Tracking")]
        public Transform movementOrigin;
        public Transform leftHandTarget;
        public Transform rightHandTarget;

        [Header("Hand Visuals")]
        public GameObject leftHandPrefab;
        public GameObject rightHandPrefab;
        public Material handMaterial;
        public float handScale = 1f;
        public Vector3 leftHandLocalPosition;
        public Vector3 leftHandLocalEuler;
        public Vector3 rightHandLocalPosition;
        public Vector3 rightHandLocalEuler;
        [Tooltip("Draw connected forearms for the desktop body rig. VR hands remain controller-tracked.")]
        public bool proceduralArms;

        [Header("Gait")]
        [Min(0.25f)] public float stepDistance = 0.72f;
        [Min(0f)] public float footLift = 0.085f;
        [Min(0f)] public float footSwing = 0.16f;
        [Min(0f)] public float legSwingDegrees = 17f;
        [Min(0f)] public float minimumSpeed = 0.28f;

        public event Action<Vector3> FootLanded;

        private Transform visualRoot;
        private Transform[] legs = new Transform[2];
        private Transform[] hands = new Transform[2];
        private Vector3[] handRestPositions = new Vector3[2];
        private Quaternion[] handRestRotations = new Quaternion[2];
        private bool[] handIsHeld = new bool[2];
        private float[] handHoldWeights = new float[2];
        private float[] handHoldVelocity = new float[2];
        private float[] handPickupPulses = new float[2];
        private Transform[] upperArms = new Transform[2];
        private Transform[] forearms = new Transform[2];
        private Vector3 lastPosition;
        private float distanceIntoStep;
        private int swingFoot;
        private bool walking;
        private float gaitProgress;
        private CharacterController characterController;
        private readonly RaycastHit[] groundCheckHits = new RaycastHit[12];

        private static Material pantsMaterial;
        private static Material bootMaterial;
        private static Material skinArmMaterial;
        private static Material shirtSleeveMaterial;

        private void Awake()
        {
            if (movementOrigin == null)
            {
                Camera camera = GetComponentInChildren<Camera>(true);
                if (camera != null) movementOrigin = camera.transform;
            }

            characterController = GetComponentInChildren<CharacterController>(true);
            visualRoot = new GameObject("FirstPerson_Limbs").transform;
            visualRoot.SetParent(transform, false);
            BuildLeg(0, -1f);
            BuildLeg(1, 1f);
            BuildHand(0, leftHandTarget);
            BuildHand(1, rightHandTarget);
            if (proceduralArms)
            {
                BuildArm(0, -1f);
                BuildArm(1, 1f);
            }
            lastPosition = movementOrigin != null ? movementOrigin.position : transform.position;
        }

        private void OnEnable()
        {
            if (movementOrigin != null) lastPosition = movementOrigin.position;
            distanceIntoStep = 0f;
            walking = false;
        }

        private void Update()
        {
            if (movementOrigin == null || visualRoot == null) return;

            // Keep the lower body under the tracked head in room-scale VR,
            // while remaining rooted to the locomotion rig in desktop mode.
            Vector3 localHead = transform.InverseTransformPoint(movementOrigin.position);
            visualRoot.localPosition = new Vector3(localHead.x, 0f, localHead.z);
            visualRoot.rotation = Quaternion.Euler(0f, movementOrigin.eulerAngles.y, 0f);

            Vector3 position = movementOrigin.position;
            Vector3 delta = position - lastPosition;
            lastPosition = position;
            delta.y = 0f;
            float distance = delta.magnitude;
            float speed = Time.deltaTime > 0.0001f ? distance / Time.deltaTime : 0f;

            // Tracking recenter/teleport must not create a burst of phantom steps.
            if (distance > 1.5f)
            {
                distanceIntoStep = 0f;
                walking = false;
                ResetLegPose();
                return;
            }

            bool grounded = IsGrounded();
            walking = grounded && speed >= minimumSpeed;
            if (!walking)
            {
                distanceIntoStep = 0f;
                gaitProgress = 0f;
                ResetLegPose();
                return;
            }

            distanceIntoStep += distance;
            gaitProgress = Mathf.Clamp01(distanceIntoStep / Mathf.Max(0.25f, stepDistance));
            AnimateLegs(gaitProgress);

            while (distanceIntoStep >= stepDistance)
            {
                distanceIntoStep -= stepDistance;
                int footLanded = swingFoot;
                Transform landed = legs[footLanded];
                if (landed != null)
                {
                    landed.localPosition = new Vector3((footLanded == 0 ? -1f : 1f) * 0.13f, 0.91f, 0.04f);
                    landed.localRotation = Quaternion.identity;
                    if (FootLanded != null) FootLanded(landed.position);
                }
                swingFoot = 1 - swingFoot;
                gaitProgress = Mathf.Clamp01(distanceIntoStep / Mathf.Max(0.25f, stepDistance));
                AnimateLegs(gaitProgress);
            }
        }

        /// <summary>Desktop stick holders are created after this component Awake.</summary>
        public void SetHandTargets(Transform left, Transform right)
        {
            leftHandTarget = left;
            rightHandTarget = right;
            BuildHand(0, leftHandTarget);
            BuildHand(1, rightHandTarget);
            if (proceduralArms)
            {
                if (upperArms[0] == null) BuildArm(0, -1f);
                if (upperArms[1] == null) BuildArm(1, 1f);
            }
        }

        /// <summary>Apply a rig-specific grip pose before the hand visuals are built.</summary>
        public void SetHandPose(
            Vector3 leftPosition, Vector3 leftEuler,
            Vector3 rightPosition, Vector3 rightEuler)
        {
            leftHandLocalPosition = leftPosition;
            leftHandLocalEuler = leftEuler;
            rightHandLocalPosition = rightPosition;
            rightHandLocalEuler = rightEuler;
        }

        /// <summary>Ease the wrist into a closed-stick grip when a stick is selected.</summary>
        public void SetHandHeld(int index, bool isHeld)
        {
            if (index < 0 || index >= hands.Length) return;
            handIsHeld[index] = isHeld;
            if (isHeld) handPickupPulses[index] = 1f;
        }

        /// <summary>Resolve an XR interactor back to the controller hand target.</summary>
        public void SetHandHeldFromInteractor(Transform interactor, bool isHeld)
        {
            if (interactor == null) return;
            if (MatchesHandTarget(interactor, leftHandTarget)) SetHandHeld(0, isHeld);
            else if (MatchesHandTarget(interactor, rightHandTarget)) SetHandHeld(1, isHeld);
        }

        private static bool MatchesHandTarget(Transform interactor, Transform target)
        {
            return target != null && (interactor == target || interactor.IsChildOf(target) || target.IsChildOf(interactor));
        }

        /// <summary>Quick wrist flick driven by a desktop or XR drum strike.</summary>
        public void PulseHand(int index)
        {
            if (index >= 0 && index < handPickupPulses.Length)
                handPickupPulses[index] = 1f;
        }

        public void PulseHandFromInteractor(Transform interactor)
        {
            if (MatchesHandTarget(interactor, leftHandTarget)) PulseHand(0);
            else if (MatchesHandTarget(interactor, rightHandTarget)) PulseHand(1);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i] == null) continue;
                float target = handIsHeld[i] ? 1f : 0f;
                float weight = Mathf.SmoothDamp(handHoldWeights[i], target,
                    ref handHoldVelocity[i], 0.12f);
                handHoldWeights[i] = weight;

                float pulse = handPickupPulses[i];
                handPickupPulses[i] = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 4.5f);
                float side = i == 0 ? -1f : 1f;
                Vector3 gripRotation = proceduralArms
                    ? new Vector3(-4f * weight, 0f, side * 4f * pulse)
                    : new Vector3(-12f * weight, side * 12f * weight, side * 14f * pulse);
                hands[i].localPosition = handRestPositions[i] +
                    new Vector3(0f, (proceduralArms ? -0.008f : -0.035f) * weight - 0.01f * pulse, 0f);
                hands[i].localRotation = handRestRotations[i] * Quaternion.Euler(gripRotation);
            }

            if (proceduralArms) UpdateArms();
        }

        private void BuildArm(int index, float side)
        {
            if (visualRoot == null || upperArms[index] != null) return;
            upperArms[index] = CreateLimbSegment(index == 0 ? "Left_UpperArm" : "Right_UpperArm",
                visualRoot, 0.105f, ShirtSleeve());
            forearms[index] = CreateLimbSegment(index == 0 ? "Left_Forearm" : "Right_Forearm",
                visualRoot, 0.082f, ArmSkin());
        }

        private Transform CreateLimbSegment(string name, Transform parent, float diameter, Material material)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segment.name = name;
            segment.transform.SetParent(parent, false);
            Collider collider = segment.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = segment.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            segment.transform.localScale = new Vector3(diameter, 0.25f, diameter);
            return segment.transform;
        }

        private void UpdateArms()
        {
            for (int i = 0; i < 2; i++)
            {
                if (hands[i] == null || upperArms[i] == null || forearms[i] == null) continue;

                float side = i == 0 ? -1f : 1f;
                Vector3 shoulder = visualRoot.TransformPoint(new Vector3(side * 0.22f, 1.38f, -0.10f));
                // Stop the forearm just inside the hand cuff; letting the
                // capsule run through the palm makes the supplied hand look
                // clipped off instead of joined to an arm.
                Vector3 wrist = Vector3.MoveTowards(hands[i].position, shoulder, 0.075f);
                Vector3 elbow = Vector3.Lerp(shoulder, wrist, 0.52f) + visualRoot.forward * 0.06f;
                SetSegment(upperArms[i], shoulder, elbow, 0.105f);
                SetSegment(forearms[i], elbow, wrist, 0.082f);
            }
        }

        private static void SetSegment(Transform segment, Vector3 start, Vector3 end, float diameter)
        {
            Vector3 delta = end - start;
            float length = Mathf.Max(0.08f, delta.magnitude);
            segment.position = (start + end) * 0.5f;
            segment.rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
            // Unity's built-in capsule is two units tall with a 0.5 unit radius.
            segment.localScale = new Vector3(diameter, length * 0.5f + diameter * 0.35f, diameter);
        }

        private bool IsGrounded()
        {
            if (characterController != null && characterController.enabled)
                return characterController.isGrounded;

            int count = Physics.RaycastNonAlloc(movementOrigin.position + Vector3.up * 0.1f,
                Vector3.down, groundCheckHits, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (groundCheckHits[i].collider == null) continue;
                Transform hit = groundCheckHits[i].transform;
                if (hit == transform || hit.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        private void AnimateLegs(float progress)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float side = i == 0 ? -1f : 1f;
                bool swing = i == swingFoot;
                if (!swing)
                {
                    legs[i].localPosition = new Vector3(side * 0.13f, 0.91f, 0.04f);
                    legs[i].localRotation = Quaternion.identity;
                    continue;
                }

                float arc = Mathf.Sin(progress * Mathf.PI);
                float z = Mathf.Lerp(-footSwing * 0.5f, footSwing * 0.5f, progress);
                legs[i].localPosition = new Vector3(side * 0.13f, 0.91f + arc * footLift, 0.04f + z);
                legs[i].localRotation = Quaternion.Euler(-arc * legSwingDegrees, 0f, 0f);
            }
        }

        private void ResetLegPose()
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                legs[i].localPosition = new Vector3(i == 0 ? -0.13f : 0.13f, 0.91f, 0.04f);
                legs[i].localRotation = Quaternion.identity;
            }
        }

        private void BuildLeg(int index, float side)
        {
            if (visualRoot == null) return;
            Transform root = new GameObject(index == 0 ? "Left_Leg" : "Right_Leg").transform;
            root.SetParent(visualRoot, false);
            root.localPosition = new Vector3(side * 0.13f, 0.91f, 0.04f);
            legs[index] = root;

            CreatePart(root, "Thigh", PrimitiveType.Capsule,
                new Vector3(0f, -0.19f, 0f), new Vector3(0.115f, 0.20f, 0.12f), Pants());
            CreatePart(root, "Shin", PrimitiveType.Capsule,
                new Vector3(0f, -0.59f, 0.015f), new Vector3(0.082f, 0.22f, 0.09f), Pants());
            CreatePart(root, "Boot", PrimitiveType.Cube,
                new Vector3(0f, -0.84f, 0.065f), new Vector3(0.15f, 0.10f, 0.23f), Boots());
            CreatePart(root, "Boot_Toe", PrimitiveType.Sphere,
                new Vector3(0f, -0.83f, 0.14f), new Vector3(0.145f, 0.085f, 0.12f), Boots());
        }

        private void BuildHand(int index, Transform target)
        {
            if (target == null || hands[index] != null) return;
            // The source left mesh is an open, flat palm while the right is a
            // natural relaxed grip. Mirror the grip geometry for the desktop
            // left hand so both hands can plausibly hold the drumsticks.
            GameObject prefab = proceduralArms ? rightHandPrefab : (index == 0 ? leftHandPrefab : rightHandPrefab);
            if (prefab == null) return;

            GameObject hand = Instantiate(prefab, target, false);
            hand.name = index == 0 ? "Left_Hand_Visual" : "Right_Hand_Visual";
            hand.transform.localPosition = index == 0 ? leftHandLocalPosition : rightHandLocalPosition;
            hand.transform.localRotation = Quaternion.Euler(index == 0 ? leftHandLocalEuler : rightHandLocalEuler);
            // The FBX carries a unit-conversion scale on its root; keep it and
            // apply the requested hand scale on top instead of replacing it.
            hand.transform.localScale = hand.transform.localScale * Mathf.Max(0.01f, handScale);
            hands[index] = hand.transform;
            if (proceduralArms && index == 0) MirrorHandGeometry(hand);
            handRestPositions[index] = hand.transform.localPosition;
            handRestRotations[index] = hand.transform.localRotation;
            Renderer[] renderers = hand.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (handMaterial != null) renderer.sharedMaterial = handMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void MirrorHandGeometry(GameObject hand)
        {
            MeshFilter[] filters = hand.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                Mesh source = filters[f].sharedMesh;
                if (source == null) continue;
                Mesh mirrored = Instantiate(source);
                mirrored.name = source.name + "_MirroredLeft";
                Vector3[] vertices = mirrored.vertices;
                Vector3[] normals = mirrored.normals;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].x = -vertices[i].x;
                    if (normals != null && normals.Length == vertices.Length)
                        normals[i].x = -normals[i].x;
                }
                mirrored.vertices = vertices;
                if (normals != null && normals.Length == vertices.Length) mirrored.normals = normals;
                for (int sub = 0; sub < mirrored.subMeshCount; sub++)
                {
                    int[] triangles = mirrored.GetTriangles(sub);
                    for (int i = 0; i + 2 < triangles.Length; i += 3)
                    {
                        int swap = triangles[i + 1];
                        triangles[i + 1] = triangles[i + 2];
                        triangles[i + 2] = swap;
                    }
                    mirrored.SetTriangles(triangles, sub, false);
                }
                mirrored.RecalculateBounds();
                filters[f].sharedMesh = mirrored;
            }
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType shape,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        private static Material MakeMaterial(ref Material material, string name, Color color)
        {
            if (material != null) return material;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            material = new Material(shader) { name = name, color = color, hideFlags = HideFlags.DontSave };
            return material;
        }

        private static Material Pants() { return MakeMaterial(ref pantsMaterial, "Player_Pants", new Color(0.045f, 0.052f, 0.065f)); }
        private static Material Boots() { return MakeMaterial(ref bootMaterial, "Player_Boots", new Color(0.025f, 0.022f, 0.021f)); }
        private static Material ArmSkin() { return MakeMaterial(ref skinArmMaterial, "Player_Arm_Skin", new Color(0.62f, 0.36f, 0.23f)); }
        private static Material ShirtSleeve() { return MakeMaterial(ref shirtSleeveMaterial, "Player_Shirt_Sleeve", new Color(0.055f, 0.065f, 0.08f)); }

        private void OnDestroy()
        {
            if (visualRoot != null) Destroy(visualRoot.gameObject);
        }
    }
}
