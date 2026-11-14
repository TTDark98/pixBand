# Third-party model credits

The models under `Assets/Models` come from external packs. This file records
where each one came from so the demo can be attributed correctly if it is
shared or published.

| Project files | Asset | Creator / source | License |
| --- | --- | --- | --- |
| `Cat/Cat_Animated.fbx`, `Cat/Textures/*` | "An animated cat" (black cat, grooming loop) | mark2580, via Sketchfab | **CC BY 4.0** — attribution required |
| `Skeletons/Skeleton_Male.fbx`, `Skeletons/Textures/*` | Male anatomical skeleton, rigged | Supplied by the project owner | Unknown — confirm before public distribution |
| `Player/HandsVR_Left.fbx`, `Player/HandsVR_Right.fbx`, `Player/T_VRHands_Base_color.png` | Rigged VR hands, split into left/right meshes | Supplied by the project owner (`free-pack-vr-hands-rigged.zip`) | Unknown — confirm before public distribution |

## Changes made for this project

- The cat and skeleton were re-exported from Blender with the textures embedded
  as external project files, because Unity did not extract Blender-embedded FBX
  textures. The skeleton textures were downscaled to 512 px, the cat textures
  kept at 2048 px.
- The VR hand pack was split into two static meshes (left and right) and posed
  with a relaxed grip; the original rig and animation were not imported.
- The skeleton is used with no animation clips: `SkeletonModelDancer` rotates its
  imported bones at runtime to follow the drum performance.

Audio credits are separate and live in
`Assets/Audio/Horror/CC0_Sound_Sources.md`.
