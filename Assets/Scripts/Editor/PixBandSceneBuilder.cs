using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using PixBand.Audio;
using PixBand.Ambient;
using PixBand.Core;
using PixBand.Interaction;
using TMPro;

namespace PixBand.EditorTools
{
    /// <summary>
    /// Builds the single-scene demo: forest, mansion, drum room, menu and rigs.
    /// Menu: PixBand > Rebuild Demo Scene
    /// </summary>
    public static class PixBandSceneBuilder
    {
        private const string DemoScenePath = "Assets/Scenes/PixBand_Demo.unity";
        private const string KitSourceScene = "Assets/Scenes/Theatre_Drums.unity";
        private const string KitPrefabPath = "Assets/Prefabs/DrumKit.prefab";
        private const string XrSetupPath = "Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/Prefabs/XR Interaction Setup.prefab";
        private const string XrSimulatorPath = "Assets/Samples/XR Interaction Toolkit/2.5.4/XR Device Simulator/XR Device Simulator.prefab";
        private const string MaterialsDir = "Assets/Materials";

        private const string CatModelPath = "Assets/Models/Cat/Cat_Animated.fbx";
        private const string SkeletonModelPath = "Assets/Models/Skeletons/Skeleton_Male.fbx";
        private const string HandLeftModelPath = "Assets/Models/Player/HandsVR_Left.fbx";
        private const string HandRightModelPath = "Assets/Models/Player/HandsVR_Right.fbx";
        private const string HandSkinPath = "Assets/Models/Player/T_VRHands_Base_color.png";

        private const float ForestSize = 90f;
        private const int ForestResolution = 128;
        private const float ChunkSize = 16f;
        private const float PathHalfWidth = 3.2f;
        private const float MansionZ = 33f;

        private static float MansionX { get { return PathX(MansionZ - 4.5f); } }

        private class MenuBuild
        {
            public GameObject root;
            public GameObject panel;
            public List<MenuButton> buttons = new List<MenuButton>();
        }

        // ---------------------------------------------------------------- entry

        [MenuItem("PixBand/Rebuild Demo Scene")]
        public static void RebuildDemo()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(MaterialsDir);
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Prefabs");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject kitPrefab = EnsureKitPrefab();

            ConfigureAtmosphere();
            Material bark = MakeStandard("Bark_Dark", new Color(0.085f, 0.07f, 0.055f), 0.05f);
            Material leaf = MakeStandard("Foliage_Pine", new Color(0.05f, 0.12f, 0.075f), 0.02f);
            Material ground = MakeStandard("Ground_Moss", new Color(0.055f, 0.085f, 0.055f), 0.02f);
            Material dirt = MakeStandard("Path_Dirt", new Color(0.19f, 0.155f, 0.115f), 0.05f);
            Material stone = MakeStandard("Mansion_Stone", new Color(0.09f, 0.10f, 0.10f), 0.12f);
            Material roof = MakeStandard("Mansion_Roof", new Color(0.05f, 0.055f, 0.06f), 0.08f);
            Material wood = MakeStandard("Mansion_Wood", new Color(0.07f, 0.05f, 0.04f), 0.1f);
            Material plaster = MakeStandard("Mansion_Plaster", new Color(0.12f, 0.105f, 0.09f), 0.04f);
            Material bone = MakeStandard("Bone_Aged", new Color(0.82f, 0.74f, 0.56f), 0.16f);
            Material socket = MakeStandard("Skeleton_Socket_Shadow", new Color(0.025f, 0.012f, 0.01f), 0.08f);
            Material windowGlow = MakeEmissive("Mansion_Window", new Color(0.9f, 0.6f, 0.25f), new Color(1.6f, 0.9f, 0.35f));
            Material lanternGlow = MakeEmissive("Lantern_Glow", new Color(1f, 0.72f, 0.35f), new Color(2.2f, 1.2f, 0.45f));

            ApplySurfaceTexture(bark, "Forest_Bark", "bark", new Color(0.045f, 0.034f, 0.026f), new Color(0.22f, 0.15f, 0.09f), new Vector2(3f, 3f), 0.05f, 0.18f);
            ApplySurfaceTexture(leaf, "Forest_Needles", "mottle", new Color(0.018f, 0.055f, 0.035f), new Color(0.11f, 0.22f, 0.12f), new Vector2(2f, 2f), 0f, 0.08f);
            ApplySurfaceTexture(ground, "Forest_Grass_Detail", "grass", new Color(0.025f, 0.055f, 0.025f), new Color(0.26f, 0.32f, 0.13f), new Vector2(32f, 32f), 0f, 0.01f);
            ApplySurfaceTexture(dirt, "Forest_Dirt", "dirt", new Color(0.10f, 0.062f, 0.038f), new Color(0.34f, 0.24f, 0.13f), new Vector2(5f, 7f), 0f, 0.04f);
            ApplySurfaceTexture(stone, "Mansion_Rough_Stone", "stone", new Color(0.035f, 0.045f, 0.045f), new Color(0.22f, 0.24f, 0.22f), new Vector2(2f, 2f), 0.08f, 0.22f);
            ApplySurfaceTexture(roof, "Mansion_Slate", "stone", new Color(0.018f, 0.025f, 0.033f), new Color(0.12f, 0.15f, 0.17f), new Vector2(4f, 3f), 0.22f, 0.2f);
            ApplySurfaceTexture(wood, "Mansion_Aged_Wood", "wood", new Color(0.04f, 0.022f, 0.014f), new Color(0.19f, 0.09f, 0.035f), new Vector2(2f, 2f), 0f, 0.18f);
            ApplySurfaceTexture(plaster, "Mansion_Plaster_Texture", "stone", new Color(0.055f, 0.045f, 0.038f), new Color(0.23f, 0.19f, 0.15f), new Vector2(3f, 2f), 0f, 0.05f);
            ApplySurfaceTexture(bone, "Aged_Bone_Texture", "bone", new Color(0.30f, 0.24f, 0.15f), new Color(0.91f, 0.82f, 0.63f), new Vector2(2f, 2f), 0.02f, 0.2f);

            BuildTerrain(ground);
            BuildPath(dirt);
            BuildForest(bark, leaf);
            Vector3 kitPosition = BuildMansion(stone, roof, wood, plaster, bone, socket, windowGlow, lanternGlow);
            BuildLanterns();
            BuildFireflies();
            BuildForestMist();
            BuildNightSky();
            BuildAmbience();

            if (kitPrefab != null)
            {
                GameObject kit = (GameObject)PrefabUtility.InstantiatePrefab(kitPrefab);
                kit.name = "DrumKit_Instrument";
                kit.transform.position = kitPosition;
                kit.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                AlignKitToRiser(kit);
                ApplyDrumKitMaterials(kit);
                WireKit(kit);
                RemoveDecorativeKitColliders(kit);
                kit.AddComponent<DrumKitPlayerCollisionGuard>();
            }
            else
            {
                Debug.LogError("[PixBand] Drum kit prefab could not be created.");
            }

            Vector3 spawn = new Vector3(0f, Height(0f, 0f) + 0.06f, 0f);
            GameObject spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.position = spawn;
            spawnGo.transform.rotation = Quaternion.identity;

            MenuBuild menu = BuildMenu(spawn);
            BuildRigs(spawn, menu);

