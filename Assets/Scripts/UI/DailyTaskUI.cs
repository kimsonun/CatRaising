using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Daily task panel showing 5 tasks with completion status.
    /// 
    /// SETUP:
    /// 1. Create a Panel "DailyTaskPanel" under Canvas
    /// 2. Add 5 task slot GameObjects (each with checkmark Image, name Text, reward Text)
    /// 3. Add progress text and close button
    /// </summary>
    public class DailyTaskUI : MonoBehaviour
    {
        [System.Serializable]
        public class TaskSlotUI
        {
            public DailyTaskType taskType;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI rewardText;
            public Image checkmark;
            public Color completedColor = new Color(0.2f, 0.8f, 0.2f);
            public Color pendingColor = new Color(0.6f, 0.6f, 0.6f);
        }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TaskSlotUI[] taskSlots;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (panel != null) panel.SetActive(false);

            if (DailyTaskManager.Instance != null)
            {
                DailyTaskManager.Instance.OnTaskCompleted += OnTaskCompleted;
                DailyTaskManager.Instance.OnTasksReset += RefreshAll;
            }
        }

        private void OnDestroy()
        {
            if (DailyTaskManager.Instance != null)
            {
                DailyTaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
                DailyTaskManager.Instance.OnTasksReset -= RefreshAll;
            }
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

        private void OnTaskCompleted(DailyTaskType type)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (DailyTaskManager.Instance == null) return;

            foreach (var slot in taskSlots)
            {
                bool done = DailyTaskManager.Instance.IsTaskComplete(slot.taskType);

                if (slot.nameText != null)
                    slot.nameText.text = DailyTaskManager.Instance.GetTaskName(slot.taskType);

                if (slot.rewardText != null)
                    slot.rewardText.text = done ? "✓" : $"+{DailyTaskManager.Instance.GetReward(slot.taskType)}";

                if (slot.checkmark != null)
                    slot.checkmark.color = done ? slot.completedColor : slot.pendingColor;
            }

            if (progressText != null)
            {
                int done = DailyTaskManager.Instance.CompletedCount;
                int total = DailyTaskManager.Instance.TotalTasks;
                progressText.text = $"Daily Tasks: {done}/{total}";
            }
        }
    }
}
