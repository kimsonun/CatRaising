using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class ShopOpenButton : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private Button shopButton;

    void Start()
    {
        shopButton.onClick.AddListener(() => shopUI.Open());
    }
}
