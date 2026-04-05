using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class DailyTaskOpen : MonoBehaviour
{
    [SerializeField] private DailyTaskUI dailyTaskUI;
    [SerializeField] private Button dailyTaskButton;

    void Start()
    {
        dailyTaskButton.onClick.AddListener(() => dailyTaskUI.Open());
    }
}
