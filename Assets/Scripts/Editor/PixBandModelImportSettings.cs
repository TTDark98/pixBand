using UnityEditor;
using UnityEngine;

namespace PixBand.EditorTools
{
    /// <summary>
    /// Import settings for the supplied third-party models. Keeping them in one
    /// postprocessor means a fresh clone or a reimport gets the same result:
    /// the cat plays its grooming clip as Legacy animation, the skeleton keeps
    /// its bone hierarchy for the code-driven dance, and the hand meshes stay
    /// plain static meshes with our own material.
    /// </summary>
    public class PixBandModelImportSettings : AssetPostprocessor
    {
        private const string CatPath = "Assets/Models/Cat/Cat_Animated.fbx";
        private const string SkeletonPath = "Assets/Models/Skeletons/Skeleton_Male.fbx";
        private const string HandLeftPath = "Assets/Models/Player/HandsVR_Left.fbx";
        private const string HandRightPath = "Assets/Models/Player/HandsVR_Right.fbx";
        private const string HandTexturePath = "Assets/Models/Player/T_VRHands_Base_color.png";

        private void OnPreprocessModel()
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            string path = assetPath.Replace('\\', '/');

            if (path == CatPath)
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.animationWrapMode = WrapMode.Loop;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.importConstraints = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                return;
            }

            if (path == SkeletonPath)
            {
                // Static hierarchy: SkeletonModelDancer rotates the imported
                // bones at runtime, so no clips or avatar are needed.
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.importConstraints = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.addCollider = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                return;
            }

            if (path == HandLeftPath || path == HandRightPath)
            {
                // Baked, posed meshes: no rig, no animation, and the builder
                // assigns the skin material explicitly.
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.importConstraints = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.addCollider = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.None;
            }
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.Replace('\\', '/') != HandTexturePath) return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
