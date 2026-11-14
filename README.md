# PixBand

Quest-ready forest-to-mansion drum experience, built with Unity **2022.3.62f3**.

## Open and build

1. Clone this repository and open its root folder in Unity Hub with Unity 2022.3.62f3.
2. Let Unity Package Manager resolve `Packages/manifest.json`; keep the checked-in
   `Packages/packages-lock.json` so package versions remain reproducible.
3. Open `Assets/Scenes/PixBand_Demo.unity` (it is already the enabled build scene).
4. To regenerate the procedural scene, use **PixBand → Rebuild Demo Scene**.
5. To create a Quest APK, use **PixBand → Build Android APK**. The output is
   `Builds/pixBand.apk` (Android ARM64, IL2CPP, OpenXR).

The build scene, XR loader settings, project settings, package manifest, lock file,
asset GUIDs, and `.meta` files are source-controlled. Unity's `Library`, `Temp`,
`Logs`, user settings, generated IDE files, and APK output are intentionally
ignored; each developer/editor regenerates them locally.

## Project layout

- `Assets/Scripts/Core` — rig controls, audio ambience, footsteps, and menu.
- `Assets/Scripts/Interaction` — sticks, drum pads, proximity, and collision safety.
- `Assets/Scripts/Editor` — scene generation and Android/Linux build menu commands.
- `Assets/Scenes` — the demo scene and retained earlier scene variants.
- `Assets/Models`, `Assets/Audio`, `Assets/Materials`, `Assets/Art` — source and
  generated content used by the demo.

Recorded forest wind, rare wolf howls, and surface-matched footsteps are bundled
locally under `Assets/Audio/Horror`; they require no runtime network access.
Source pages and CC0 licensing are documented in
`Assets/Audio/Horror/CC0_Sound_Sources.md`. Drum hit clips are included under
`Assets/Audio/Horror/SFX` and are not changed by the ambience/foley setup.

Third-party models (the entrance cat, the skeleton dancers, and the VR hands)
and their attribution requirements are listed in `Assets/Models/CREDITS.md`.