            GameObject flow = new GameObject("SceneFlow");
            SceneFlowManager flowManager = flow.AddComponent<SceneFlowManager>();
            flowManager.spawnPoint = spawnGo.transform;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DemoScenePath);
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PixBand] Demo scene rebuilt: " + DemoScenePath);
        }

        [MenuItem("PixBand/Rebuild Everything")]
        public static void RebuildEverything()
        {
            RebuildDemo();
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(DemoScenePath, true)
            };
        }

        // ------------------------------------------------------------- prefab

        private static GameObject EnsureKitPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(KitSourceScene) == null)
            {
                Debug.LogError("[PixBand] Missing kit source scene: " + KitSourceScene);
                return null;
            }

            Scene source = EditorSceneManager.OpenScene(KitSourceScene, OpenSceneMode.Additive);
            GameObject kit = FindDeep(source, "DrumKit_Instrument");

            if (kit == null)
            {
                Debug.LogError("[PixBand] DrumKit_Instrument not found in " + KitSourceScene);
                EditorSceneManager.CloseScene(source, true);
                return null;
            }

            InstrumentInteractable legacy = kit.GetComponent<InstrumentInteractable>();
            if (legacy != null) Object.DestroyImmediate(legacy);

            AudioSource sourceAudio = kit.GetComponent<AudioSource>();
            if (sourceAudio == null) sourceAudio = kit.AddComponent<AudioSource>();
            sourceAudio.playOnAwake = false;
            sourceAudio.loop = false;
            sourceAudio.spatialBlend = 1f;
            sourceAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            sourceAudio.minDistance = 1.5f;
            sourceAudio.maxDistance = 22f;
            sourceAudio.dopplerLevel = 0f;
            sourceAudio.spread = 20f;

            ParticleSystemRenderer dustRenderer = null;
            ParticleSystemRenderer[] particleRenderers = kit.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < particleRenderers.Length; i++)
            {
                if (particleRenderers[i].gameObject.name == "DrumDust")
                {
                    dustRenderer = particleRenderers[i];
                    break;
                }
            }
            if (dustRenderer != null)
                dustRenderer.sharedMaterial = MakeParticleMaterial("DrumDust", "Legacy Shaders/Particles/Alpha Blended");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(kit, KitPrefabPath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(source, true);

            Debug.Log("[PixBand] Drum kit prefab saved: " + KitPrefabPath);
            return prefab;
        }

        private static GameObject FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindDeep(roots[i].transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void WireKit(GameObject kit)
        {
            AudioSource source = kit.GetComponent<AudioSource>();
            if (source == null) source = kit.AddComponent<AudioSource>();

            ParticleSystem dust = null;
            ParticleSystem[] particles = kit.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].gameObject.name == "DrumDust")
                {
                    dust = particles[i];
                    break;
                }
            }
            if (dust == null) dust = BuildDust(kit.transform);

            DrumStick[] sticks = kit.GetComponentsInChildren<DrumStick>(true);
            if (sticks.Length == 0) sticks = BuildSticks(kit.transform, source, dust);
            for (int i = 0; i < sticks.Length; i++)
            {
                DrumStick stick = sticks[i];
                if (stick == null) continue;

                Transform socket = stick.restSocket;
                if (socket != null)
                {
                    float side = i == 0 ? -1f : 1f;
                    socket.localPosition = new Vector3(side * 0.75f, 0.03f, 0.82f);
                    socket.localRotation = Quaternion.Euler(-12f, 0f, side * 10f);
                    stick.transform.SetPositionAndRotation(socket.position, socket.rotation);
                }

                XRGrabInteractable grab = stick.GetComponent<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectMode = InteractableSelectMode.Multiple;
                    Transform grip = stick.transform.Find("Grip");
                    if (grip != null) grab.attachTransform = grip;
                }
            }

            DrumPad[] pads = kit.GetComponentsInChildren<DrumPad>(true);
            Material ringMaterial = MakeRingMaterial();
            if (pads.Length == 0) pads = BuildPads(kit.transform, source, dust, ringMaterial);
            WirePadVisuals(pads, ringMaterial);

            DrumPerformance performance = kit.GetComponent<DrumPerformance>();
            if (performance == null) performance = kit.AddComponent<DrumPerformance>();

            DrumProximityZone zone = kit.GetComponent<DrumProximityZone>();
            if (zone == null) zone = kit.AddComponent<DrumProximityZone>();
            zone.radius = 3.4f;
            zone.sticks = sticks;
            zone.player = null;

            GameObject prompt = CreateLabel(
                "DrumPrompt",
                "GRAB THE STICKS",
                kit.transform.position + new Vector3(0f, 1.5f, -2.1f),
                0.5f,
                new Color(0.8f, 1f, 0.9f));
            zone.promptRoot = prompt;
            prompt.SetActive(false);

            Debug.Log("[PixBand] Kit wired: pads=" + pads.Length + " sticks=" + sticks.Length);
        }

        private static void AlignKitToRiser(GameObject kit)
        {
            GameObject mansion = GameObject.Find("Mansion");
            Transform riser = mansion == null ? null : mansion.transform.Find("Riser");
            Renderer surface = riser == null ? null : riser.GetComponent<Renderer>();
            if (surface == null) return;

            MeshRenderer[] renderers = kit.GetComponentsInChildren<MeshRenderer>(true);
            bool found = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == "DrumDust") continue;
                if (!found)
                {
                    bounds = renderers[i].bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!found) return;

            float targetY = surface.bounds.max.y + 0.012f;
            float offset = targetY - bounds.min.y;
            kit.transform.position += Vector3.up * offset;
            Debug.Log("[PixBand] Drum kit aligned to riser; vertical correction=" + offset.ToString("F2") + " m");
        }

        private static void ApplyDrumKitMaterials(GameObject kit)
        {
            Material shell = MakeStandard("Kit_Lacquered_Teal", Color.white, 0.52f);
            ApplySurfaceTexture(shell, "Kit_Shell_Lacquer", "wood", new Color(0.025f, 0.085f, 0.11f), new Color(0.16f, 0.34f, 0.39f), new Vector2(2f, 2f), 0.28f, 0.55f);

            Material metal = MakeStandard("Kit_Brushed_Steel", Color.white, 0.58f);
            ApplySurfaceTexture(metal, "Kit_Brushed_Steel_Texture", "metal", new Color(0.16f, 0.21f, 0.22f), new Color(0.62f, 0.70f, 0.69f), new Vector2(3f, 3f), 0.78f, 0.62f);

            Material bronze = MakeStandard("Kit_Aged_Bronze", Color.white, 0.42f);
            ApplySurfaceTexture(bronze, "Kit_Cymbal_Bronze_Texture", "cymbal", new Color(0.15f, 0.075f, 0.018f), new Color(0.82f, 0.53f, 0.16f), new Vector2(2f, 2f), 0.82f, 0.46f);

            Material rubber = MakeStandard("Kit_Matte_Rubber", new Color(0.16f, 0.17f, 0.17f), 0.18f);
            ApplySurfaceTexture(rubber, "Kit_Rubber_Texture", "mottle", new Color(0.025f, 0.03f, 0.03f), new Color(0.16f, 0.18f, 0.18f), new Vector2(2f, 2f), 0f, 0.18f);

            Material rug = MakeStandard("Kit_Woven_Rug", Color.white, 0.04f);
            ApplySurfaceTexture(rug, "Kit_Woven_Rug_Texture", "fabric", new Color(0.035f, 0.008f, 0.012f), new Color(0.22f, 0.045f, 0.035f), new Vector2(3f, 2f), 0f, 0.04f);

            Renderer[] renderers = kit.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] source = renderers[i].sharedMaterials;
                Material[] styled = new Material[source.Length];
                for (int j = 0; j < source.Length; j++)
                {
                    string name = source[j] == null ? string.Empty : source[j].name;
                    if (name == "Main Color") styled[j] = shell;
                    else if (name == "Metal") styled[j] = metal;
                    else if (name == "Cymbal_Brass_Mat") styled[j] = bronze;
                    else if (name == "Black" || name == "Black Glossy" || name == "Black Glossy.001" || name == "Black transp") styled[j] = rubber;
                    else if (name == "Drum_Rug_Mat") styled[j] = rug;
                    else styled[j] = source[j];
                }
                renderers[i].sharedMaterials = styled;
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private static void RemoveDecorativeKitColliders(GameObject kit)
        {
            Collider[] colliders = kit.GetComponentsInChildren<Collider>(true);
            int removed = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;

                // Keep only the intentional stick grab volumes and drum-pad hit
                // triggers. Imported visual meshes often contain oversized
                // non-trigger capsules that can punt a CharacterController.
                if (collider.GetComponentInParent<DrumStick>() != null ||
                    collider.GetComponentInParent<DrumPad>() != null)
                    continue;

                Object.DestroyImmediate(collider);
                removed++;
            }

            Debug.Log("[PixBand] Removed " + removed + " decorative drum-kit colliders");
        }

        // ------------------------------------------------------------- terrain

        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.075f, 0.105f, 0.12f);
            // Keep close ground detail crisp while the far tree line fades into
            // a soft cool night haze rather than disappearing into black.
            RenderSettings.fogDensity = 0.017f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.145f, 0.115f);
            RenderSettings.skybox = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.sun = null;

            GameObject moon = new GameObject("Moonlight");
            Light light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.48f, 0.63f, 0.78f);
            light.intensity = 0.48f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            moon.transform.rotation = Quaternion.Euler(28f, 155f, 0f);
        }

        private static float RawHeight(float x, float z)
        {
            float a = Mathf.PerlinNoise((x + 100f) * 0.035f, (z + 100f) * 0.035f);
            float b = Mathf.PerlinNoise((x + 40f) * 0.13f, (z + 40f) * 0.13f);
            return (a - 0.5f) * 1.7f + (b - 0.5f) * 0.22f;
        }

        private static float Height(float x, float z)
        {
            float h = RawHeight(x, z);

            // Flatten a pad under the mansion so the floor never clips the terrain.
            float mx = MansionX;
            float dx = x - mx;
            float dz = z - MansionZ;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            float blend = Mathf.Clamp01((distance - 6.5f) / 4.5f);
            if (blend < 1f)
            {
                float flat = RawHeight(mx, MansionZ);
                h = Mathf.Lerp(flat, h, blend);
            }

            return h;
        }

        private static float PathX(float z)
        {
            return Mathf.Sin(z * 0.10f) * 2.9f + Mathf.Sin(z * 0.037f + 1.1f) * 1.5f;
        }

        private static float PathDistance(float x, float z)
        {
            if (z < -8f || z > 47f) return 999f;
            return Mathf.Abs(x - PathX(z));
        }

        private static void BuildTerrain(Material material)
        {
            int res = ForestResolution;
            int verts = (res + 1) * (res + 1);
            Vector3[] positions = new Vector3[verts];
            Color[] colors = new Color[verts];
            Vector2[] uvs = new Vector2[verts];
            int[] tris = new int[res * res * 6];

            float half = ForestSize * 0.5f;
            float step = ForestSize / res;

            for (int z = 0; z <= res; z++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int i = z * (res + 1) + x;
                    float wx = -half + x * step;
                    float wz = -half + z * step;
                    positions[i] = new Vector3(wx, Height(wx, wz), wz);
                    uvs[i] = new Vector2((float)x / res, (float)z / res);

                    float patch = Mathf.PerlinNoise(wx * 0.2f, wz * 0.2f);
                    colors[i] = Color.Lerp(new Color(0.05f, 0.075f, 0.05f), new Color(0.08f, 0.10f, 0.07f), patch);
                }
            }

            int t = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    tris[t++] = i;
                    tris[t++] = i + res + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + res + 1;
                    tris[t++] = i + res + 2;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "ForestTerrain";
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = positions;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            SaveMeshAsset(mesh, "Assets/Art/ForestTerrain.asset");

            GameObject go = new GameObject("Forest_Terrain");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private static void BuildPath(Material material)
        {
            const int samples = 70;
            Vector3[] verts = new Vector3[samples * 2];
            Vector2[] uvs = new Vector2[samples * 2];
            int[] tris = new int[(samples - 1) * 6];

            float zStart = -4f;
            float zEnd = 29f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                float z = Mathf.Lerp(zStart, zEnd, t);
                float x = PathX(z);
                float dx = PathX(z + 0.1f) - x;
                Vector3 tangent = new Vector3(dx, 0f, 0.1f).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;

                Vector3 left = new Vector3(x, 0f, z) + side * 1.3f;
                Vector3 right = new Vector3(x, 0f, z) - side * 1.3f;
                left.y = Height(left.x, left.z) + 0.035f;
                right.y = Height(right.x, right.z) + 0.035f;

                verts[i * 2] = left;
                verts[i * 2 + 1] = right;
                uvs[i * 2] = new Vector2(0f, t * 12f);
                uvs[i * 2 + 1] = new Vector2(1f, t * 12f);
            }

            for (int i = 0; i < samples - 1; i++)
            {
                int b = i * 2;
                tris[i * 6] = b;
                tris[i * 6 + 1] = b + 2;
                tris[i * 6 + 2] = b + 1;
                tris[i * 6 + 3] = b + 1;
                tris[i * 6 + 4] = b + 2;
                tris[i * 6 + 5] = b + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = "ForestPath";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            GameObject go = new GameObject("Path");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        // -------------------------------------------------------------- forest

        private static void BuildForest(Material bark, Material leaf)
        {
            Mesh[] trunkVariants = new Mesh[3];
            Mesh[] leafVariants = new Mesh[3];
            Vector3[] scales = new Vector3[3];

            for (int i = 0; i < trunkVariants.Length; i++)
            {
                Mesh trunk;
                Mesh foliage;
                BuildTreeMeshes(1701 + i * 37, 0.85f + i * 0.12f, out trunk, out foliage);
                trunk.name = "PineTrunk_" + i;
                foliage.name = "PineFoliage_" + i;
                trunkVariants[i] = trunk;
                leafVariants[i] = foliage;
                scales[i] = new Vector3(0.9f + i * 0.08f, 0.85f + i * 0.15f, 0.9f + i * 0.08f);
            }

            Dictionary<Vector2Int, List<CombineInstance>> trunkChunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            Dictionary<Vector2Int, List<CombineInstance>> leafChunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            System.Random random = new System.Random(424242);
            float half = ForestSize * 0.5f;

            for (float z = -half + 2f; z < half - 2f; z += 2.0f)
            {
                for (float x = -half + 2f; x < half - 2f; x += 2.0f)
                {
                    float jx = x + (float)(random.NextDouble() - 0.5) * 2.0f;
                    float jz = z + (float)(random.NextDouble() - 0.5) * 2.0f;

                    if (PathDistance(jx, jz) < PathHalfWidth) continue;
                    if (jz > 26f && Mathf.Abs(jx) < 12f) continue;
                    if (Vector3.Distance(new Vector3(jx, 0f, jz), new Vector3(0f, 0f, 3f)) < 5.5f) continue;
                    if (random.NextDouble() < 0.10) continue;

                    int variant = random.Next(trunkVariants.Length);
                    float y = Height(jx, jz);
                    Quaternion rot = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    float scale = 0.8f + (float)random.NextDouble() * 0.55f;
                    Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(jx, y, jz), rot, scales[variant] * scale);

                    Vector2Int key = new Vector2Int(
                        Mathf.FloorToInt(jx / ChunkSize),
                        Mathf.FloorToInt(jz / ChunkSize));

                    if (!trunkChunks.ContainsKey(key)) trunkChunks[key] = new List<CombineInstance>();
                    if (!leafChunks.ContainsKey(key)) leafChunks[key] = new List<CombineInstance>();

                    CombineInstance trunkInstance = new CombineInstance();
                    trunkInstance.mesh = trunkVariants[variant];
                    trunkInstance.transform = matrix;
                    trunkChunks[key].Add(trunkInstance);

                    CombineInstance leafInstance = new CombineInstance();
                    leafInstance.mesh = leafVariants[variant];
                    leafInstance.transform = matrix;
                    leafChunks[key].Add(leafInstance);
                }
            }

            GameObject root = new GameObject("Forest_Trees");
            int index = 0;
            foreach (KeyValuePair<Vector2Int, List<CombineInstance>> pair in trunkChunks)
            {
                GameObject chunk = new GameObject("Chunk_" + pair.Key.x + "_" + pair.Key.y);
                chunk.transform.SetParent(root.transform, false);
                chunk.isStatic = true;

                Mesh trunkMesh = new Mesh();
                trunkMesh.name = "ChunkTrunks_" + index;
                trunkMesh.indexFormat = IndexFormat.UInt32;
                trunkMesh.CombineMeshes(pair.Value.ToArray(), true, true);
                GameObject trunkGo = new GameObject("Trunks");
                trunkGo.transform.SetParent(chunk.transform, false);
                trunkGo.AddComponent<MeshFilter>().sharedMesh = trunkMesh;
                MeshRenderer trunkRenderer = trunkGo.AddComponent<MeshRenderer>();
                trunkRenderer.sharedMaterial = bark;
                trunkRenderer.shadowCastingMode = ShadowCastingMode.Off;
                trunkRenderer.receiveShadows = false;

                Mesh leafMesh = new Mesh();
                leafMesh.name = "ChunkFoliage_" + index;
                leafMesh.indexFormat = IndexFormat.UInt32;
                leafMesh.CombineMeshes(leafChunks[pair.Key].ToArray(), true, true);
                GameObject leafGo = new GameObject("Foliage");
                leafGo.transform.SetParent(chunk.transform, false);
                leafGo.AddComponent<MeshFilter>().sharedMesh = leafMesh;
                MeshRenderer leafRenderer = leafGo.AddComponent<MeshRenderer>();
                leafRenderer.sharedMaterial = leaf;
                leafRenderer.shadowCastingMode = ShadowCastingMode.Off;
                leafRenderer.receiveShadows = false;

                index++;
            }

            Debug.Log("[PixBand] Forest chunks=" + index + " (trunk + foliage meshes per chunk)");

            CreateInvisibleWall(new Vector3(0f, 6f, half), new Vector3(ForestSize * 2f, 12f, 1f));
            CreateInvisibleWall(new Vector3(0f, 6f, -half), new Vector3(ForestSize * 2f, 12f, 1f));
            CreateInvisibleWall(new Vector3(half, 6f, 0f), new Vector3(1f, 12f, ForestSize * 2f));
            CreateInvisibleWall(new Vector3(-half, 6f, 0f), new Vector3(1f, 12f, ForestSize * 2f));
        }

        private static void BuildTreeMeshes(int seed, float width, out Mesh trunkMesh, out Mesh foliageMesh)
        {
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trunkTris = new List<int>();

            System.Random random = new System.Random(seed);

            int sides = 6;
            float trunkRadius = 0.13f * width;
            float trunkHeight = 2.3f;
            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring * trunkHeight;
                for (int s = 0; s < sides; s++)
                {
                    float a = (float)s / sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * trunkRadius, y, Mathf.Sin(a) * trunkRadius));
                    normals.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                    uvs.Add(new Vector2((float)s / sides, ring));
                }
            }

            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;
                int a = s;
                int b = next;
                int c = sides + s;
                int d = sides + next;
                trunkTris.Add(a); trunkTris.Add(c); trunkTris.Add(b);
                trunkTris.Add(b); trunkTris.Add(c); trunkTris.Add(d);
            }

            trunkMesh = new Mesh();
            trunkMesh.name = "PineTrunk";
            trunkMesh.SetVertices(verts);
            trunkMesh.SetNormals(normals);
            trunkMesh.SetUVs(0, uvs);
            trunkMesh.SetTriangles(trunkTris, 0);
            trunkMesh.RecalculateBounds();

            verts.Clear();
            normals.Clear();
            uvs.Clear();
            List<int> leafTris = new List<int>();

            // Dense, overlapping branch skirts create a more natural evergreen
            // silhouette than four isolated cone primitives. The final crown is
            // narrower and taller to break up the repeated triangular outline.
            float[] baseY = { 1.35f, 2.15f, 3.0f, 3.85f, 4.65f };
            float[] radius = { 1.3f, 1.12f, 0.91f, 0.70f, 0.48f };
            float[] tierHeight = { 1.85f, 1.8f, 1.7f, 1.55f, 1.45f };

            for (int tier = 0; tier < baseY.Length; tier++)
            {
                float r = radius[tier] * width * (0.92f + (float)random.NextDouble() * 0.16f);
                float y0 = baseY[tier];
                int ringSides = 9;
                int start = verts.Count;

                // Each bough skirt flares out low, then narrows in two steps.
                // Per-vertex variation avoids perfectly machined silhouettes.
                float[] profileY = { 0f, 0.18f, 0.58f, 1f };
                float[] profileR = { 0.46f, 1f, 0.78f, 0.08f };
                for (int ring = 0; ring < profileY.Length; ring++)
                {
                    for (int s = 0; s < ringSides; s++)
                    {
                        float a = (float)s / ringSides * Mathf.PI * 2f + tier * 0.37f;
                        float irregular = 0.9f + (float)random.NextDouble() * 0.2f;
                        float ringRadius = r * profileR[ring] * irregular;
                        float y = y0 + tierHeight[tier] * profileY[ring];
                        verts.Add(new Vector3(Mathf.Cos(a) * ringRadius, y, Mathf.Sin(a) * ringRadius));
                        Vector3 normal = new Vector3(Mathf.Cos(a) * 0.72f, 0.46f, Mathf.Sin(a) * 0.72f).normalized;
                        normals.Add(normal);
                        uvs.Add(new Vector2((float)s / ringSides, profileY[ring]));
                    }
                }

                for (int ring = 0; ring < profileY.Length - 1; ring++)
                {
                    int lower = start + ring * ringSides;
                    int upper = lower + ringSides;
                    for (int s = 0; s < ringSides; s++)
                    {
                        int next = (s + 1) % ringSides;
                        leafTris.Add(lower + s);
                        leafTris.Add(upper + s);
                        leafTris.Add(lower + next);
                        leafTris.Add(lower + next);
                        leafTris.Add(upper + s);
                        leafTris.Add(upper + next);
                    }
                }
            }

            foliageMesh = new Mesh();
            foliageMesh.name = "PineFoliage";
            foliageMesh.SetVertices(verts);
            foliageMesh.SetNormals(normals);
            foliageMesh.SetUVs(0, uvs);
            foliageMesh.SetTriangles(leafTris, 0);
            foliageMesh.RecalculateBounds();
        }

        private static void CreateInvisibleWall(Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject("Boundary");
            go.transform.position = position;
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        // ------------------------------------------------------------- mansion

        private static Vector3 BuildMansion(
            Material stone,
            Material roofMaterial,
            Material wood,
            Material plaster,
            Material bone,
            Material socket,
            Material windowGlow,
            Material lanternGlow)
        {
            float groundY = Height(MansionX, MansionZ);
            GameObject root = new GameObject("Mansion");
            root.transform.position = new Vector3(MansionX, groundY, MansionZ);
            root.AddComponent<MansionInteriorFog>();

            const float floorTop = 0.25f;
            const float wallHeight = 5.6f;
            const float wallCenterY = floorTop + wallHeight * 0.5f;
            const float doorWidth = 3.0f;
            const float doorHeight = 3.4f;

            // Solid floor (also the collider the player walks on).
            CreateBox(root.transform, "Floor", new Vector3(9.6f, 0.5f, 8.6f), new Vector3(0f, 0f, 0f), wood);

            CreateBox(root.transform, "Wall_Back", new Vector3(10f, wallHeight, 0.5f), new Vector3(0f, wallCenterY, 4.25f), plaster);
            CreateBox(root.transform, "Wall_Left", new Vector3(0.5f, wallHeight, 9f), new Vector3(-4.75f, wallCenterY, 0f), plaster);
            CreateBox(root.transform, "Wall_Right", new Vector3(0.5f, wallHeight, 9f), new Vector3(4.75f, wallCenterY, 0f), plaster);

            float sideWidth = (10f - doorWidth) * 0.5f;
            CreateBox(root.transform, "Wall_Front_L", new Vector3(sideWidth, wallHeight, 0.5f), new Vector3(-(doorWidth * 0.5f + sideWidth * 0.5f), wallCenterY, -4.25f), plaster);
            CreateBox(root.transform, "Wall_Front_R", new Vector3(sideWidth, wallHeight, 0.5f), new Vector3(doorWidth * 0.5f + sideWidth * 0.5f, wallCenterY, -4.25f), plaster);
            CreateBox(root.transform, "Wall_Front_Header", new Vector3(doorWidth, wallHeight - doorHeight, 0.5f), new Vector3(0f, floorTop + doorHeight + (wallHeight - doorHeight) * 0.5f, -4.25f), plaster);

            // Stone skin on the outside of the timber shell, so it reads as a mansion.
            CreateBox(root.transform, "Stone_Back", new Vector3(10.4f, wallHeight, 0.2f), new Vector3(0f, wallCenterY, 4.62f), stone);
            CreateBox(root.transform, "Stone_Left", new Vector3(0.2f, wallHeight, 9.4f), new Vector3(-5.12f, wallCenterY, 0f), stone);
            CreateBox(root.transform, "Stone_Right", new Vector3(0.2f, wallHeight, 9.4f), new Vector3(5.12f, wallCenterY, 0f), stone);
            CreateBox(root.transform, "Stone_Front_L", new Vector3(sideWidth + 0.2f, wallHeight, 0.2f), new Vector3(-(doorWidth * 0.5f + sideWidth * 0.5f), wallCenterY, -4.62f), stone);
            CreateBox(root.transform, "Stone_Front_R", new Vector3(sideWidth + 0.2f, wallHeight, 0.2f), new Vector3(doorWidth * 0.5f + sideWidth * 0.5f, wallCenterY, -4.62f), stone);

            // Interior ceiling and a closed, thick gable roof above it.
            CreateBox(root.transform, "Ceiling", new Vector3(9.4f, 0.3f, 8.4f), new Vector3(0f, wallHeight - 0.35f, 0f), wood);

            Mesh roof = BuildRoofMesh(11.6f, 3.4f, 10.6f);
            SaveMeshAsset(roof, "Assets/Art/MansionRoof.asset");
            GameObject roofGo = new GameObject("Roof");
            roofGo.transform.SetParent(root.transform, false);
            roofGo.transform.localPosition = new Vector3(0f, wallHeight + 0.02f, 0f);
            roofGo.AddComponent<MeshFilter>().sharedMesh = roof;
            MeshRenderer roofRenderer = roofGo.AddComponent<MeshRenderer>();
            roofRenderer.sharedMaterial = roofMaterial;
            roofRenderer.shadowCastingMode = ShadowCastingMode.Off;
            roofRenderer.receiveShadows = false;

            // Door frame and an open door leaf.
            CreateBox(root.transform, "DoorJamb_L", new Vector3(0.18f, doorHeight, 0.6f), new Vector3(-1.59f, floorTop + doorHeight * 0.5f, -4.24f), wood);
            CreateBox(root.transform, "DoorJamb_R", new Vector3(0.18f, doorHeight, 0.6f), new Vector3(1.59f, floorTop + doorHeight * 0.5f, -4.24f), wood);
            CreateBox(root.transform, "DoorLintel", new Vector3(doorWidth + 0.4f, 0.22f, 0.6f), new Vector3(0f, floorTop + doorHeight + 0.05f, -4.24f), wood);

            GameObject pivot = new GameObject("DoorPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(-1.5f, floorTop, -4.22f);
            pivot.transform.localRotation = Quaternion.Euler(0f, -105f, 0f);
            CreateBox(pivot.transform, "DoorLeaf", new Vector3(1.48f, 3.32f, 0.1f), new Vector3(0.74f, 1.66f, 0f), wood);

            // Windows (thin emissive panes just proud of the wall).
            CreateBox(root.transform, "Window_Front_L", new Vector3(1.2f, 1.8f, 0.04f), new Vector3(-3.3f, 3.5f, -4.74f), windowGlow);
            CreateBox(root.transform, "Window_Front_R", new Vector3(1.2f, 1.8f, 0.04f), new Vector3(3.3f, 3.5f, -4.74f), windowGlow);
            CreateBox(root.transform, "Window_Side_L", new Vector3(0.04f, 1.8f, 1.2f), new Vector3(-5.24f, 3.5f, 0f), windowGlow);
            CreateBox(root.transform, "Window_Side_R", new Vector3(0.04f, 1.8f, 1.2f), new Vector3(5.24f, 3.5f, 0f), windowGlow);
            BuildGhostAudience(root.transform, wood);

            // Porch.
            CreateBox(root.transform, "Step", new Vector3(4.6f, 0.25f, 1.4f), new Vector3(0f, 0.12f, -5.2f), stone);
            CreateBox(root.transform, "Column_L", new Vector3(0.45f, 4.6f, 0.45f), new Vector3(-2.4f, 2.3f, -5.1f), stone);
            CreateBox(root.transform, "Column_R", new Vector3(0.45f, 4.6f, 0.45f), new Vector3(2.4f, 2.3f, -5.1f), stone);
            CreateBox(root.transform, "PorchRoof", new Vector3(5.8f, 0.3f, 1.9f), new Vector3(0f, 4.65f, -5.0f), roofMaterial);

            GameObject porchLight = new GameObject("Porch_Light");
            porchLight.transform.SetParent(root.transform, false);
            porchLight.transform.localPosition = new Vector3(0f, 3.9f, -5.0f);
            Light porch = porchLight.AddComponent<Light>();
            porch.type = LightType.Point;
            porch.range = 11f;
            porch.intensity = 1.5f;
            porch.color = new Color(1f, 0.62f, 0.28f);
            porch.shadows = LightShadows.None;

            BuildEntranceCat(root.transform);

            // Interior: riser, rug, two flickering lanterns.
            CreateBox(root.transform, "Riser", new Vector3(3.6f, 0.2f, 3.0f), new Vector3(0f, floorTop + 0.1f, 0.6f), wood);
            Material rug = MakeStandard("Rug_Red", new Color(0.16f, 0.035f, 0.035f), 0.02f);
            ApplySurfaceTexture(rug, "Mansion_Rug_Woven", "fabric", new Color(0.055f, 0.012f, 0.018f), new Color(0.28f, 0.055f, 0.045f), new Vector2(3f, 2f), 0f, 0.04f);
            CreateBox(root.transform, "Rug", new Vector3(4.2f, 0.02f, 2.2f), new Vector3(0f, floorTop + 0.01f, -2.3f), rug);

            BuildInteriorLantern(root.transform, new Vector3(-3.4f, 3.35f, -0.4f), lanternGlow, wood);
            BuildInteriorLantern(root.transform, new Vector3(3.4f, 3.35f, 0.4f), lanternGlow, wood);
            BuildInteriorLights(root.transform);
            // Both supplied dancers are the anatomical skeleton, driven by
            // SkeletonModelDancer through its imported bone names.
            float skeletonBase = floorTop;
            BuildSkeletonModel(root.transform, "Skeleton_Left", new Vector3(-3.68f, skeletonBase, 1.0f), 180f, 0f);
            BuildSkeletonModel(root.transform, "Skeleton_Right", new Vector3(3.68f, skeletonBase, 1.0f), 180f, 0.62f);

            return root.transform.position + new Vector3(0f, floorTop + 1.95f, 0.6f);
        }

        private static void BuildInteriorLights(Transform parent)
        {
            GameObject keyObject = new GameObject("Kit_Key_Light");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.localPosition = new Vector3(0f, 5.15f, 1.2f);
            keyObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Spot;
            key.range = 8f;
            key.spotAngle = 72f;
            key.intensity = 2.8f;
            key.color = new Color(1f, 0.68f, 0.38f);
            key.shadows = LightShadows.None;
            key.renderMode = LightRenderMode.ForcePixel;

            GameObject fillObject = new GameObject("Door_Moon_Fill");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.localPosition = new Vector3(0f, 2.4f, -3.8f);
            fillObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Spot;
            fill.range = 9f;
            fill.spotAngle = 80f;
            fill.intensity = 1.0f;
            fill.color = new Color(0.36f, 0.56f, 0.68f);
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForcePixel;
        }

        private static SkeletonDancer BuildSkeleton(
            Transform parent, string name, Vector3 localPosition, bool slumped, Material bone, Material socket)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(0f, slumped ? 165f : 195f, slumped ? -8f : 6f);

            // Joint pivots instead of a flat pile of capsules: the dancer rotates
            // these, and each pivot owns one combined mesh so the whole skeleton
            // stays at roughly a dozen draw calls.
            GameObject hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform, false);
            hips.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            CreateSkeletonSphere(hips.transform, "Pelvis", Vector3.zero, new Vector3(0.32f, 0.2f, 0.2f), bone);

            GameObject spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform, false);
            spine.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            spine.transform.localRotation = Quaternion.Euler(slumped ? 9f : 1.5f, 0f, 0f);

            CreateSkeletonBone(spine.transform, "Spine_Lower", Vector3.zero, new Vector3(0f, 0.26f, 0f), 0.055f, bone);
            CreateSkeletonBone(spine.transform, "Spine_Upper", new Vector3(0f, 0.25f, 0f), new Vector3(0f, 0.49f, 0f), 0.05f, bone);

            for (int rib = 0; rib < 3; rib++)
            {
                float y = 0.16f + rib * 0.11f;
                float width = 0.25f - rib * 0.025f;
                CreateSkeletonBone(spine.transform, "Rib_L_" + rib, new Vector3(-0.025f, y, 0f), new Vector3(-width, y - 0.04f, 0.015f), 0.028f, bone);
                CreateSkeletonBone(spine.transform, "Rib_R_" + rib, new Vector3(0.025f, y, 0f), new Vector3(width, y - 0.04f, 0.015f), 0.028f, bone);
            }
            CreateSkeletonBone(spine.transform, "Clavicle_L", new Vector3(0f, 0.47f, 0f), new Vector3(-0.25f, 0.44f, 0f), 0.035f, bone);
            CreateSkeletonBone(spine.transform, "Clavicle_R", new Vector3(0f, 0.47f, 0f), new Vector3(0.25f, 0.44f, 0f), 0.035f, bone);

            GameObject neck = new GameObject("Neck");
            neck.transform.SetParent(spine.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            neck.transform.localRotation = Quaternion.Euler(slumped ? 14f : 2f, 0f, 0f);

            CreateSkeletonBone(neck.transform, "Neck_Bone", Vector3.zero, new Vector3(0f, 0.18f, 0f), 0.045f, bone);
            CreateSkeletonSphere(neck.transform, "Skull", new Vector3(0f, 0.33f, 0.015f), new Vector3(0.23f, 0.26f, 0.2f), bone);
            CreateSkeletonBone(neck.transform, "Jaw", new Vector3(-0.11f, 0.24f, 0.11f), new Vector3(0.11f, 0.24f, 0.11f), 0.035f, bone);
            CreateSkeletonSphere(neck.transform, "EyeSocket_L", new Vector3(-0.075f, 0.35f, 0.184f), new Vector3(0.052f, 0.063f, 0.025f), socket);
            CreateSkeletonSphere(neck.transform, "EyeSocket_R", new Vector3(0.075f, 0.35f, 0.184f), new Vector3(0.052f, 0.063f, 0.025f), socket);
            CreateSkeletonSphere(neck.transform, "Nose_Cavity", new Vector3(0f, 0.27f, 0.187f), new Vector3(0.035f, 0.055f, 0.02f), socket);

            GameObject shoulderLeft = BuildArm(spine.transform, "Shoulder_L", new Vector3(-0.25f, 0.42f, 0f), -1f, slumped, bone);
            GameObject shoulderRight = BuildArm(spine.transform, "Shoulder_R", new Vector3(0.25f, 0.42f, 0f), 1f, slumped, bone);
            Transform elbowLeft = shoulderLeft.transform.Find("Elbow_L");
            Transform elbowRight = shoulderRight.transform.Find("Elbow_R");

            GameObject hipLeft = BuildLeg(hips.transform, "Hip_L", new Vector3(-0.11f, -0.03f, 0f), -1f, bone);
            GameObject hipRight = BuildLeg(hips.transform, "Hip_R", new Vector3(0.11f, -0.03f, 0f), 1f, bone);
            Transform kneeLeft = hipLeft.transform.Find("Knee_L");
            Transform kneeRight = hipRight.transform.Find("Knee_R");

            // One merged mesh per pivot, so animation does not multiply draw calls.
            CombineJointGeometry(hips.transform, name + "_Hips");
            CombineJointGeometry(spine.transform, name + "_Spine");
            CombineJointGeometry(neck.transform, name + "_Skull");
            CombineJointGeometry(shoulderLeft.transform, name + "_ArmL");
            CombineJointGeometry(shoulderRight.transform, name + "_ArmR");
            CombineJointGeometry(elbowLeft, name + "_ForeL");
            CombineJointGeometry(elbowRight, name + "_ForeR");
            CombineJointGeometry(hipLeft.transform, name + "_LegL");
            CombineJointGeometry(hipRight.transform, name + "_LegR");
            CombineJointGeometry(kneeLeft, name + "_ShinL");
            CombineJointGeometry(kneeRight, name + "_ShinR");

            SkeletonDancer dancer = root.AddComponent<SkeletonDancer>();
            dancer.hips = hips.transform;
            dancer.spine = spine.transform;
            dancer.neck = neck.transform;
            dancer.shoulderLeft = shoulderLeft.transform;
            dancer.shoulderRight = shoulderRight.transform;
            dancer.elbowLeft = elbowLeft;
            dancer.elbowRight = elbowRight;
            dancer.hipLeft = hipLeft.transform;
            dancer.hipRight = hipRight.transform;
            dancer.kneeLeft = kneeLeft;
            dancer.kneeRight = kneeRight;
            dancer.phaseOffset = slumped ? 0.7f : 0f;
            return dancer;
        }

        /// <summary>
        /// Instantiate the supplied anatomical skeleton, normalise it to human
        /// height with its feet on the floor, and hand its imported bones to
        /// SkeletonModelDancer. Falls back to the procedural rig if the model
        /// asset is missing.
        /// </summary>
        private static void BuildSkeletonModel(
            Transform parent, string name, Vector3 localPosition, float yaw, float phaseOffset)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonModelPath);
            if (prefab == null)
            {
                Debug.LogWarning("[PixBand] Skeleton model missing at " + SkeletonModelPath + "; using procedural fallback.");
                Material fallbackBone = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Bone_Aged.mat");
                Material fallbackSocket = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Skeleton_Socket_Shadow.mat");
                BuildSkeleton(parent, name, localPosition, yaw > 90f, fallbackBone, fallbackSocket);
                return;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            model.name = name + "_Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // The raw asset is about 18 m tall (Poser units); scale relative to
            // the current hierarchy so any baked root scale is preserved.
            Bounds bounds = CalculateBounds(model);
            float ratio = 1.78f / Mathf.Max(0.01f, bounds.size.y);
            model.transform.localScale = model.transform.localScale * ratio;

            bounds = CalculateBounds(model);
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }

            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            SkeletonModelDancer dancer = root.AddComponent<SkeletonModelDancer>();
            dancer.phaseOffset = phaseOffset;
            dancer.Bind(model.transform);
        }

        /// <summary>
        /// The animated entrance cat. It sits on the porch step, loops its
        /// grooming clip, and can be petted for a purr and a meow. Petting is
        /// cosmetic and never gates the skeleton dance.
        /// </summary>
        private static void BuildEntranceCat(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatModelPath);
            if (prefab == null)
            {
                Debug.LogWarning("[PixBand] Cat model missing at " + CatModelPath);
                return;
            }

            GameObject root = new GameObject("Entrance_Cat");
            root.transform.SetParent(parent, false);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            model.name = "Cat_Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Bounds bounds = CalculateBoneBounds(model);
            float ratio = 0.52f / Mathf.Max(0.01f, bounds.size.y);
            model.transform.localScale = model.transform.localScale * ratio;

            bounds = CalculateBoneBounds(model);
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = CalculateBoneBounds(model);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = new Vector3(bounds.size.x * 1.12f, bounds.size.y * 1.04f, bounds.size.z * 1.12f);

            root.transform.localPosition = new Vector3(1.35f, 0.245f, -5.05f);
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            AudioSource source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.55f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.6f;
            source.maxDistance = 12f;

            root.AddComponent<XRSimpleInteractable>();

            PetableCat cat = root.AddComponent<PetableCat>();
            cat.purrClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cat/Purr.wav");
            cat.meows = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cat/CatMeow_Cute.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cat/CatMeow_Soft.mp3")
            };

            Animation animation = model.GetComponent<Animation>();
            if (animation == null) animation = model.AddComponent<Animation>();
            AnimationClip clip = animation.clip;
            if (clip == null) clip = FindAnimationClip(prefab);
            if (clip != null)
            {
                if (animation.GetClip(clip.name) == null) animation.AddClip(clip, clip.name);
                animation.clip = clip;
                animation.wrapMode = WrapMode.Loop;
            }
            cat.idleAnimation = animation;
            cat.idleClip = clip;
        }

        private static AnimationClip FindAnimationClip(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                // Legacy import also exposes a "__preview__" clip for the
                // inspector; the real take is the one without the prefix.
                if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
            }
            return null;
        }

        private static GameObject BuildArm(
            Transform parent, string name, Vector3 localPosition, float side, bool slumped, Material bone)
        {
            GameObject shoulder = new GameObject(name);
            shoulder.transform.SetParent(parent, false);
            shoulder.transform.localPosition = localPosition;
            shoulder.transform.localRotation = Quaternion.Euler(0f, 0f, side * (slumped ? -16f : -4f));

            float handX = side * 0.12f;
            CreateSkeletonBone(shoulder.transform, "Arm_Upper",
                Vector3.zero, new Vector3(handX, -0.28f, 0f), 0.045f, bone);

            GameObject elbow = new GameObject("Elbow_" + name.Substring(name.Length - 1));
            elbow.transform.SetParent(shoulder.transform, false);
            elbow.transform.localPosition = new Vector3(handX, -0.28f, 0f);
            elbow.transform.localRotation = Quaternion.Euler(slumped ? -22f : -8f, 0f, 0f);

            Vector3 hand = new Vector3(side * 0.11f, -0.30f, 0.08f);
            CreateSkeletonBone(elbow.transform, "Arm_Fore", Vector3.zero, hand, 0.035f, bone);
            CreateSkeletonSphere(elbow.transform, "Hand", hand, new Vector3(0.09f, 0.07f, 0.06f), bone);
            return shoulder;
        }

        private static GameObject BuildLeg(
            Transform parent, string name, Vector3 localPosition, float side, Material bone)
        {
            GameObject hip = new GameObject(name);
            hip.transform.SetParent(parent, false);
            hip.transform.localPosition = localPosition;

            Vector3 kneePosition = new Vector3(side * 0.07f, -0.32f, 0f);
            CreateSkeletonBone(hip.transform, "Leg_Upper", Vector3.zero, kneePosition, 0.06f, bone);

            GameObject knee = new GameObject("Knee_" + name.Substring(name.Length - 1));
            knee.transform.SetParent(hip.transform, false);
            knee.transform.localPosition = kneePosition;

            Vector3 ankle = new Vector3(-side * 0.02f, -0.29f, 0.04f);
            CreateSkeletonBone(knee.transform, "Leg_Lower", Vector3.zero, ankle, 0.042f, bone);
            CreateSkeletonBone(knee.transform, "Foot", ankle, ankle + new Vector3(-side * 0.15f, -0.03f, 0.18f), 0.04f, bone);
            return hip;
        }

        /// <summary>Merge every mesh directly under one pivot into a single renderer.</summary>
        private static void CombineJointGeometry(Transform joint, string name)
        {
            if (joint == null) return;

            Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
            List<Transform> parts = new List<Transform>();

            for (int i = 0; i < joint.childCount; i++)
            {
                Transform child = joint.GetChild(i);
                MeshFilter filter = child.GetComponent<MeshFilter>();
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                    continue;

                Material material = renderer.sharedMaterial;
                if (!groups.ContainsKey(material)) groups[material] = new List<CombineInstance>();
                CombineInstance instance = new CombineInstance();
                instance.mesh = filter.sharedMesh;
                instance.transform = joint.worldToLocalMatrix * child.localToWorldMatrix;
                groups[material].Add(instance);
                parts.Add(child);
            }

            int index = 0;
            foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
            {
                Mesh mesh = new Mesh();
                mesh.name = name + "_" + group.Key.name;
                mesh.CombineMeshes(group.Value.ToArray(), true, true);

                GameObject combined = new GameObject(name + "_Mesh_" + index++);
                combined.transform.SetParent(joint, false);
                combined.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = combined.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = group.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null) Object.DestroyImmediate(parts[i].gameObject);
        }

        private static GameObject CreateSkeletonSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        private static GameObject CreateSkeletonBone(Transform parent, string name, Vector3 start, Vector3 end, float radius, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            part.name = name;
            part.transform.SetParent(parent, false);
            Vector3 delta = end - start;
            part.transform.localPosition = (start + end) * 0.5f;
            part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            part.transform.localScale = new Vector3(radius, delta.magnitude * 0.5f, radius);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        /// <summary>
        /// Spectral audience in the drum-room windows. The panes sit on the
        /// inside face of the walls, because the player plays from inside the
        /// mansion: ghosts painted on the exterior windows would never be seen.
        /// </summary>
        private static void BuildGhostAudience(Transform parent, Material frameMaterial)
        {
            Material ghostMaterial = MakeGhostMaterial();
            Material paneMaterial = MakeEmissive("Ghost_Window_Glow",
                new Color(0.045f, 0.075f, 0.09f), new Color(0.11f, 0.25f, 0.31f));

            GameObject group = new GameObject("Ghost_Audience");
            group.transform.SetParent(parent, false);

            List<Renderer> renderers = new List<Renderer>();

            // Behind the kit: the two panes the drummer looks straight at.
            CreateBox(group.transform, "Ghost_Window_Back_L", new Vector3(1.3f, 1.9f, 0.04f),
                new Vector3(-2.4f, 3.1f, 3.98f), paneMaterial);
            CreateBox(group.transform, "Ghost_Window_Back_R", new Vector3(1.3f, 1.9f, 0.04f),
                new Vector3(2.4f, 3.1f, 3.98f), paneMaterial);
            AddWindowFrame(group.transform, "Ghost_Frame_Back_L", new Vector3(-2.4f, 3.1f, 3.89f), false, frameMaterial);
            AddWindowFrame(group.transform, "Ghost_Frame_Back_R", new Vector3(2.4f, 3.1f, 3.89f), false, frameMaterial);
            AddGhostQuad(group.transform, "Ghost_Back_L", new Vector3(-2.4f, 3.02f, 3.93f), Quaternion.identity, ghostMaterial, renderers);
            AddGhostQuad(group.transform, "Ghost_Back_R", new Vector3(2.4f, 3.02f, 3.93f), Quaternion.identity, ghostMaterial, renderers);

            // Side walls, lined up with the real windows outside.
            CreateBox(group.transform, "Ghost_Window_Side_L", new Vector3(0.04f, 1.9f, 1.3f),
                new Vector3(-4.48f, 3.1f, 0f), paneMaterial);
            CreateBox(group.transform, "Ghost_Window_Side_R", new Vector3(0.04f, 1.9f, 1.3f),
                new Vector3(4.48f, 3.1f, 0f), paneMaterial);
            AddWindowFrame(group.transform, "Ghost_Frame_Side_L", new Vector3(-4.40f, 3.1f, 0f), true, frameMaterial);
            AddWindowFrame(group.transform, "Ghost_Frame_Side_R", new Vector3(4.40f, 3.1f, 0f), true, frameMaterial);
            AddGhostQuad(group.transform, "Ghost_Side_L", new Vector3(-4.44f, 3.02f, 0f), Quaternion.Euler(0f, -90f, 0f), ghostMaterial, renderers);
            AddGhostQuad(group.transform, "Ghost_Side_R", new Vector3(4.44f, 3.02f, 0f), Quaternion.Euler(0f, 90f, 0f), ghostMaterial, renderers);

            GhostAudience audience = group.AddComponent<GhostAudience>();
            audience.silhouettes = renderers.ToArray();
            audience.comboThreshold = 6;
            audience.maxOpacity = 0.72f;
        }

        /// <summary>Window casing and mullions so the ghost panes read as windows.</summary>
        private static void AddWindowFrame(
            Transform parent, string name, Vector3 center, bool alongZ, Material frame)
        {
            if (alongZ)
            {
                CreateBox(parent, name + "_V0", new Vector3(0.06f, 2.0f, 0.10f), center + new Vector3(0f, 0f, -0.65f), frame);
                CreateBox(parent, name + "_V1", new Vector3(0.06f, 2.0f, 0.10f), center + new Vector3(0f, 0f, 0.65f), frame);
                CreateBox(parent, name + "_H0", new Vector3(0.06f, 0.10f, 1.3f), center + new Vector3(0f, -0.95f, 0f), frame);
                CreateBox(parent, name + "_H1", new Vector3(0.06f, 0.10f, 1.3f), center + new Vector3(0f, 0.95f, 0f), frame);
                CreateBox(parent, name + "_M0", new Vector3(0.05f, 1.9f, 0.05f), center, frame);
                CreateBox(parent, name + "_M1", new Vector3(0.06f, 0.05f, 1.3f), center, frame);
            }
            else
            {
                CreateBox(parent, name + "_V0", new Vector3(0.10f, 2.0f, 0.06f), center + new Vector3(-0.65f, 0f, 0f), frame);
                CreateBox(parent, name + "_V1", new Vector3(0.10f, 2.0f, 0.06f), center + new Vector3(0.65f, 0f, 0f), frame);
                CreateBox(parent, name + "_H0", new Vector3(1.3f, 0.10f, 0.06f), center + new Vector3(0f, -0.95f, 0f), frame);
                CreateBox(parent, name + "_H1", new Vector3(1.3f, 0.10f, 0.06f), center + new Vector3(0f, 0.95f, 0f), frame);
                CreateBox(parent, name + "_M0", new Vector3(0.05f, 1.9f, 0.05f), center, frame);
                CreateBox(parent, name + "_M1", new Vector3(1.3f, 0.05f, 0.06f), center, frame);
            }
        }

        private static void AddGhostQuad(
            Transform parent, string name, Vector3 localPosition, Quaternion localRotation,
            Material material, List<Renderer> renderers)
        {
            GameObject quad = new GameObject(name);
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = localRotation;
            quad.transform.localScale = new Vector3(1.05f, 1.6f, 1f);

            quad.AddComponent<MeshFilter>().sharedMesh = GhostQuadMesh();

            MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderers.Add(renderer);
        }

        private static Mesh ghostQuadMesh;

        /// <summary>
        /// Unit quad with explicit white vertex colours, because the sprite
        /// shader multiplies by vertex colour and a mesh without a colour
        /// channel can read as black on some Quest drivers.
        /// </summary>
        private static Mesh GhostQuadMesh()
        {
            const string path = "Assets/Art/GhostQuad.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                ghostQuadMesh = existing;
                return existing;
            }

            Mesh mesh = new Mesh();
            mesh.name = "GhostQuad";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.hideFlags = HideFlags.None;

            EnsureFolder("Assets/Art");
            AssetDatabase.CreateAsset(mesh, path);
            ghostQuadMesh = mesh;
            return mesh;
        }

        private static void WirePadVisuals(DrumPad[] pads, Material ringMaterial)
        {
            for (int i = 0; i < pads.Length; i++)
            {
                DrumPad pad = pads[i];
                if (pad == null) continue;

                // Prefab pads were serialized with head: {fileID: 0}; recover the
                // visible mesh from the trigger's parent chain.
                if (pad.head == null)
                {
                    Transform candidate = pad.transform.parent;
                    while (candidate != null && candidate.GetComponent<Renderer>() == null)
                        candidate = candidate.parent;
                    pad.head = candidate;
                }

                bool cymbal = pad.gameObject.name.IndexOf("Circle", System.StringComparison.OrdinalIgnoreCase) >= 0;
                pad.cymbal = cymbal;
                pad.recoilDepth = 0.010f;
                pad.ringMaterial = ringMaterial;
                pad.impactTint = cymbal
                    ? new Color(1f, 0.86f, 0.55f)
                    : new Color(1f, 0.62f, 0.34f);

                // Field edits on prefab-instance components are not part of the
                // scene diff unless they are recorded explicitly.
                if (PrefabUtility.IsPartOfPrefabInstance(pad))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pad);
                EditorUtility.SetDirty(pad);
            }
        }

        private static Material MakeRingMaterial()
        {
            Material material = MakeTintedMaterial("Drum_Impact_Ring", "Sprites/Default");
            material.mainTexture = MakeImpactRingTexture();
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material MakeGhostMaterial()
        {
            Material material = MakeTintedMaterial("Ghost_Silhouette", "Sprites/Default");
            material.mainTexture = MakeGhostTexture();
            material.color = new Color(0.03f, 0.045f, 0.07f, 0.9f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Alpha-blended unlit material that exposes _Color, so renderers can
        /// fade it per-object through a MaterialPropertyBlock. Reuses the asset
        /// instead of deleting it: replacing the shader is enough.
        /// </summary>
        private static Material MakeTintedMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            string path = MaterialsDir + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static void BuildInteriorLantern(Transform parent, Vector3 localPosition, Material glow, Material chainMaterial)
        {
            GameObject lantern = new GameObject("Interior_Lantern");
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = localPosition;

            // A visible rod tucks into the ceiling so the lanterns read as
            // suspended fixtures instead of floating emissive cubes.
            CreateBox(lantern.transform, "Hanging_Rod", new Vector3(0.045f, 1.75f, 0.045f),
                new Vector3(0f, 0.98f, 0f), chainMaterial);

            // The light itself: a small warm core inside an iron cage. A bare
            // emissive cube reads as a flat yellow panel from across the room.
            CreateBox(lantern.transform, "Glow", new Vector3(0.17f, 0.25f, 0.17f), Vector3.zero, glow);
            CreateBox(lantern.transform, "Cage_Top", new Vector3(0.27f, 0.035f, 0.27f), new Vector3(0f, 0.15f, 0f), chainMaterial);
            CreateBox(lantern.transform, "Cage_Bottom", new Vector3(0.27f, 0.035f, 0.27f), new Vector3(0f, -0.15f, 0f), chainMaterial);
            CreateBox(lantern.transform, "Cage_Finial", new Vector3(0.09f, 0.06f, 0.09f), new Vector3(0f, 0.19f, 0f), chainMaterial);
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = (corner == 0 || corner == 3) ? -0.115f : 0.115f;
                float cz = (corner < 2) ? -0.115f : 0.115f;
                CreateBox(lantern.transform, "Cage_Post_" + corner, new Vector3(0.032f, 0.30f, 0.032f),
                    new Vector3(cx, 0f, cz), chainMaterial);
            }

            // Soft halo so the cage does not look like a lit box.
            Material halo = MakeParticleMaterial("Lantern_Halo", "Legacy Shaders/Particles/Additive");
            halo.mainTexture = MakeSoftDotTexture("Lantern_Halo_Dot");
            halo.SetColor("_TintColor", new Color(0.55f, 0.34f, 0.14f, 0.55f));
            EditorUtility.SetDirty(halo);
            GameObject haloGo = new GameObject("Halo");
            haloGo.transform.SetParent(lantern.transform, false);
            haloGo.transform.localScale = Vector3.one * 1.5f;
            MeshFilter haloFilter = haloGo.AddComponent<MeshFilter>();
            haloFilter.sharedMesh = GhostQuadMesh();
            MeshRenderer haloRenderer = haloGo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = halo;
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;

            Light light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6.5f;
            light.intensity = 1.05f;
            light.color = new Color(1f, 0.68f, 0.34f);
            light.shadows = LightShadows.None;

            LanternFlicker flicker = lantern.AddComponent<LanternFlicker>();
            flicker.baseIntensity = 1.05f;
        }

        private static Mesh BuildRoofMesh(float width, float height, float depth)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            Vector3[] v =
            {
                new Vector3(-hw, 0f, -hd), new Vector3(hw, 0f, -hd), new Vector3(hw, 0f, hd), new Vector3(-hw, 0f, hd),
                new Vector3(0f, height, -hd), new Vector3(0f, height, hd)
            };
            int[] tris =
            {
                0, 1, 2, 0, 2, 3,
                0, 4, 1,
                5, 3, 2,
                0, 5, 4, 0, 3, 5,
                1, 5, 2, 1, 4, 5
            };
            Mesh mesh = new Mesh();
            mesh.name = "MansionRoof";
            mesh.vertices = v;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void BuildLanterns()
        {
            Material wood = MakeStandard("Lantern_Wood", new Color(0.06f, 0.045f, 0.035f), 0.08f);
            Material glow = MakeEmissive("Lantern_Glow_Path", new Color(1f, 0.72f, 0.35f), new Color(2.2f, 1.2f, 0.45f));

            for (int i = 0; i < 4; i++)
            {
                float z = 7f + i * 6.8f;
                float side = (i % 2 == 0) ? 1f : -1f;
                float x = PathX(z) + side * 2.15f;
                float y = Height(x, z);

                GameObject root = new GameObject("Lantern_" + i);
                root.transform.position = new Vector3(x, y, z);

                CreateBox(root.transform, "Post", new Vector3(0.09f, 1.7f, 0.09f), new Vector3(0f, 0.85f, 0f), wood);
                CreateBox(root.transform, "Lamp", new Vector3(0.24f, 0.32f, 0.24f), new Vector3(0f, 1.78f, 0f), glow);

                GameObject lightGo = new GameObject("Light");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 7.5f;
                light.intensity = 1.3f;
                light.color = new Color(1f, 0.66f, 0.32f);
                light.shadows = LightShadows.None;

                LanternFlicker flicker = lightGo.AddComponent<LanternFlicker>();
                flicker.baseIntensity = 1.3f;
            }
        }

        private static void BuildFireflies()
        {
            GameObject go = new GameObject("Fireflies");
            go.transform.position = new Vector3(0f, 4f, 18f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 1f, 0.55f, 0.9f));
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(55f, 5f, 70f);

            Texture2D dot = MakeSoftDotTexture("Firefly_Dot");
            Material mat = MakeParticleMaterial("Firefly_Glow", "Legacy Shaders/Particles/Additive");
            mat.mainTexture = dot;
            mat.SetColor("_TintColor", new Color(0.7f, 1f, 0.6f, 1f));
            EditorUtility.SetDirty(mat);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildForestMist()
        {
            Texture2D smokeA = LoadSmokeSprite("Assets/Art/Textures/Kenney/whitePuff00.png");
            Texture2D smokeB = LoadSmokeSprite("Assets/Art/Textures/Kenney/whitePuff07.png");
            Texture2D smokeC = LoadSmokeSprite("Assets/Art/Textures/Kenney/whitePuff14.png");

            Material mistA = MakeParticleMaterial("Forest_Mist_Particles", "Legacy Shaders/Particles/Alpha Blended");
            mistA.mainTexture = smokeA;
            mistA.SetColor("_TintColor", new Color(0.62f, 0.72f, 0.78f, 0.55f));
            EditorUtility.SetDirty(mistA);

            Material mistB = MakeParticleMaterial("Forest_Mist_Particles_B", "Legacy Shaders/Particles/Alpha Blended");
            mistB.mainTexture = smokeB;
            mistB.SetColor("_TintColor", new Color(0.55f, 0.66f, 0.72f, 0.5f));
            EditorUtility.SetDirty(mistB);

            Material haze = MakeParticleMaterial("Forest_Haze_Particles", "Legacy Shaders/Particles/Alpha Blended");
            haze.mainTexture = smokeC;
            haze.SetColor("_TintColor", new Color(0.5f, 0.6f, 0.7f, 0.42f));
            EditorUtility.SetDirty(haze);

            // Low, slow banks that hug the forest floor and drift across the path.
            CreateAtmosphereParticles("Low_Forest_Mist_A", new Vector3(0f, 2.4f, 18f),
                new Vector3(64f, 2.8f, 62f), 14, 26f, 46f,
                7f, 13f, new Color(0.70f, 0.79f, 0.84f, 0.20f), mistA, 0.05f, 60f);
            CreateAtmosphereParticles("Low_Forest_Mist_B", new Vector3(0f, 2.0f, 22f),
                new Vector3(58f, 2.2f, 56f), 10, 22f, 40f,
                5f, 10f, new Color(0.62f, 0.72f, 0.78f, 0.16f), mistB, 0.07f, 60f);

            // A thin band above the trees so the far edge of the forest melts away.
            CreateAtmosphereParticles("High_Forest_Haze", new Vector3(0f, 8.5f, 26f),
                new Vector3(72f, 4.5f, 72f), 8, 34f, 56f,
                14f, 24f, new Color(0.52f, 0.63f, 0.72f, 0.10f), haze, 0.03f, 80f);
        }

        private static Texture2D LoadSmokeSprite(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning("[PixBand] Missing smoke sprite: " + path);
                return MakeSoftDotTexture("Mist_Fallback_Dot");
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null &&
                (importer.wrapMode != TextureWrapMode.Clamp || !importer.alphaIsTransparency))
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return texture;
        }

        private static void BuildNightSky()
        {
            Material moonMaterial = MakeEmissive("Moon_Ivory_Glow",
                new Color(0.72f, 0.81f, 0.91f), new Color(0.34f, 0.43f, 0.58f));
            GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            moon.transform.position = new Vector3(-21f, 24f, 49f);
            moon.transform.localScale = Vector3.one * 3.6f;
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.GetComponent<Renderer>().sharedMaterial = moonMaterial;
            moon.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            moon.GetComponent<Renderer>().receiveShadows = false;

            // A soft halo keeps the moon from reading as a hard white ball.
            Material halo = MakeParticleMaterial("Moon_Halo", "Legacy Shaders/Particles/Additive");
            halo.mainTexture = MakeSoftDotTexture("Star_Dot");
            halo.SetColor("_TintColor", new Color(0.42f, 0.52f, 0.68f, 0.5f));
            EditorUtility.SetDirty(halo);
            CreateAtmosphereParticles("Moon_Halo", new Vector3(-21f, 24f, 49.4f),
                new Vector3(0.1f, 0.1f, 0.1f), 1, 600f, 600f,
                9f, 9f, new Color(0.55f, 0.68f, 0.85f, 0.30f), halo, 0f, 20f);

            Material starMaterial = MakeParticleMaterial("Night_Sky_Stars", "Legacy Shaders/Particles/Additive");
            starMaterial.mainTexture = MakeSoftDotTexture("Star_Dot");
            starMaterial.SetColor("_TintColor", new Color(0.68f, 0.8f, 1f, 0.82f));
            EditorUtility.SetDirty(starMaterial);
            CreateAtmosphereParticles("Stars", new Vector3(0f, 26f, 16f),
                new Vector3(88f, 14f, 96f), 220, 600f, 600f,
                0.05f, 0.12f, new Color(0.78f, 0.86f, 1f, 0.75f), starMaterial, 0f, 2f);

            Material cloudMaterial = MakeParticleMaterial("Night_Sky_Clouds", "Legacy Shaders/Particles/Alpha Blended");
            cloudMaterial.mainTexture = LoadSmokeSprite("Assets/Art/Textures/Kenney/whitePuff14.png");
            cloudMaterial.SetColor("_TintColor", new Color(0.40f, 0.48f, 0.60f, 0.30f));
            EditorUtility.SetDirty(cloudMaterial);
            CreateAtmosphereParticles("Thin_Night_Clouds", new Vector3(0f, 17f, 20f),
                new Vector3(80f, 6f, 84f), 6, 60f, 90f,
                16f, 27f, new Color(0.44f, 0.53f, 0.66f, 0.07f), cloudMaterial, 0.012f, 80f);
        }

        private static void CreateAtmosphereParticles(
            string name, Vector3 position, Vector3 bounds, int count,
            float minimumLifetime, float maximumLifetime,
            float minimumSize, float maximumSize, Color color,
            Material material, float driftSpeed, float maxParticleSize)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 100f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minimumLifetime, maximumLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(driftSpeed * 0.65f, driftSpeed * 1.35f);
            main.startSize3D = false;
            main.startSize = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = count / Mathf.Max(1f, (minimumLifetime + maximumLifetime) * 0.5f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = bounds;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // The old build clamped every billboard to 0.35 m, which squashed
            // the wide mist cards into the rectangles that read as fog boxes.
            renderer.maxParticleSize = maxParticleSize;
            renderer.minParticleSize = 0f;
            renderer.sortingFudge = 0f;
        }

        private static void BuildAmbience()
        {
            GameObject go = new GameObject("Wind_Ambience");
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            WindAmbience wind = go.AddComponent<WindAmbience>();
            wind.ambienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Horror/Ambience/Forest_Wind_Loop.mp3");
            wind.howlClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Horror/Ambience/Howling_Wind_CC0.mp3");
            wind.volume = 0.40f;
            wind.howlVolume = 0.12f;
            wind.firstHowlMinimumDelay = 24f;
            wind.firstHowlMaximumDelay = 38f;
            wind.minimumHowlInterval = 48f;
            wind.maximumHowlInterval = 78f;
            go.transform.position = new Vector3(0f, 3f, 15f);

            GameObject horror = new GameObject("Haunted_Ambience");
            horror.transform.position = new Vector3(MansionX, Height(MansionX, MansionZ) + 1.8f, MansionZ + 0.5f);
            AudioSource drone = horror.AddComponent<AudioSource>();
            drone.playOnAwake = false;
            HauntedAmbience haunted = horror.AddComponent<HauntedAmbience>();
            haunted.howlClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Horror/Ambience/Distant_Wolf_Howl.mp3");
            haunted.howlVolume = 0.14f;
            // Both howl layers are distant accents, not constant foreground audio.
            haunted.firstHowlMinimumDelay = 60f;
            haunted.firstHowlMaximumDelay = 95f;
            haunted.minimumHowlInterval = 110f;
            haunted.maximumHowlInterval = 170f;
        }

        private static void ConfigureFootsteps(RecordedFootsteps footsteps, Transform movementOrigin)
        {
            footsteps.movementOrigin = movementOrigin;
            // Soft, leafy footfalls: use the licensed single-step recordings,
            // not the old hybrid click/thump bakes that sounded like hard shoes.
            footsteps.volume = 0.24f;
            footsteps.stepDistance = 0.72f;
            footsteps.minimumSpeed = 0.25f;
            footsteps.grassSteps = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Horror/Foley/Grass_Step_01.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Horror/Foley/Grass_Step_02.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Horror/Foley/Grass_Step_03.mp3")
            };
            footsteps.hardFloorSteps = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Horror/Foley/Wood_Step_01.mp3")
            };
            footsteps.hapticTick = false;
        }

        // --------------------------------------------------------------- drums

        private static DrumStick[] BuildSticks(Transform kit, AudioSource source, ParticleSystem dust)
        {
            DrumStick[] sticks = new DrumStick[2];
            string[] names = { "DrumStick_L", "DrumStick_R" };
            float[] xs = { -0.75f, 0.75f };

            for (int i = 0; i < 2; i++)
            {
                GameObject socket = new GameObject(names[i] + "_Socket");
                socket.transform.SetParent(kit, false);
                socket.transform.localPosition = new Vector3(xs[i], 0.03f, 0.82f);
                socket.transform.localRotation = Quaternion.Euler(-12f, 0f, 10f * (i == 0 ? -1f : 1f));

                GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stick.name = names[i];
                stick.transform.SetParent(kit, false);
                stick.transform.position = socket.transform.position;
                stick.transform.rotation = socket.transform.rotation;
                stick.transform.localScale = new Vector3(0.032f, 0.26f, 0.032f);

                Rigidbody body = stick.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                GameObject tip = new GameObject("Tip");
                tip.transform.SetParent(stick.transform, false);
                tip.transform.localPosition = new Vector3(0f, 1f, 0f);

                GameObject grip = new GameObject("Grip");
                grip.transform.SetParent(stick.transform, false);
                grip.transform.localPosition = new Vector3(0f, -0.55f, 0f);

                XRGrabInteractable grab = stick.AddComponent<XRGrabInteractable>();
                grab.attachTransform = grip.transform;
                grab.throwOnDetach = false;
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.selectMode = InteractableSelectMode.Multiple;

                DrumStick drumStick = stick.AddComponent<DrumStick>();
                drumStick.tip = tip.transform;
                drumStick.restSocket = socket.transform;

                sticks[i] = drumStick;
            }

            return sticks;
        }

        private static DrumPad[] BuildPads(Transform kit, AudioSource source, ParticleSystem dust, Material ringMaterial)
        {
            string[] padNames =
            {
                "Cylinder.014", "Cylinder.013", "Cylinder.006", "Cylinder.001",
                "Cylinder.002", "Circle.005", "Circle.003", "Circle.006"
            };
            string[] clipNames =
            {
                "snare_drum_3", "tom_2", "tom_2", "tom_2",
                "tom_2", "synth_side_stick", "crash_cymbal", "metal_snare"
            };
            float[] volumes = { 1f, 0.9f, 0.9f, 0.95f, 1f, 0.75f, 0.8f, 0.65f };
            float[] pitches = { 1f, 1.05f, 0.95f, 0.85f, 0.6f, 1.15f, 1f, 1.1f };

            List<DrumPad> pads = new List<DrumPad>();

            for (int i = 0; i < padNames.Length; i++)
            {
                Transform target = kit.Find(padNames[i]);
                if (target == null)
                {
                    Debug.LogWarning("[PixBand] Drum part not found: " + padNames[i]);
                    continue;
                }

                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer == null) continue;

                Bounds bounds = renderer.bounds;
                GameObject padGo = new GameObject("Pad_" + padNames[i]);
                padGo.transform.SetParent(target, true);
                padGo.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.01f, bounds.center.z);

                SphereCollider collider = padGo.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = Mathf.Max(0.12f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f);

                DrumPad pad = padGo.AddComponent<DrumPad>();
                pad.source = source;
                pad.clip = LoadClip(clipNames[i]);
                pad.volume = volumes[i];
                pad.pitchRange = new Vector2(pitches[i] * 0.97f, pitches[i] * 1.03f);
                pad.dust = dust;
                pad.minTipSpeed = 0.5f;
                // The trigger lives on this empty object; the visible head is the
                // mesh transform it was measured from, so it can recoil safely.
                pad.head = target;
                pad.cymbal = i >= 5;
                pad.recoilDepth = 0.010f;
                pad.ringMaterial = ringMaterial;
                pad.impactTint = pad.cymbal
                    ? new Color(1f, 0.86f, 0.55f)
                    : new Color(1f, 0.62f, 0.34f);

                pads.Add(pad);
            }

            return pads.ToArray();
        }

        private static ParticleSystem BuildDust(Transform parent)
        {
            GameObject go = new GameObject("DrumDust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
            main.startColor = new Color(0.75f, 0.7f, 0.55f, 0.55f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MakeParticleMaterial("DrumDust", "Legacy Shaders/Particles/Alpha Blended");
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private static AudioClip LoadClip(string name)
        {
            string path = "Assets/Audio/Horror/SFX/" + name + ".mp3";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[PixBand] Missing clip: " + path);
            return clip;
        }

        // ---------------------------------------------------------------- menu

        private static MenuBuild BuildMenu(Vector3 spawn)
        {
            MenuBuild menu = new MenuBuild();

            menu.root = new GameObject("StartMenu");
            menu.root.transform.position = spawn;

            Material plaque = MakeEmissive("Menu_Plaque", new Color(0.055f, 0.075f, 0.075f), Color.black);
            Material panel = MakeStandard("Menu_Panel", new Color(0.035f, 0.05f, 0.05f), 0.08f);
            Color text = new Color(0.82f, 1f, 0.9f);
            Color muted = new Color(0.5f, 0.72f, 0.62f);

            GameObject title = CreateLabel("MenuTitle", "PIXBAND", spawn + new Vector3(0f, 2.65f, 5.1f), 3.4f, text);
            title.transform.SetParent(menu.root.transform, true);
            TextMeshPro titleTmp = title.GetComponent<TextMeshPro>();
            titleTmp.fontSize = 13f;

            GameObject subtitle = CreateLabel("MenuSubtitle", "a forest.  a mansion.  a drum kit.", spawn + new Vector3(0f, 2.12f, 5.0f), 1.2f, muted);
            subtitle.transform.SetParent(menu.root.transform, true);

            MenuButton enter = CreateWorldPlaque(menu.root.transform, "Menu_Enter", "ENTER GAME",
                spawn + new Vector3(0f, 1.55f, 3.9f), new Vector2(1.6f, 0.44f), MenuAction.EnterGame, plaque, text);
            MenuButton controls = CreateWorldPlaque(menu.root.transform, "Menu_Controls", "CONTROLS",
                spawn + new Vector3(-1.05f, 1.03f, 3.9f), new Vector2(1.15f, 0.34f), MenuAction.ShowControls, plaque, text);
            MenuButton exit = CreateWorldPlaque(menu.root.transform, "Menu_Exit", "EXIT",
                spawn + new Vector3(1.05f, 1.03f, 3.9f), new Vector2(0.95f, 0.34f), MenuAction.ExitGame, plaque, text);

            menu.buttons.Add(enter);
            menu.buttons.Add(controls);
            menu.buttons.Add(exit);

            GameObject hint = CreateLabel("MenuHint", "aim + trigger    /    click", spawn + new Vector3(0f, 0.72f, 3.85f), 0.55f, muted);
            hint.transform.SetParent(menu.root.transform, true);

            // Controls panel (hidden until CONTROLS is pressed).
            menu.panel = new GameObject("ControlsPanel");
            menu.panel.transform.position = spawn + new Vector3(0f, 1.55f, 3.35f);
            menu.panel.AddComponent<Billboard>();

            CreateBox(menu.panel.transform, "Background", new Vector3(2.7f, 1.95f, 0.06f), new Vector3(0f, 0.05f, 0f), panel);
            CreatePanelText(menu.panel.transform, "PanelTitle", "CONTROLS", new Vector3(0f, 0.82f, -0.05f), 7f, 0.2f, text, TextAlignmentOptions.Center);

            CreatePanelText(menu.panel.transform, "VRHeader", "VR", new Vector3(-0.62f, 0.52f, -0.05f), 6f, 0.2f, text, TextAlignmentOptions.Left);
            CreatePanelText(menu.panel.transform, "PCHeader", "PC", new Vector3(0.48f, 0.52f, -0.05f), 6f, 0.2f, text, TextAlignmentOptions.Left);

            string[] vr = { "Left stick - Move", "Right stick - Snap turn", "Grip - Grab stick", "Trigger - Select / UI", "B / Y - Reset" };
            string[] pc = { "WASD - Move", "Mouse - Look", "E - Next stick / drop all", "Click - Strike pad", "R - Reset", "Tab - XR simulator", "Esc - Free mouse", "M - Menu" };
            CreateControlLines(menu.panel.transform, "VR", vr, -0.62f, 0.30f, muted);
            CreateControlLines(menu.panel.transform, "PC", pc, 0.48f, 0.30f, muted);

            MenuButton back = CreatePanelPlaque(menu.panel.transform, "Menu_Back", "BACK",
                new Vector3(0f, -0.72f, -0.08f), new Vector2(0.9f, 0.28f), MenuAction.HideControls, plaque, text);
            menu.buttons.Add(back);

            menu.panel.SetActive(false);
            return menu;
        }

        private static MenuButton CreateWorldPlaque(
            Transform parent,
            string name,
            string label,
            Vector3 worldPosition,
            Vector2 size,
            MenuAction action,
            Material material,
            Color textColor)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 0.06f);

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            go.AddComponent<Billboard>();
            go.AddComponent<XRSimpleInteractable>();

            MenuButton button = go.AddComponent<MenuButton>();
            button.action = action;
            button.plaque = renderer;

            AddPlaqueLabel(go.transform, label, size, textColor);
            return button;
        }

        private static MenuButton CreatePanelPlaque(
            Transform parent,
            string name,
            string label,
            Vector3 localPosition,
            Vector2 size,
            MenuAction action,
            Material material,
            Color textColor)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 0.05f);

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            go.AddComponent<XRSimpleInteractable>();

            MenuButton button = go.AddComponent<MenuButton>();
            button.action = action;
            button.plaque = renderer;

            AddPlaqueLabel(go.transform, label, size, textColor);
            return button;
        }

        private static void AddPlaqueLabel(Transform plaque, string text, Vector2 size, Color color)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(plaque, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            go.transform.localScale = new Vector3(0.2f / size.x, 0.2f / size.y, 3.5f);

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 6f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
        }

        private static void CreatePanelText(
            Transform parent,
            string name,
            string text,
            Vector3 localPosition,
            float fontSize,
            float scale,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(3.4f, 4.0f);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = false;
            tmp.lineSpacing = 0f;
        }

        private static void CreateControlLines(Transform parent, string prefix, string[] lines, float x, float topY, Color color)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                GameObject go = new GameObject(prefix + "Line_" + i);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(x, topY - i * 0.095f, -0.05f);
                go.transform.localScale = Vector3.one * 0.2f;

                TextMeshPro tmp = go.AddComponent<TextMeshPro>();
                tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
                tmp.text = lines[i];
                tmp.fontSize = 4.6f;
                tmp.color = color;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.enableWordWrapping = false;
            }
        }

        // ---------------------------------------------------------------- rigs

        private static void BuildRigs(Vector3 spawnPosition, MenuBuild menu)
        {
            GameObject setup = AssetDatabase.LoadAssetAtPath<GameObject>(XrSetupPath);
            GameObject xr = null;

            GameObject handLeft = AssetDatabase.LoadAssetAtPath<GameObject>(HandLeftModelPath);
            GameObject handRight = AssetDatabase.LoadAssetAtPath<GameObject>(HandRightModelPath);
            Material handMaterial = MakeHandMaterial();
            if (handLeft == null || handRight == null)
                Debug.LogWarning("[PixBand] Rigged hand meshes are missing; falling back to no hand visuals.");

            if (setup != null)
            {
                xr = (GameObject)PrefabUtility.InstantiatePrefab(setup);
                xr.name = "XROriginRig";
                xr.transform.position = spawnPosition;
                xr.transform.rotation = Quaternion.identity;

                Camera cam = xr.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    cam.tag = "MainCamera";
                    cam.nearClipPlane = 0.03f;
                    cam.farClipPlane = 120f;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = RenderSettings.fogColor;
                }

                PlayerBodyVisuals xrBody = xr.AddComponent<PlayerBodyVisuals>();
                xrBody.movementOrigin = cam != null ? cam.transform : null;
                Transform leftController = FindDeep(xr.transform, "Left Controller");
                Transform rightController = FindDeep(xr.transform, "Right Controller");
                xrBody.leftHandTarget = leftController;
                xrBody.rightHandTarget = rightController;
                xrBody.leftHandPrefab = handLeft;
                xrBody.rightHandPrefab = handRight;
                xrBody.handMaterial = handMaterial;
                xrBody.handScale = 1f;
                xrBody.SetHandPose(
                    Vector3.zero, new Vector3(0f, 0f, -50f),
                    Vector3.zero, new Vector3(180f, 0f, -90f));
                DisableControllerModelPrefab(leftController);
                DisableControllerModelPrefab(rightController);

                RecordedFootsteps xrFootsteps = xr.AddComponent<RecordedFootsteps>();
                ConfigureFootsteps(xrFootsteps, null);
                xrFootsteps.spatialBlend = 0.18f;
            }
            else
            {
                Debug.LogError("[PixBand] XR Interaction Setup prefab missing at " + XrSetupPath);
            }

            GameObject fp = new GameObject("FirstPersonRig");
            fp.transform.position = spawnPosition + Vector3.up * 0.05f;
            fp.transform.rotation = Quaternion.identity;

            CharacterController cc = fp.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.radius = 0.3f;

            GameObject head = new GameObject("Head");
            head.transform.SetParent(fp.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            Camera fpCam = head.AddComponent<Camera>();
            fpCam.tag = "MainCamera";
            fpCam.nearClipPlane = 0.03f;
            fpCam.farClipPlane = 120f;
            fpCam.clearFlags = CameraClearFlags.SolidColor;
            fpCam.backgroundColor = RenderSettings.fogColor;
            head.AddComponent<AudioListener>();

            DesktopRigController controller = fp.AddComponent<DesktopRigController>();
            controller.head = head.transform;

            PlayerBodyVisuals desktopBody = fp.AddComponent<PlayerBodyVisuals>();
            desktopBody.movementOrigin = head.transform;
            desktopBody.leftHandPrefab = handLeft;
            desktopBody.rightHandPrefab = handRight;
            desktopBody.handMaterial = handMaterial;
            desktopBody.handScale = 1f;
            desktopBody.proceduralArms = true;

            RecordedFootsteps desktopFootsteps = fp.AddComponent<RecordedFootsteps>();
            ConfigureFootsteps(desktopFootsteps, head.transform);
            desktopFootsteps.spatialBlend = 0.12f;

            DesktopStickGrabber grabber = fp.AddComponent<DesktopStickGrabber>();
            grabber.head = head.transform;
            // Keep the desktop hands slightly below the view center while
            // leaving enough of the grip visible during drum play. The holder
            // is now rig-root-relative so it does not rotate with camera pitch.
            grabber.holderOffset = new Vector3(0.28f, 1.12f, 0.42f);

            GameObject manager = new GameObject("RigManager");
            DualRigManager dual = manager.AddComponent<DualRigManager>();
            dual.firstPersonRig = fp;
            dual.xrOriginRig = xr;

            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrSimulatorPath);
            if (simulatorPrefab != null)
            {
                GameObject simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab);
                simulator.name = "XR Device Simulator";
                simulator.transform.position = spawnPosition;
                dual.xrDeviceSimulator = simulator;
                simulator.SetActive(false);
            }

            GameObject menuManager = new GameObject("MenuManager");
            StartMenuController startMenu = menuManager.AddComponent<StartMenuController>();
            startMenu.menuRoot = menu.root;
            startMenu.controlsPanel = menu.panel;
            startMenu.desktopRig = fp;
            startMenu.xrRig = xr;
            startMenu.desktopController = controller;

            if (xr != null)
            {
                LocomotionProvider[] providers = xr.GetComponentsInChildren<LocomotionProvider>(true);
                List<MonoBehaviour> list = new List<MonoBehaviour>();
                for (int i = 0; i < providers.Length; i++) list.Add(providers[i]);
                startMenu.locomotionProviders = list.ToArray();
            }

            for (int i = 0; i < menu.buttons.Count; i++)
                menu.buttons[i].menu = startMenu;
        }

        /// <summary>
        /// Replace the sample controller meshes with hand visuals parented to
        /// the tracked poses. Keep all interactor and input components intact.
        /// </summary>
        private static void DisableControllerModelPrefab(Transform controller)
        {
            if (controller == null) return;
            MonoBehaviour[] components = controller.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null) continue;
                SerializedObject serialized = new SerializedObject(component);
                SerializedProperty modelPrefab = serialized.FindProperty("m_ModelPrefab");
                if (modelPrefab == null || modelPrefab.objectReferenceValue == null) continue;
                modelPrefab.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        // --------------------------------------------------------------- labels

        private static GameObject CreateLabel(string name, string text, Vector3 position, float size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 8f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            go.transform.localScale = Vector3.one * Mathf.Max(0.02f, size * 0.12f);

            go.AddComponent<Billboard>();
            return go;
        }

        // --------------------------------------------------------------- helpers

        private static void SaveMeshAsset(Mesh mesh, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        /// <summary>World-space bounds of every renderer under a root.</summary>
        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Bounds of a skinned hierarchy taken from its bones. SkinnedMeshRenderer
        /// bounds come from the bind pose and can be far larger than the visible
        /// model, while the bones follow the authored size.
        /// </summary>
        private static Bounds CalculateBoneBounds(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            if (transforms.Length == 0) return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = new Bounds(transforms[0].position, Vector3.zero);
            for (int i = 1; i < transforms.Length; i++) bounds.Encapsulate(transforms[i].position);
            return bounds;
        }

        private static void CreateBox(Transform parent, string name, Vector3 size, Vector3 localPosition, Material material)
        {            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material MakeStandard(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.enableInstancing = true;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            string path = MaterialsDir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material MakeHandMaterial()
        {
            Material material = MakeStandard("Player_Hands_Skin", Color.white, 0.32f);
            Texture2D skin = AssetDatabase.LoadAssetAtPath<Texture2D>(HandSkinPath);
            if (skin != null) material.mainTexture = skin;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.34f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material MakeEmissive(string name, Color color, Color emission)
        {
            Material material = MakeStandard(name, color, 0.2f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplySurfaceTexture(
            Material material,
            string assetName,
            string pattern,
            Color dark,
            Color light,
            Vector2 tiling,
            float metallic,
            float smoothness)
        {
            Texture2D texture = CreateSurfaceTexture(assetName, pattern, dark, light);
            material.color = Color.white;
            material.mainTexture = texture;
            material.SetTextureScale("_MainTex", tiling);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static Texture2D CreateSurfaceTexture(string assetName, string pattern, Color dark, Color light)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false, false);
            texture.name = assetName;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float broad = Mathf.PerlinNoise(u * 7.3f + 1.9f, v * 7.3f + 7.1f);
                    float fine = Mathf.PerlinNoise(u * 28.7f + 5.2f, v * 28.7f + 2.3f);
                    float grain = Mathf.PerlinNoise(u * 72f + 3.1f, v * 9f + 4.7f);
                    float value;

                    switch (pattern)
                    {
                        case "bark":
                            value = broad * 0.44f + fine * 0.24f + grain * 0.32f;
                            value *= 0.76f + 0.24f * Mathf.PerlinNoise(u * 35f, v * 3.5f);
                            break;
                        case "wood":
                            float woodGrain = Mathf.Sin(v * 220f + broad * 13f) * 0.5f + 0.5f;
                            value = broad * 0.4f + grain * 0.35f + fine * 0.15f + woodGrain * 0.1f;
                            break;
                        case "metal":
                            value = broad * 0.22f + fine * 0.12f + grain * 0.66f;
                            break;
                        case "cymbal":
                            float dx = u - 0.5f;
                            float dy = v - 0.5f;
                            float radius = Mathf.Sqrt(dx * dx + dy * dy);
                            float rings = Mathf.Sin(radius * 720f + broad * 2.8f) * 0.5f + 0.5f;
                            value = broad * 0.30f + fine * 0.18f + rings * 0.52f;
                            break;
                        case "stone":
                            float bx = (x % 32) / 32f;
                            float by = (y % 22) / 22f;
                            float rowOffset = ((y / 22) % 2) * 0.5f;
                            float seam = (bx + rowOffset) % 1f < 0.055f || by < 0.07f ? 0.10f : 1f;
                            value = (broad * 0.58f + fine * 0.28f + grain * 0.14f) * seam;
                            break;
                        case "fabric":
                            float weaveX = Mathf.Sin(u * 804f) * 0.5f + 0.5f;
                            float weaveY = Mathf.Sin(v * 804f) * 0.5f + 0.5f;
                            value = broad * 0.52f + fine * 0.25f + (weaveX + weaveY) * 0.115f;
                            break;
                        case "bone":
                            float stain = Mathf.PerlinNoise(u * 17f + 4f, v * 17f + 6f);
                            value = broad * 0.36f + fine * 0.24f + stain * 0.4f;
                            break;
                        case "dirt":
                            float flecks = Mathf.PerlinNoise(u * 96f + 0.2f, v * 96f + 0.8f);
                            value = broad * 0.46f + fine * 0.28f + flecks * 0.26f;
                            break;
                        case "grass":
                            float grassFlecks = Mathf.PerlinNoise(u * 110f + 2.1f, v * 110f + 9.4f);
                            float alongBlade = Mathf.Sin((u * 0.82f + v * 0.57f) * 170f + fine * 2.6f) * 0.5f + 0.5f;
                            float acrossBlade = Mathf.Sin((-u * 0.57f + v * 0.82f) * 38f + broad * 3f) * 0.5f + 0.5f;
                            value = broad * 0.30f + fine * 0.19f + grassFlecks * 0.27f + alongBlade * acrossBlade * 0.24f;
                            break;
                        default:
                            value = broad * 0.48f + fine * 0.34f + grain * 0.18f;
                            break;
                    }

                    float scratches = Mathf.PerlinNoise(u * 120f + 12f, v * 120f + 18f);
                    value = Mathf.Clamp01(value * (0.82f + broad * 0.2f) + (scratches - 0.5f) * 0.08f);
                    pixels[y * size + x] = Color.Lerp(dark, light, value);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            string path = "Assets/Art/Textures/" + assetName + ".asset";
            EnsureFolder("Assets/Art/Textures");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Material MakeParticleMaterial(string name, string shaderName)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            Material material = new Material(shader);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Soft radial dot used by fireflies and stars. The asset is created once
        /// and reused: deleting and recreating it mid-build used to invalidate
        /// every material that already referenced the previous object, which is
        /// what turned the mist and cloud billboards into opaque rectangles.
        /// </summary>
        private static Texture2D MakeSoftDotTexture(string assetName)
        {
            string path = "Assets/Art/Textures/" + assetName + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            EnsureFolder("Assets/Art/Textures");
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        /// <summary>Ring sprite for the drum impact flash (white, alpha in the ring).</summary>
        private static Texture2D MakeImpactRingTexture()
        {
            const string path = "Assets/Art/Textures/Drum_Impact_Ring.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.74f) / 0.26f);
                    float inner = Mathf.Clamp01(1f - d) * 0.25f;
                    float a = Mathf.Clamp01(ring * ring + inner);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            EnsureFolder("Assets/Art/Textures");
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        /// <summary>Hooded silhouette used for the ghosts behind the windows.</summary>
        private static Texture2D MakeGhostTexture()
        {
            const string path = "Assets/Art/Textures/Ghost_Silhouette.asset";
            const int width = 128;
            const int height = 192;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool created = texture == null;
            if (created)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.name = "Ghost_Silhouette";
            }

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = (x - width * 0.5f) / (width * 0.5f);
                    float py = y / (float)height;

                    // Robe: widest at the ragged hem, sloping up to narrow
                    // shoulders, so the shape reads as a body and not a bell.
                    float robe = 0f;
                    if (py <= 0.755f)
                    {
                        float t = Mathf.Clamp01(py / 0.755f);
                        float half = Mathf.Lerp(0.82f, 0.44f, Mathf.SmoothStep(0f, 1f, t));
                        float edge = Mathf.Clamp01((half - Mathf.Abs(px)) / 0.055f);
                        float hem = Mathf.Clamp01(
                            (py - 0.045f - 0.028f * Mathf.Sin(px * 13f + 1.3f)) / 0.045f);
                        robe = edge * hem;
                    }

                    // Hood: an ellipse with a slight point at the crown.
                    float hoodX = px / 0.265f;
                    float hoodY = (py - 0.845f) / 0.172f;
                    float hood = Mathf.Clamp01(1f - Mathf.Sqrt(hoodX * hoodX + hoodY * hoodY));
                    if (py > 0.92f)
                    {
                        float crown = Mathf.Clamp01((0.265f - Mathf.Abs(px) * (1f + (py - 0.92f) * 9f)) / 0.06f);
                        hood = Mathf.Max(hood, crown * Mathf.Clamp01((1.02f - py) / 0.02f));
                    }

                    // Shoulders: a soft cap that joins the hood to the robe.
                    float capX = px / 0.38f;
                    float capY = (py - 0.735f) / 0.09f;
                    float cap = Mathf.Clamp01(1f - Mathf.Sqrt(capX * capX + capY * capY));

                    float alpha = Mathf.Max(robe, Mathf.Max(cap, hood));

                    // A notch under the chin keeps head and body apart.
                    float notch = Mathf.Clamp01(
                        1f - Mathf.Sqrt((px / 0.12f) * (px / 0.12f) +
                                        ((py - 0.762f) / 0.042f) * ((py - 0.762f) / 0.042f)));
                    alpha *= 1f - 0.85f * notch;

                    // Hollow eyes: the pane glows through the silhouette.
                    for (int e = -1; e <= 1; e += 2)
                    {
                        float ex = (px - e * 0.095f) / 0.05f;
                        float ey = (py - 0.868f) / 0.034f;
                        float eye = Mathf.Clamp01(1f - Mathf.Sqrt(ex * ex + ey * ey));
                        alpha = Mathf.Lerp(alpha, alpha * 0.10f, eye);
                    }

                    alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(alpha));
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            if (created)
            {
                EnsureFolder("Assets/Art/Textures");
                AssetDatabase.CreateAsset(texture, path);
            }
            else
            {
                EditorUtility.SetDirty(texture);
            }

            return texture;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string child = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
