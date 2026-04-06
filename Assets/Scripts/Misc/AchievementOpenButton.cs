using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class AchievementOpenButton : MonoBehaviour
{
    [SerializeField] private AchievementUI achievementUI;
    [SerializeField] private Button achievementButton;

    void Start()
    {
        achievementButton.onClick.AddListener(() => achievementUI.OpenList());
    }
}
