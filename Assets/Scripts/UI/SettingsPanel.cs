using CatRaising.Systems;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatRaising.UI
{
    /// <summary>
    /// Settings panel with volume control.
    /// 
    /// SETUP:
    /// 1. Create a Panel under the Canvas called "SettingsPanel"
    /// 2. Add a Slider for volume (min 0, max 1, whole numbers OFF)
    /// 3. Optionally add a label showing volume percentage
    /// 4. Add a "Close" or "Back" button
    /// 5. Assign references in the Inspector
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeLabel;
        [SerializeField] private Slider BGMvolumeSlider;
        [SerializeField] private TextMeshProUGUI BGMvolumeLabel; // Optional: shows "Volume: 80%"
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel; // Optional: shows "Volume: 80%"

        private void OnEnable()
        {
            // Initialize slider to current volume
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = Core.AudioSettings.MasterVolume;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (BGMvolumeSlider != null)
            {
                BGMvolumeSlider.value = SoundEffectHooks.Instance.bgmVolume;
                BGMvolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = SoundEffectHooks.Instance.sfxVolume;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            UpdateLabel();
        }

        private void OnDisable()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (BGMvolumeSlider != null)
                BGMvolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        private void OnMasterVolumeChanged(float value)
        {
            Core.AudioSettings.MasterVolume = value;
            UpdateLabel();
        }

        private void OnBGMVolumeChanged(float value)
        {
            SoundEffectHooks.Instance.bgmVolume = value;
            SoundEffectHooks.Instance.bgmSource.volume = value * SoundEffectHooks.Instance.masterVolume;
            UpdateLabel();
        }

        private void OnSFXVolumeChanged(float arg0)
        {
            SoundEffectHooks.Instance.sfxVolume = arg0;
            UpdateLabel();
        }
        private void UpdateLabel()
        {
            if (masterVolumeLabel != null)
                masterVolumeLabel.text = $"Master Volume: {Mathf.RoundToInt(Core.AudioSettings.MasterVolume * 100)}%";
            if (BGMvolumeLabel != null)
                BGMvolumeLabel.text = $"BGM Volume: {Mathf.RoundToInt(SoundEffectHooks.Instance.bgmVolume * 100)}%";
            if (sfxVolumeLabel != null)
                sfxVolumeLabel.text = $"SFX Volume: {Mathf.RoundToInt(SoundEffectHooks.Instance.sfxVolume * 100)}%";
        }
    }
}
