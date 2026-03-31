using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;

namespace CatRaising.UI
{
    /// <summary>
    /// First-launch cat naming screen. Shows a cozy introduction and lets the 
    /// player name their cat before gameplay begins.
    /// </summary>
    public class NamingScreenUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Image catPreviewImage;

        [Header("Settings")]
        [SerializeField] private int minNameLength = 1;
        [SerializeField] private int maxNameLength = 20;
        [SerializeField] private string defaultName = "Kitty";

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private CanvasGroup canvasGroup;

        private System.Action<string> _onNameConfirmed;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (errorText != null) errorText.gameObject.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxNameLength;
                nameInputField.onValueChanged.AddListener(OnNameChanged);
            }
        }

        /// <summary>
        /// Show the naming screen with a callback for when the name is confirmed.
        /// </summary>
        public void Show(System.Action<string> onNameConfirmed)
        {
            _onNameConfirmed = onNameConfirmed;

            if (panel != null) panel.SetActive(true);

            // Set default text
            if (titleText != null)
                titleText.text = "A New Friend!";
            if (subtitleText != null)
                subtitleText.text = "What will you name your new companion?";
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.placeholder.GetComponent<TextMeshProUGUI>().text = defaultName;
            }

            // Disable confirm until valid name
            UpdateConfirmButton();

            // Fade in
            if (canvasGroup != null)
                StartCoroutine(FadeIn());

            Debug.Log("[NamingScreenUI] Naming screen shown.");
        }

        /// <summary>
        /// Called when the confirm button is clicked.
        /// </summary>
        private void OnConfirmClicked()
        {
            string name = GetEnteredName();

            if (!ValidateName(name))
                return;

            Debug.Log($"[NamingScreenUI] Cat named: {name}");

            // Hide the panel
            if (panel != null) panel.SetActive(false);

            // Invoke callback
            _onNameConfirmed?.Invoke(name);
        }

        /// <summary>
        /// Called when the input field text changes.
        /// </summary>
        private void OnNameChanged(string text)
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
            UpdateConfirmButton();
        }

        /// <summary>
        /// Get the entered name, falling back to default if empty.
        /// </summary>
        private string GetEnteredName()
        {
            if (nameInputField == null) return defaultName;

            string name = nameInputField.text.Trim();
            return string.IsNullOrEmpty(name) ? defaultName : name;
        }

        /// <summary>
        /// Validate the entered name.
        /// </summary>
        private bool ValidateName(string name)
        {
            if (name.Length < minNameLength)
            {
                ShowError($"Name must be at least {minNameLength} character(s)!");
                return false;
            }

            if (name.Length > maxNameLength)
            {
                ShowError($"Name must be {maxNameLength} characters or less!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Show an error message.
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Enable/disable confirm button based on input validity.
        /// </summary>
        private void UpdateConfirmButton()
        {
            if (confirmButton == null) return;

            // Always enabled — empty input will use default name
            confirmButton.interactable = true;
        }

        /// <summary>
        /// Fade in animation for the panel.
        /// </summary>
        private System.Collections.IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Hide the naming screen.
        /// </summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
