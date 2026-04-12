using CatRaising;
using CatRaising.UI;
using CatRaising.Systems;
using UnityEngine;
using UnityEngine.UI;

public class RoomOpenButton : MonoBehaviour
{
    [SerializeField] private RoomSwitcherUI roomUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private FurniturePlacementUI furniturePlacementUI;
    [SerializeField] private Button roomButton;

    void Start()
    {
        roomButton.onClick.AddListener(() =>
        {
            SoundEffectHooks.Instance?.PlayButtonClick();
            roomUI.Open();
            shopUI.Close();
            furniturePlacementUI.Close();
        });
    }
}
