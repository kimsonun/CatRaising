using CatRaising.MiniGame;
using CatRaising.Systems;
using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameButton : MonoBehaviour
{
    [SerializeField] private CatFishingGame catFishingGameUI;
    [SerializeField] private Button miniganeButton;

    void Start()
    {
        miniganeButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            TutorialHints.Instance?.OnActionPerformed("mini_game");
            catFishingGameUI.Open();
        });
    }
}
