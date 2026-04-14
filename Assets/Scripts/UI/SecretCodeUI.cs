using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Secret keycode input system. Player enters a code in a hidden input field.
    /// When the correct code is entered, a special author message panel appears.
    ///
    /// SETUP:
    /// 1. In your Settings Panel (or any panel), add a small, subtle TMP_InputField
    ///    (e.g., at the bottom with placeholder "Enter code...").
    /// 2. Create a "SecretMessagePanel" overlay with:
    ///    - A background image (can be semi-transparent or decorative)
    ///    - A title TMP text (e.g., "Secret Message")
    ///    - A body TMP text (the author's message)
    ///    - A close button
    /// 3. Attach this script and assign all references in the Inspector.
    /// 4. Set the secret code and author message in the Inspector fields.
    /// </summary>
    public class SecretCodeUI : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("The input field where the player types the secret code")]
        [SerializeField] private TMP_InputField codeInputField;
        [Tooltip("Optional submit button (code also submits on Enter key)")]
        [SerializeField] private Button submitButton;

        [Header("Secret Message Panel")]
        [SerializeField] private GameObject inputField;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageTitleText;
        [SerializeField] private TextMeshProUGUI messageBodyText;
        [SerializeField] private Button closeMessageButton;
        [SerializeField] private Image messageBackground;

        [Header("Configuration")]
        [Tooltip("The secret code (case-insensitive)")]
        [SerializeField] private string secretCode = "meowmeow";
        [Tooltip("Title shown when code is correct")]
        [SerializeField] private string secretTitle = "🐱 A Message From The Creator";
        [Tooltip("The special message from the author")]
        [SerializeField, TextArea(5, 15)]
        private string secretMessage =
            "Thank you so much for playing my game!\n\n" +
            "This project was made with love, lots of coffee, and many late nights.\n\n" +
            "I hope this little cat brings a smile to your face, " +
            "just like real cats bring joy to our lives.\n\n" +
            "Take good care of your virtual kitty! 💕\n\n" +
            "— The Developer";

        [Header("Wrong Code Feedback")]
        [Tooltip("Text to briefly flash when wrong code is entered")]
        [SerializeField] private string wrongCodeMessage = "Hmm... that's not it. 🤔";
        [SerializeField] private float wrongCodeFlashDuration = 2f;

        [Header("Effects")]
        [Tooltip("Optional particle effect or animation on correct code")]
        [SerializeField] private GameObject celebrationEffect;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool _hasUnlockedSecret = false;
        private Coroutine _wrongCodeCoroutine;

        private void Start()
        {
            // Hide message panel initially
            if (messagePanel != null) messagePanel.SetActive(false);
            if (celebrationEffect != null) celebrationEffect.SetActive(false);

            // Wire up submit
            if (submitButton != null)
                submitButton.onClick.AddListener(TrySubmitCode);

            // Submit on Enter key
            if (codeInputField != null)
                codeInputField.onSubmit.AddListener(_ => TrySubmitCode());

            // Close button
            if (closeMessageButton != null)
                closeMessageButton.onClick.AddListener(() => { 
                    inputField.SetActive(false);
                    SoundEffectHooks.Instance?.StopSecretBGM();
                    Systems.SoundEffectHooks.Instance?.PlayButtonClick(); 
                    CloseMessage(); 
                });
        }

        /// <summary>
        /// Validate the entered code against the secret code.
        /// </summary>
        public void TrySubmitCode()
        {
            if (codeInputField == null) return;

            string entered = codeInputField.text.Trim();

            if (string.IsNullOrEmpty(entered)) return;

            if (string.Equals(entered, secretCode, System.StringComparison.OrdinalIgnoreCase))
            {
                // Correct code!
                OnCodeCorrect();
            }
            else
            {
                // Wrong code
                OnCodeWrong();
            }

            // Clear input
            codeInputField.text = "";
        }

        private void OnCodeCorrect()
        {
            _hasUnlockedSecret = true;

            // Play a special sound
            if (SoundEffectHooks.Instance != null)
                SoundEffectHooks.Instance.PlaySound("secret_unlock");

            SoundEffectHooks.Instance?.StartSecretBGM();

            // Show the secret message panel
            ShowMessage();

            Debug.Log("[SecretCode] Secret code accepted!");
        }

        private void OnCodeWrong()
        {
            // Play button click as feedback
            SoundEffectHooks.Instance?.PlayButtonClick();

            // Flash wrong code message in the input placeholder
            if (_wrongCodeCoroutine != null)
                StopCoroutine(_wrongCodeCoroutine);
            _wrongCodeCoroutine = StartCoroutine(FlashWrongCode());

            Debug.Log("[SecretCode] Wrong code entered.");
        }

        private void ShowMessage()
        {
            // Set text
            if (messageTitleText != null)
                messageTitleText.text = secretTitle;

            if (messageBodyText != null)
                messageBodyText.text = secretMessage;

            // Show panel
            if (messagePanel != null)
                messagePanel.SetActive(true);

            // Show celebration effect
            if (celebrationEffect != null)
                celebrationEffect.SetActive(true);

            // Fade in
            if (panelCanvasGroup != null)
                StartCoroutine(FadePanel(0f, 1f, fadeDuration));
        }

        private void CloseMessage()
        {
            SoundEffectHooks.Instance?.PlayButtonClick();

            if (celebrationEffect != null)
                celebrationEffect.SetActive(false);

            if (panelCanvasGroup != null)
            {
                StartCoroutine(FadePanel(1f, 0f, fadeDuration, () =>
                {
                    if (messagePanel != null) messagePanel.SetActive(false);
                }));
            }
            else
            {
                if (messagePanel != null) messagePanel.SetActive(false);
            }
        }

        // ─── Coroutines ─────────────────────────────────────────

        private System.Collections.IEnumerator FlashWrongCode()
        {
            if (codeInputField == null) yield break;

            // Temporarily show wrong code message as placeholder
            var placeholder = codeInputField.placeholder as TextMeshProUGUI;
            string originalPlaceholder = placeholder != null ? placeholder.text : "";

            if (placeholder != null)
            {
                placeholder.text = wrongCodeMessage;
                placeholder.color = new Color(1f, 0.4f, 0.4f); // Reddish
            }

            yield return new WaitForSeconds(wrongCodeFlashDuration);

            // Restore original placeholder
            if (placeholder != null)
            {
                placeholder.text = originalPlaceholder;
                placeholder.color = new Color(0.7f, 0.7f, 0.7f); // Default gray
            }

            _wrongCodeCoroutine = null;
        }

        private System.Collections.IEnumerator FadePanel(float from, float to, float duration,
            System.Action onComplete = null)
        {
            if (panelCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            panelCanvasGroup.alpha = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            panelCanvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Whether the secret has been unlocked this session.
        /// </summary>
        public bool HasUnlockedSecret => _hasUnlockedSecret;
    }
}
