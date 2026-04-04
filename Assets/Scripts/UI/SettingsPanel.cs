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
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeLabel; // Optional: shows "Volume: 80%"

        private void OnEnable()
        {
            // Initialize slider to current volume
            if (volumeSlider != null)
            {
                volumeSlider.value = Core.AudioSettings.MasterVolume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            UpdateLabel();
        }

        private void OnDisable()
        {
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        private void OnVolumeChanged(float value)
        {
            Core.AudioSettings.MasterVolume = value;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (volumeLabel != null)
                volumeLabel.text = $"Volume: {Mathf.RoundToInt(Core.AudioSettings.MasterVolume * 100)}%";
        }
    }
}
