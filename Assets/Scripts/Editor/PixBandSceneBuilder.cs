using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using PixBand.Audio;
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
            ApplySurfaceTexture(ground, "Forest_Moss", "mottle", new Color(0.035f, 0.055f, 0.035f), new Color(0.15f, 0.20f, 0.11f), new Vector2(6f, 6f), 0f, 0.02f);
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
            BuildFogLayer("Forest_Ground_Mist", new Vector3(0f, 1.1f, 17f), new Vector3(62f, 1.4f, 62f), 44, 0.075f);
            BuildFogLayer("Mansion_Room_Mist", new Vector3(MansionX, Height(MansionX, MansionZ) + 1.1f, MansionZ), new Vector3(8f, 0.8f, 8f), 10, 0.025f);
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
            if (pads.Length == 0) pads = BuildPads(kit.transform, source, dust);

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

        // ------------------------------------------------------------- terrain

        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.055f, 0.085f, 0.068f);
            RenderSettings.fogDensity = 0.052f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.145f, 0.115f);
            RenderSettings.skybox = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.sun = null;

            GameObject moon = new GameObject("Moonlight");
            Light light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.72f, 0.75f);
            light.intensity = 1.15f;
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

            float[] baseY = { 1.6f, 2.5f, 3.4f, 4.3f };
            float[] radius = { 1.25f, 1.02f, 0.78f, 0.50f };
            float[] tierHeight = { 1.5f, 1.4f, 1.25f, 1.0f };

            for (int tier = 0; tier < baseY.Length; tier++)
            {
                float r = radius[tier] * width * (0.92f + (float)random.NextDouble() * 0.16f);
                float y0 = baseY[tier];
                float y1 = y0 + tierHeight[tier];
                int ringSides = 7;
                int start = verts.Count;

                for (int s = 0; s < ringSides; s++)
                {
                    float a = (float)s / ringSides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y0, Mathf.Sin(a) * r));
                    normals.Add(new Vector3(Mathf.Cos(a) * 0.6f, 0.35f, Mathf.Sin(a) * 0.6f).normalized);
                    uvs.Add(new Vector2((float)s / ringSides, 0f));
                }

                int apex = verts.Count;
                verts.Add(new Vector3(0f, y1, 0f));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f, 1f));

                for (int s = 0; s < ringSides; s++)
                {
                    int next = (s + 1) % ringSides;
                    leafTris.Add(start + s);
                    leafTris.Add(start + next);
                    leafTris.Add(apex);
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

            // Interior: riser, rug, two flickering lanterns.
            CreateBox(root.transform, "Riser", new Vector3(3.6f, 0.2f, 3.0f), new Vector3(0f, floorTop + 0.1f, 0.6f), wood);
            Material rug = MakeStandard("Rug_Red", new Color(0.16f, 0.035f, 0.035f), 0.02f);
            ApplySurfaceTexture(rug, "Mansion_Rug_Woven", "fabric", new Color(0.055f, 0.012f, 0.018f), new Color(0.28f, 0.055f, 0.045f), new Vector2(3f, 2f), 0f, 0.04f);
            CreateBox(root.transform, "Rug", new Vector3(4.2f, 0.02f, 2.2f), new Vector3(0f, floorTop + 0.01f, -2.3f), rug);

            BuildInteriorLantern(root.transform, new Vector3(-3.4f, 3.35f, -0.4f), lanternGlow);
            BuildInteriorLantern(root.transform, new Vector3(3.4f, 3.35f, 0.4f), lanternGlow);
            BuildInteriorLights(root.transform);
            BuildSkeleton(root.transform, "Skeleton_Left", new Vector3(-3.68f, floorTop, 1.0f), false, bone, socket);
            BuildSkeleton(root.transform, "Skeleton_Right", new Vector3(3.68f, floorTop, 1.0f), true, bone, socket);

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

        private static void BuildSkeleton(Transform parent, string name, Vector3 localPosition, bool slumped, Material bone, Material socket)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(0f, slumped ? 165f : 195f, slumped ? -8f : 6f);

            CreateSkeletonSphere(root.transform, "Pelvis", new Vector3(0f, 0.72f, 0f), new Vector3(0.32f, 0.2f, 0.2f), bone);
            CreateSkeletonBone(root.transform, "Spine_Lower", new Vector3(0f, 0.78f, 0f), new Vector3(0f, 1.04f, 0f), 0.055f, bone);
            CreateSkeletonBone(root.transform, "Spine_Upper", new Vector3(0f, 1.03f, 0f), new Vector3(0f, 1.27f, 0f), 0.05f, bone);
            CreateSkeletonBone(root.transform, "Neck", new Vector3(0f, 1.24f, 0f), new Vector3(0f, 1.42f, 0f), 0.045f, bone);
            CreateSkeletonSphere(root.transform, "Skull", new Vector3(0f, 1.57f, 0.015f), new Vector3(0.23f, 0.26f, 0.2f), bone);
            CreateSkeletonBone(root.transform, "Jaw", new Vector3(-0.11f, 1.48f, 0.11f), new Vector3(0.11f, 1.48f, 0.11f), 0.035f, bone);

            // Dark eye sockets and a nasal cavity give the skull a readable face.
            CreateSkeletonSphere(root.transform, "EyeSocket_L", new Vector3(-0.075f, 1.59f, 0.184f), new Vector3(0.052f, 0.063f, 0.025f), socket);
            CreateSkeletonSphere(root.transform, "EyeSocket_R", new Vector3(0.075f, 1.59f, 0.184f), new Vector3(0.052f, 0.063f, 0.025f), socket);
            CreateSkeletonSphere(root.transform, "Nose_Cavity", new Vector3(0f, 1.51f, 0.187f), new Vector3(0.035f, 0.055f, 0.02f), socket);

            for (int rib = 0; rib < 3; rib++)
            {
                float y = 0.94f + rib * 0.11f;
                float width = 0.25f - rib * 0.025f;
                CreateSkeletonBone(root.transform, "Rib_L_" + rib, new Vector3(-0.025f, y, 0f), new Vector3(-width, y - 0.04f, 0.015f), 0.028f, bone);
                CreateSkeletonBone(root.transform, "Rib_R_" + rib, new Vector3(0.025f, y, 0f), new Vector3(width, y - 0.04f, 0.015f), 0.028f, bone);
            }
            CreateSkeletonBone(root.transform, "Clavicle_L", new Vector3(0f, 1.25f, 0f), new Vector3(-0.25f, 1.22f, 0f), 0.035f, bone);
            CreateSkeletonBone(root.transform, "Clavicle_R", new Vector3(0f, 1.25f, 0f), new Vector3(0.25f, 1.22f, 0f), 0.035f, bone);

            float leftHand = slumped ? -0.55f : -0.48f;
            float rightHand = slumped ? 0.35f : 0.58f;
            CreateSkeletonBone(root.transform, "Arm_L_Upper", new Vector3(-0.25f, 1.2f, 0f), new Vector3(-0.37f, 0.92f, 0f), 0.045f, bone);
            CreateSkeletonBone(root.transform, "Arm_L_Fore", new Vector3(-0.37f, 0.92f, 0f), new Vector3(leftHand, 0.64f, 0.08f), 0.035f, bone);
            CreateSkeletonSphere(root.transform, "Hand_L", new Vector3(leftHand, 0.62f, 0.08f), new Vector3(0.09f, 0.07f, 0.06f), bone);
            CreateSkeletonBone(root.transform, "Arm_R_Upper", new Vector3(0.25f, 1.2f, 0f), new Vector3(0.36f, 0.92f, 0f), 0.045f, bone);
            CreateSkeletonBone(root.transform, "Arm_R_Fore", new Vector3(0.36f, 0.92f, 0f), new Vector3(rightHand, 0.64f, 0.08f), 0.035f, bone);
            CreateSkeletonSphere(root.transform, "Hand_R", new Vector3(rightHand, 0.62f, 0.08f), new Vector3(0.09f, 0.07f, 0.06f), bone);

            CreateSkeletonBone(root.transform, "Leg_L_Upper", new Vector3(-0.11f, 0.69f, 0f), new Vector3(-0.18f, 0.37f, 0f), 0.06f, bone);
            CreateSkeletonBone(root.transform, "Leg_L_Lower", new Vector3(-0.18f, 0.37f, 0f), new Vector3(-0.16f, 0.08f, 0.04f), 0.042f, bone);
            CreateSkeletonBone(root.transform, "Foot_L", new Vector3(-0.16f, 0.08f, 0.04f), new Vector3(-0.31f, 0.05f, 0.22f), 0.04f, bone);
            CreateSkeletonBone(root.transform, "Leg_R_Upper", new Vector3(0.11f, 0.69f, 0f), new Vector3(0.18f, 0.37f, 0f), 0.06f, bone);
            CreateSkeletonBone(root.transform, "Leg_R_Lower", new Vector3(0.18f, 0.37f, 0f), new Vector3(0.16f, 0.08f, 0.04f), 0.042f, bone);
            CreateSkeletonBone(root.transform, "Foot_R", new Vector3(0.16f, 0.08f, 0.04f), new Vector3(0.31f, 0.05f, 0.22f), 0.04f, bone);

            CombineSkeletonMeshes(root.transform);
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

        private static void CombineSkeletonMeshes(Transform root)
        {
            Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null) continue;

                Material material = renderer.sharedMaterial;
                if (!groups.ContainsKey(material)) groups[material] = new List<CombineInstance>();
                CombineInstance instance = new CombineInstance();
                instance.mesh = filter.sharedMesh;
                instance.transform = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                groups[material].Add(instance);
            }

            foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
            {
                Mesh mesh = new Mesh();
                mesh.name = root.name + "_" + group.Key.name;
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                GameObject combined = new GameObject(mesh.name);
                combined.transform.SetParent(root, false);
                combined.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = combined.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = group.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) Object.DestroyImmediate(renderers[i].gameObject);
        }

        private static void BuildInteriorLantern(Transform parent, Vector3 localPosition, Material glow)
        {
            GameObject lantern = new GameObject("Interior_Lantern");
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = localPosition;

            CreateBox(lantern.transform, "Glow", new Vector3(0.22f, 0.32f, 0.22f), Vector3.zero, glow);

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

            Texture2D dot = MakeDotTexture();
            Material mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            mat.mainTexture = dot;
            mat.SetColor("_TintColor", new Color(0.7f, 1f, 0.6f, 1f));

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildFogLayer(string name, Vector3 position, Vector3 volume, int maxParticles, float alpha)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;

            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 30f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.05f);
            bool interior = volume.x < 10f;
            main.startSize = interior
                ? new ParticleSystem.MinMaxCurve(2.0f, 3.6f)
                : new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startColor = new Color(0.55f, 0.66f, 0.61f, alpha);
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = Mathf.Max(0.3f, maxParticles / 20f);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = volume;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.045f, 0.045f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.015f, 0.025f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);

            Texture2D soft = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/FireflyDot.asset");
            Material material = MakeParticleMaterial("Soft_Mist", "Legacy Shaders/Particles/Alpha Blended");
            if (soft != null) material.mainTexture = soft;
            material.SetColor("_TintColor", Color.white);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildAmbience()
        {
            GameObject go = new GameObject("Wind_Ambience");
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            WindAmbience wind = go.AddComponent<WindAmbience>();
            wind.volume = 0.62f;
            go.transform.position = new Vector3(0f, 3f, 15f);

            GameObject horror = new GameObject("Haunted_Ambience");
            horror.transform.position = new Vector3(MansionX, Height(MansionX, MansionZ) + 1.8f, MansionZ + 0.5f);
            AudioSource drone = horror.AddComponent<AudioSource>();
            drone.playOnAwake = false;
            HauntedAmbience haunted = horror.AddComponent<HauntedAmbience>();
            haunted.droneVolume = 0.48f;
            haunted.wailVolume = 0.34f;
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

        private static DrumPad[] BuildPads(Transform kit, AudioSource source, ParticleSystem dust)
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

            DesktopStickGrabber grabber = fp.AddComponent<DesktopStickGrabber>();
            grabber.head = head.transform;

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

        private static void CreateBox(Transform parent, string name, Vector3 size, Vector3 localPosition, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        private static Texture2D MakeDotTexture()
        {
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
            string path = "Assets/Art/FireflyDot.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(texture, path);
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
