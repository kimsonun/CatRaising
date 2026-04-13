using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Daily task panel with Claim buttons.
    /// Also manages a red-dot notification on the HUD button.
    /// 
    /// SETUP:
    /// 1. Each task slot needs: nameText, rewardText, claimButton, checkmark
    /// 2. Assign the hudRedDot image (small red circle on the HUD "Tasks" button)
    /// </summary>
    public class DailyTaskUI : MonoBehaviour
    {
        [System.Serializable]
        public class TaskSlotUI
        {
            public DailyTaskType taskType;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI rewardText;
            public Button claimButton;
            public TextMeshProUGUI claimButtonText;
            public Image checkmark;
            public Color completedColor = new Color(0.2f, 0.8f, 0.2f);
            public Color pendingColor = new Color(0.6f, 0.6f, 0.6f);
            public Color claimedColor = new Color(0.9f, 0.75f, 0.1f);
        }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TaskSlotUI[] taskSlots;

        [Header("HUD Notification")]
        [Tooltip("Red dot image on the HUD Tasks button — shown when there are unclaimed tasks")]
        [SerializeField] private GameObject hudRedDot;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); Close(); });

            if (panel != null) panel.SetActive(false);

            // Wire claim buttons
            foreach (var slot in taskSlots)
            {
                if (slot.claimButton != null)
                {
                    var capturedType = slot.taskType;
                    slot.claimButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); OnClaimClicked(capturedType); });
                }
            }

            if (DailyTaskManager.Instance != null)
            {
                DailyTaskManager.Instance.OnTaskCompleted += _ => RefreshAll();
                DailyTaskManager.Instance.OnTasksReset += RefreshAll;
                DailyTaskManager.Instance.OnClaimStateChanged += UpdateRedDot;
            }

            UpdateRedDot();
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            RefreshAll();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        private void OnClaimClicked(DailyTaskType taskType)
        {
            if (DailyTaskManager.Instance == null) return;
            DailyTaskManager.Instance.ClaimTask(taskType);
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (DailyTaskManager.Instance == null) return;

            foreach (var slot in taskSlots)
            {
                bool done = DailyTaskManager.Instance.IsTaskComplete(slot.taskType);
                bool claimed = DailyTaskManager.Instance.IsTaskClaimed(slot.taskType);

                if (slot.nameText != null)
                    slot.nameText.text = DailyTaskManager.Instance.GetTaskName(slot.taskType);

                if (slot.rewardText != null)
                    slot.rewardText.text = $"+{DailyTaskManager.Instance.GetReward(slot.taskType)} ";

                // Claim button state
                if (slot.claimButton != null)
                {
                    if (claimed)
                    {
                        slot.claimButton.interactable = false;
                        if (slot.claimButtonText != null) slot.claimButtonText.text = "Claimed";
                    }
                    else if (done)
                    {
                        slot.claimButton.interactable = true;
                        if (slot.claimButtonText != null) slot.claimButtonText.text = "Claim";
                    }
                    else
                    {
                        slot.claimButton.interactable = false;
                        if (slot.claimButtonText != null) slot.claimButtonText.text = "";
                    }
                }

                // Checkmark color
                if (slot.checkmark != null)
                {
                    if (claimed) slot.checkmark.color = slot.claimedColor;
                    else if (done) slot.checkmark.color = slot.completedColor;
                    else slot.checkmark.color = slot.pendingColor;
                }
            }

            if (progressText != null)
            {
                int done = DailyTaskManager.Instance.CompletedCount;
                int total = DailyTaskManager.Instance.TotalTasks;
                progressText.text = $"Daily Tasks: {done}/{total}";
            }

            UpdateRedDot();
        }

        private void UpdateRedDot()
        {
            if (hudRedDot == null) return;
            bool hasUnclaimed = DailyTaskManager.Instance != null &&
                               DailyTaskManager.Instance.HasUnclaimedTasks;
            hudRedDot.SetActive(hasUnclaimed);
        }
    }
}
