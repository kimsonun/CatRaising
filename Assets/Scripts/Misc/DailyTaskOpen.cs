using CatRaising.UI;
using CatRaising.Systems;
using UnityEngine;
using UnityEngine.UI;

public class DailyTaskOpen : MonoBehaviour
{
    [SerializeField] private DailyTaskUI dailyTaskUI;
    [SerializeField] private Button dailyTaskButton;

    void Start()
    {
        dailyTaskButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            dailyTaskUI.Open();
        });
    }
}
