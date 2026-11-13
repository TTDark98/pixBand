using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixBand.Core
{
    public enum MenuAction
    {
        EnterGame,
        ShowControls,
        HideControls,
        ExitGame
    }

    /// <summary>
    /// A world-space menu plaque. Works with the XR ray interactor (trigger) and
    /// the desktop menu's physics-ray pointer while the cursor is free.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class MenuButton : MonoBehaviour
    {
        public StartMenuController menu;
        public MenuAction action;
        public Renderer plaque;
        public Color hoverEmission = new Color(0.35f, 0.95f, 0.75f);

        private XRSimpleInteractable interactable;
        private MaterialPropertyBlock block;
        private Vector3 baseScale;
        private bool hovering;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            block = new MaterialPropertyBlock();
            baseScale = transform.localScale;

            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
                interactable.selectEntered.AddListener(OnSelected);
            }

            SetHover(false);
        }

        private void OnDestroy()
        {
            if (interactable == null) return;
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelected);
        }

        private void OnHoverEntered(HoverEnterEventArgs args) { SetHover(true); }
        private void OnHoverExited(HoverExitEventArgs args) { SetHover(false); }
        private void OnSelected(SelectEnterEventArgs args) { Activate(); }

        public void SetDesktopHover(bool on)
        {
            SetHover(on);
        }

        public void Activate()
        {
            if (menu != null) menu.Handle(action);
        }

        private void SetHover(bool on)
        {
            hovering = on;
            transform.localScale = baseScale * (hovering ? 1.05f : 1f);

            if (plaque == null) return;
            plaque.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", hovering ? hoverEmission : Color.black);
            plaque.SetPropertyBlock(block);
        }
    }
}
