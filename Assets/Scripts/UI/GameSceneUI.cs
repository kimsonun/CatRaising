using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CatRaising.UI
{
    /// <summary>
    /// In-game UI button to return to the main menu.
    /// 
    /// SETUP:
    /// 1. In your game scene Canvas, add a small button (e.g., top-left corner)
    /// 2. Attach this script to the button OR to an empty "GameUI" object
    /// 3. Assign the button reference
    /// </summary>
    public class GameSceneUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button guideButton;
        
        [SerializeField] private GameObject settingsPanel;
        [Header("Scene")]
        [Tooltip("Name of the menu scene (must be in Build Settings)")]
        [SerializeField] private string menuSceneName = "MenuScene";

        [Header("Confirm Dialog (optional)")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private void Start()
        {
            if (menuButton != null)
                menuButton.onClick.AddListener(() =>
                {
                    Systems.SoundEffectHooks.Instance?.PlayButtonClick();
                    OnMenuButtonPressed();
                });

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() =>
                {
                    Systems.SoundEffectHooks.Instance?.PlayButtonClick();
                    OnOpenSettings();
                });

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(() =>
                {
                    Systems.SoundEffectHooks.Instance?.PlayButtonClick();
                    OnCloseSettings();
                });


            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(() =>
                {
                    Systems.SoundEffectHooks.Instance?.PlayButtonClick();
                    OnConfirmYes();
                });

            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); OnConfirmNo(); });

            if (confirmDialog != null)
                confirmDialog.SetActive(false);
        }

        private void OnMenuButtonPressed()
        {
            // If we have a confirm dialog, show it. Otherwise go directly.
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(true);
            }
            else
            {
                GoToMenu();
            }
        }

        private void OnConfirmYes()
        {
            GoToMenu();
        }

        private void OnConfirmNo()
        {
            if (confirmDialog != null)
                confirmDialog.SetActive(false);
        }

        private void GoToMenu()
        {
            // TODO: Save game before leaving
            Debug.Log("[GameSceneUI] Returning to menu...");
            SceneManager.LoadScene(menuSceneName);
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
    }
}
