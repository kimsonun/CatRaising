using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Achievement popup notification + achievement list panel.
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        [Header("Popup Notification")]
        [SerializeField] private GameObject popup;
        [SerializeField] private TextMeshProUGUI popupTitle;
        [SerializeField] private TextMeshProUGUI popupDescription;
        [SerializeField] private TextMeshProUGUI popupReward;
        [SerializeField] private Button popupCloseButton;
        [SerializeField] private float popupAutoHideTime = 4f;

        [Header("Achievement List Panel")]
        [SerializeField] private GameObject listPanel;
        [SerializeField] private Button listCloseButton;
        [SerializeField] private Transform listContent; // ScrollView content
        [SerializeField] private GameObject listItemPrefab; // Prefab with name, desc, status
        [SerializeField] private TextMeshProUGUI progressText;

        private void Start()
        {
            if (popup != null) popup.SetActive(false);
            if (listPanel != null) listPanel.SetActive(false);

            if (popupCloseButton != null) popupCloseButton.onClick.AddListener(HidePopup);
            if (listCloseButton != null) listCloseButton.onClick.AddListener(CloseList);

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += ShowPopup;
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= ShowPopup;
        }

        /// <summary>
        /// Show unlock popup for a newly earned achievement.
        /// </summary>
        private void ShowPopup(AchievementManager.AchievementDef def)
        {
            if (popup == null) return;

            if (popupTitle != null) popupTitle.text = $"🏆 {def.name}";
            if (popupDescription != null) popupDescription.text = def.description;

            string reward = $"+{def.coinReward}";
            if (def.bondReward > 0) reward += $"  +{def.bondReward} bond";
            if (popupReward != null) popupReward.text = reward;

            popup.SetActive(true);
            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), popupAutoHideTime);
        }

        private void HidePopup()
        {
            if (popup != null) popup.SetActive(false);
        }

        /// <summary>
        /// Open the full achievement list panel.
        /// </summary>
        public void OpenList()
        {
            if (listPanel != null) listPanel.SetActive(true);
            RefreshList();
        }

        public void CloseList()
        {
            if (listPanel != null) listPanel.SetActive(false);
        }

        private void RefreshList()
        {
            // Clear existing
            if (listContent != null)
            {
                foreach (Transform child in listContent)
                    Destroy(child.gameObject);
            }

            var allDefs = AchievementManager.GetAllDefinitions();
            int unlocked = 0;

            foreach (var def in allDefs)
            {
                bool isUnlocked = AchievementManager.Instance != null &&
                                  AchievementManager.Instance.IsUnlocked(def.id);

                if (isUnlocked) unlocked++;

                if (listItemPrefab != null && listContent != null)
                {
                    var itemObj = Instantiate(listItemPrefab, listContent);
                    var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();

                    // Expect 3 text components: name, description, reward/status
                    if (texts.Length >= 1) texts[0].text = isUnlocked ? def.name : "???";
                    if (texts.Length >= 2) texts[1].text = isUnlocked ? def.description : "Locked";
                    if (texts.Length >= 3)
                        texts[2].text = isUnlocked ? "✅ Unlocked" : $"{def.coinReward}";

                    // Dim locked achievements
                    var cg = itemObj.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = isUnlocked ? 1f : 0.5f;
                }
            }

            if (progressText != null)
                progressText.text = $"{unlocked}/{allDefs.Length}";
        }
    }
}
