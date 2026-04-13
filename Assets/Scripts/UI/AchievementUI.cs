using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Achievement popup + list with Claim buttons.
    /// Manages a red-dot notification on the HUD button.
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        [Header("Popup Notification")]
        [SerializeField] private GameObject popup;
        [SerializeField] private TextMeshProUGUI popupTitle;
        [SerializeField] private TextMeshProUGUI popupDescription;
        [SerializeField] private TextMeshProUGUI popupReward;
        [SerializeField] private TextMeshProUGUI bondRewardText;
        [SerializeField] private Button popupCloseButton;

        [Header("Achievement List Panel")]
        [SerializeField] private GameObject listPanel;
        [SerializeField] private Button listCloseButton;
        [SerializeField] private Transform listContent;
        [SerializeField] private GameObject listItemPrefab; // Needs: 3x TMP_Text, 1x Button, optional CanvasGroup
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("HUD Notification")]
        [Tooltip("Red dot on the HUD Achievements button")]
        [SerializeField] private GameObject hudRedDot;

        private void Start()
        {
            if (popup != null) popup.SetActive(false);
            if (listPanel != null) listPanel.SetActive(false);

            if (popupCloseButton != null) popupCloseButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); HidePopup(); });
            if (listCloseButton != null) listCloseButton.onClick.AddListener(() => {Systems.SoundEffectHooks.Instance?.PlayButtonClick() ; CloseList(); });

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked += ShowPopup;
                AchievementManager.Instance.OnClaimStateChanged += UpdateRedDot;
            }

            UpdateRedDot();
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= ShowPopup;
                AchievementManager.Instance.OnClaimStateChanged -= UpdateRedDot;
            }
        }

        private void ShowPopup(AchievementManager.AchievementDef def)
        {
            if (popup == null) return;

            if (popupTitle != null) popupTitle.text = $"{def.name}";
            if (popupDescription != null) popupDescription.text = def.description;

            string reward = $"+{def.coinReward} ";
            string bondReward = def.bondReward > 0 ? $"  +{def.bondReward} bond" : "";
            if (popupReward != null) popupReward.text = reward;
            if (bondReward != null) bondRewardText.text = bondReward;
            if (def.bondReward == 0) bondRewardText.gameObject.SetActive(false);
             else bondRewardText.gameObject.SetActive(true);

            popup.SetActive(true);
        }

        private void HidePopup()
        {
            if (popup != null) popup.SetActive(false);
        }

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
            if (listContent != null)
            {
                foreach (Transform child in listContent)
                    Destroy(child.gameObject);
            }

            var allDefs = AchievementManager.GetAllDefinitions();
            int unlocked = 0;
            int i = 0;
            foreach (var def in allDefs)
            {
                bool isUnlocked = AchievementManager.Instance != null &&
                                  AchievementManager.Instance.IsUnlocked(def.id);
                bool isClaimed = AchievementManager.Instance != null &&
                                AchievementManager.Instance.IsClaimed(def.id);

                if (isUnlocked) unlocked++;

                if (listItemPrefab != null && listContent != null)
                {
                    var itemObj = Instantiate(listItemPrefab, listContent);
                    var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();

                    if (texts.Length >= 1) texts[0].text = isUnlocked ? def.name : allDefs[i].name;
                    if (texts.Length >= 2) texts[1].text = isUnlocked ? def.description : allDefs[i].description;
                    i++;
                    if (texts.Length >= 3)
                    {
                        if (isClaimed) texts[2].text = "Claimed";
                        //if (isUnlocked) texts[3].text = $"+{def.coinReward} ";
                        texts[3].text = $"+{def.coinReward} ";
                    }

                    // Claim button (4th child button, if present)
                    var btn = itemObj.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        if (isUnlocked && !isClaimed)
                        {
                            btn.interactable = true;
                            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                            if (btnText != null) btnText.text = "Claim";

                            var capturedId = def.id;
                            btn.onClick.AddListener(() =>
                            {
                                if (AchievementManager.Instance != null)
                                    AchievementManager.Instance.ClaimAchievement(capturedId);
                                RefreshList();
                            });
                        }
                        else
                        {
                            btn.interactable = false;
                            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                            if (btnText != null)
                                btnText.text = isClaimed ? "Claimed" : "Locked";
                        }
                    }

                    var cg = itemObj.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = isUnlocked ? 1f : 0.8f;
                }
            }

            if (progressText != null)
                progressText.text = $"{unlocked}/{allDefs.Length}";

            UpdateRedDot();
        }

        private void UpdateRedDot()
        {
            if (hudRedDot == null) return;
            bool hasUnclaimed = AchievementManager.Instance != null &&
                               AchievementManager.Instance.HasUnclaimedAchievements;
            hudRedDot.SetActive(hasUnclaimed);
        }
    }
}
