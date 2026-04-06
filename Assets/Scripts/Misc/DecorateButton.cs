using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class DecorateButton : MonoBehaviour
{
    [SerializeField] private FurniturePlacementUI furniturePlacementUI;
    [SerializeField] private Button decorateButton;

    void Start()
    {
        decorateButton.onClick.AddListener(() => furniturePlacementUI.Open());
    }
}
