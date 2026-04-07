using CatRaising;
using CatRaising.UI;
using UnityEngine;
using UnityEngine.UI;

public class RoomOpenButton : MonoBehaviour
{
    [SerializeField] private RoomSwitcherUI roomUI;
    [SerializeField] private Button roomButton;

    void Start()
    {
        roomButton.onClick.AddListener(() => roomUI.Open());
    }
}
