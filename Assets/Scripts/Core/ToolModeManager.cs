using UnityEngine;
using UnityEngine.UI;

namespace CatRaising.Core
{
    /// <summary>
    /// Manages the current interaction tool mode: Hand (petting) or FeatherToy (playing).
    /// Attach to a UI Button and assign the button in the Inspector.
    /// </summary>
    public class ToolModeManager : MonoBehaviour
    {
        public static ToolModeManager Instance { get; private set; }

        public enum ToolMode
        {
            Hand,
            FeatherToy
        }

        [Header("State")]
        [SerializeField] private ToolMode _currentMode = ToolMode.Hand;

        [Header("UI References")]
        [Tooltip("Button that toggles the tool mode")]
        [SerializeField] private Button toggleButton;
        [Tooltip("Image on the button to swap icons")]
        [SerializeField] private Image buttonIcon;

        [Header("Icons")]
        [SerializeField] private Sprite handIcon;
        [SerializeField] private Sprite featherIcon;

        public ToolMode CurrentMode => _currentMode;
        public bool IsHandMode => _currentMode == ToolMode.Hand;
        public bool IsFeatherMode => _currentMode == ToolMode.FeatherToy;

        /// <summary>
        /// Fired when the tool mode changes. Passes the new mode.
        /// </summary>
        public event System.Action<ToolMode> OnModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleMode);

            UpdateIcon();
        }

        /// <summary>
        /// Toggle between Hand and FeatherToy modes.
        /// </summary>
        public void ToggleMode()
        {
            _currentMode = _currentMode == ToolMode.Hand ? ToolMode.FeatherToy : ToolMode.Hand;
            UpdateIcon();
            OnModeChanged?.Invoke(_currentMode);
            Debug.Log($"[ToolMode] Switched to: {_currentMode}");
        }

        /// <summary>
        /// Set a specific mode.
        /// </summary>
        public void SetMode(ToolMode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;
            UpdateIcon();
            OnModeChanged?.Invoke(_currentMode);
        }

        private void UpdateIcon()
        {
            if (buttonIcon == null) return;

            buttonIcon.sprite = _currentMode == ToolMode.Hand ? handIcon : featherIcon;
        }
    }
}
