using CatRaising.UI;
using CatRaising.Systems;
using UnityEngine;
using UnityEngine.UI;

public class DecorateButton : MonoBehaviour
{
    [SerializeField] private FurniturePlacementUI furniturePlacementUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RoomSwitcherUI roomSwitcherUI;
    [SerializeField] private Button decorateButton;

    void Start()
    {
        decorateButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            TutorialHints.Instance?.OnActionPerformed("furniture");
            furniturePlacementUI.Open();
            shopUI.Close();
            roomSwitcherUI.Close();
        });
    }
}
