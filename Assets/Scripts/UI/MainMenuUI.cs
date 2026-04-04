using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CatRaising.UI
{
    /// <summary>
    /// Main Menu UI controller. Handles the Start Game and Settings buttons.
    /// 
    /// SETUP:
    /// 1. Create a Canvas in the Menu scene
    /// 2. Add: Title text, Start button, Settings button
    /// 3. Create a SettingsPanel child (see SettingsPanel.cs)
    /// 4. Assign all references in the Inspector
    /// 5. Add this script to the Canvas or an empty "MenuManager" object
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Scene")]
        [Tooltip("Name of the main game scene (must be in Build Settings)")]
        [SerializeField] private string gameSceneName = "SampleScene";

        private void Start()
        {
            // Load saved audio volume
            Core.AudioSettings.LoadSavedVolume();

            if (startButton != null)
                startButton.onClick.AddListener(OnStartGame);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnOpenSettings);

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(OnCloseSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitGame);

            // Hide settings panel at start
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnStartGame()
        {
            Debug.Log("[MainMenu] Starting game...");
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnOpenSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        private void OnCloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnQuitGame()
        {
            Application.Quit();
        }
    }
}
