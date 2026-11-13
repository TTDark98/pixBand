using System.Collections.Generic;
using UnityEngine;

namespace PixBand.Audio
{
    public class StageAudioController : MonoBehaviour
    {
        [Header("Instruments")]
        [SerializeField] private InstrumentInteractable drumKit;
        [SerializeField] private InstrumentInteractable keyboard;
        [SerializeField] private InstrumentInteractable guitar;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode toggleDrumLoopKey = KeyCode.B;
        [SerializeField] private KeyCode crashCymbalKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode snare3Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode snare5Key = KeyCode.Alpha3;
        [SerializeField] private KeyCode metalSnareKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode tomKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode sideStickKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode toggleHudKey = KeyCode.H;

        private DualRigManager rigManager;
        private bool showHud = true;
        private Texture2D panelBackground;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle itemStyle;
        private GUIStyle badgeStyle;
        private GUIStyle miniBadgeStyle;

        private void Awake()
        {
            rigManager = FindObjectOfType<DualRigManager>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleHudKey))
            {
                showHud = !showHud;
            }

            // Toggle drum loop
            if (Input.GetKeyDown(toggleDrumLoopKey) && drumKit != null)
            {
                drumKit.ToggleLoop();
            }

            // One-shots on Drum Kit
            if (drumKit != null)
            {
                if (Input.GetKeyDown(crashCymbalKey)) drumKit.PlayOneShot(0);
                if (Input.GetKeyDown(snare3Key)) drumKit.PlayOneShot(1);
                if (Input.GetKeyDown(snare5Key)) drumKit.PlayOneShot(2);
                if (Input.GetKeyDown(metalSnareKey)) drumKit.PlayOneShot(3);
                if (Input.GetKeyDown(tomKey)) drumKit.PlayOneShot(4);
            }

            // Keyboard one-shot
            if (keyboard != null)
            {
                if (Input.GetKeyDown(sideStickKey)) keyboard.PlayOneShot(0);
            }
        }

        private void InitStyles()
        {
            if (panelBackground == null)
            {
                panelBackground = new Texture2D(1, 1);
                panelBackground.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.90f));
                panelBackground.Apply();
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelBackground;
                panelStyle.padding = new RectOffset(16, 16, 14, 14);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 15;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(0.95f, 0.96f, 1.0f);
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(GUI.skin.label);
                sectionStyle.fontSize = 12;
                sectionStyle.fontStyle = FontStyle.Bold;
                sectionStyle.normal.textColor = new Color(0.40f, 0.80f, 1.0f);
            }

            if (itemStyle == null)
            {
                itemStyle = new GUIStyle(GUI.skin.label);
                itemStyle.fontSize = 12;
                itemStyle.richText = true;
                itemStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
            }

            if (badgeStyle == null)
            {
                badgeStyle = new GUIStyle(GUI.skin.label);
                badgeStyle.fontSize = 12;
                badgeStyle.fontStyle = FontStyle.Bold;
                badgeStyle.richText = true;
            }

            if (miniBadgeStyle == null)
            {
                miniBadgeStyle = new GUIStyle(GUI.skin.box);
                miniBadgeStyle.normal.background = panelBackground;
                miniBadgeStyle.normal.textColor = Color.white;
                miniBadgeStyle.fontSize = 12;
                miniBadgeStyle.richText = true;
                miniBadgeStyle.padding = new RectOffset(8, 8, 4, 4);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            if (!showHud)
            {
                // Compact hint pill
                if (GUI.Button(new Rect(14, 14, 150, 28), "<b>[H] Show Controls</b>", miniBadgeStyle))
                {
                    showHud = true;
                }
                return;
            }

            float panelWidth = 360f;
            float panelHeight = 415f;
            Rect panelRect = new Rect(14, 14, panelWidth, panelHeight);

            GUILayout.BeginArea(panelRect, panelStyle);

            // Header with Hide button
            GUILayout.BeginHorizontal();
            GUILayout.Label("STAGE SPATIAL AUDIO", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("[H] Hide", GUILayout.Width(64), GUILayout.Height(20)))
            {
                showHud = false;
            }
            GUILayout.EndHorizontal();

            // Mode Pill
            string modeName = (rigManager != null) ? rigManager.CurrentMode.ToString() : "WASD FirstPerson";
            bool isFP = (rigManager == null || rigManager.CurrentMode == DualRigManager.ActiveRigMode.FirstPersonWASD);
            string modeColor = isFP ? "#00E5FF" : "#FFD600";
            GUILayout.Label($"Mode: <color={modeColor}><b>{modeName}</b></color> <size=10>(Press [Tab] to toggle)</size>", itemStyle);
            GUILayout.Space(4);

            // Controls Section
            GUILayout.Label("LOCOMOTION", sectionStyle);
            GUILayout.Label("• <b>[W][A][S][D]</b> Move | <b>[Mouse]</b> Look 360°", itemStyle);
            GUILayout.Label("• <b>[Shift]</b> Sprint | <b>[Esc]</b> Cursor Lock", itemStyle);
            GUILayout.Space(4);

            // Spatial Audio Instruments Section
            GUILayout.Label("3D SPATIAL INSTRUMENTS", sectionStyle);
            string drumStatus = (drumKit != null && drumKit.IsLoopPlaying)
                ? "<color=#00FF88>● PLAYING (Looped)</color>"
                : "<color=#888888>○ PAUSED</color>";
            GUILayout.Label($"• <b>[B]</b> Drum Beat Loop: {drumStatus}", itemStyle);
            GUILayout.Label("• <b>[1]</b> Crash Cymbal   • <b>[2]</b> Snare #3", itemStyle);
            GUILayout.Label("• <b>[3]</b> Snare #5        • <b>[4]</b> Metal Snare", itemStyle);
            GUILayout.Label("• <b>[5]</b> Tom Drum       • <b>[6]</b> Synth Side Stick", itemStyle);
            GUILayout.Space(4);

            // Live Acoustics Distance Meters
            GUILayout.Label("ACOUSTIC DISTANCES TO LISTENER", sectionStyle);
            if (drumKit != null)
            {
                float d = drumKit.GetDistanceToListener();
                string distStr = d >= 0 ? $"{d:F1}m" : "N/A";
                GUILayout.Label($"  🥁 <b>Drum Kit</b> (Center): <color=#00E5FF>{distStr}</color>", itemStyle);
            }
            if (keyboard != null)
            {
                float d = keyboard.GetDistanceToListener();
                string distStr = d >= 0 ? $"{d:F1}m" : "N/A";
                GUILayout.Label($"  🎹 <b>Yamaha Keyboard</b> (Stage Left): <color=#00E5FF>{distStr}</color>", itemStyle);
            }
            if (guitar != null)
            {
                float d = guitar.GetDistanceToListener();
                string distStr = d >= 0 ? $"{d:F1}m" : "N/A";
                GUILayout.Label($"  🎸 <b>Guitar Collection</b> (Stage Right): <color=#00E5FF>{distStr}</color>", itemStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=10><color=#707888>Walk around to hear 3D spatial pan & attenuation.</color></size>", itemStyle);

            GUILayout.EndArea();
        }

        public void SetReferences(InstrumentInteractable drums, InstrumentInteractable keys, InstrumentInteractable gtr)
        {
            drumKit = drums;
            keyboard = keys;
            guitar = gtr;
        }
    }
}
