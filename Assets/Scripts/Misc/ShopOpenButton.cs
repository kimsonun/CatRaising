using CatRaising.UI;
using CatRaising.Systems;
using UnityEngine;
using UnityEngine.UI;

public class ShopOpenButton : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private Button shopButton;

    void Start()
    {
        shopButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            TutorialHints.Instance?.OnActionPerformed("shop");
            shopUI.Open();
        });
    }
}
