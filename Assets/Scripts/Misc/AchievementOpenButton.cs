using CatRaising.UI;
using CatRaising.Systems;
using UnityEngine;
using UnityEngine.UI;

public class AchievementOpenButton : MonoBehaviour
{
    [SerializeField] private AchievementUI achievementUI;
    [SerializeField] private Button achievementButton;

    void Start()
    {
        achievementButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            achievementUI.OpenList();
        });
    }
}
