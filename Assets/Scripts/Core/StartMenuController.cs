using UnityEngine;

namespace PixBand.Core
{
    /// <summary>
    /// Owns the launch menu and the controls screen. Locomotion stays frozen until
    /// ENTER GAME, so the player starts on the menu instead of walking away from it.
    /// </summary>
    public class StartMenuController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject menuRoot;
        public GameObject controlsPanel;

        [Header("Rigs")]
        public GameObject desktopRig;
        public GameObject xrRig;
        public DesktopRigController desktopController;

        [Header("Locomotion")]
        public MonoBehaviour[] locomotionProviders;

        private bool menuOpen = true;
        private MenuButton desktopHoveredButton;

        public bool IsMenuOpen { get { return menuOpen; } }
        public bool IsControlsOpen { get { return controlsPanel != null && controlsPanel.activeSelf; } }

        private void Awake()
        {
            if (desktopController == null && desktopRig != null)
                desktopController = desktopRig.GetComponent<DesktopRigController>();

            // Stop the desktop rig from locking the cursor before the menu is clicked.
            if (desktopController != null) desktopController.enabled = false;

            SetLocomotion(false);
            if (menuRoot != null) menuRoot.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            DesktopRigController.SetCursor(false);
        }

        private void Update()
        {
            if (!menuOpen)
            {
                if (Input.GetKeyDown(KeyCode.M)) ShowMenu();
                return;
            }

            UpdateDesktopPointer();

            if (IsControlsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.C))
                    HideControls();
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    EnterGame();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                EnterGame();
            else if (Input.GetKeyDown(KeyCode.C))
                ShowControls();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
                ExitGame();
        }

        public void Handle(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.EnterGame: EnterGame(); break;
                case MenuAction.ShowControls: ShowControls(); break;
                case MenuAction.HideControls: HideControls(); break;
                case MenuAction.ExitGame: ExitGame(); break;
            }
        }

        public void EnterGame()
        {
            menuOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            SetLocomotion(true);

            bool desktop = desktopRig != null && desktopRig.activeInHierarchy;
            if (desktop && desktopController != null)
            {
                desktopController.enabled = true;
                DesktopRigController.SetCursor(true);
            }
            else
            {
                DesktopRigController.SetCursor(false);
            }
        }

        /// <summary>Reopen the menu from inside the game (M on desktop).</summary>
        public void ShowMenu()
        {
            menuOpen = true;
            if (menuRoot != null) menuRoot.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            SetLocomotion(false);
            if (desktopController != null) desktopController.enabled = false;
            DesktopRigController.SetCursor(false);
        }

        public void ShowControls()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(true);
            DesktopRigController.SetCursor(false);
        }

        public void HideControls()
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);
            DesktopRigController.SetCursor(false);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetLocomotion(bool on)
        {
            if (locomotionProviders == null) return;
            for (int i = 0; i < locomotionProviders.Length; i++)
            {
                if (locomotionProviders[i] != null)
                    locomotionProviders[i].enabled = on;
            }
        }

        private void UpdateDesktopPointer()
        {
            Camera camera = Camera.main;
            MenuButton target = null;
            if (camera != null)
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 20f, ~0, QueryTriggerInteraction.Collide))
                    target = hit.collider.GetComponentInParent<MenuButton>();
            }

            if (target != desktopHoveredButton)
            {
                if (desktopHoveredButton != null) desktopHoveredButton.SetDesktopHover(false);
                desktopHoveredButton = target;
                if (desktopHoveredButton != null) desktopHoveredButton.SetDesktopHover(true);
            }

            if (target != null && Input.GetMouseButtonDown(0))
                target.Activate();
        }
    }
}
