using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CatRaising.UI
{
    /// <summary>
    /// Individual item card in the shop grid.
    /// Attach to a prefab with Image, Text, and Button components.
    /// </summary>
    public class ShopItemSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private CanvasGroup canvasGroup;

        private ShopUI.ShopItem _item;
        private System.Action<ShopUI.ShopItem> _onClick;

        public void Setup(ShopUI.ShopItem item, bool owned, bool canAfford, System.Action<ShopUI.ShopItem> onClick)
        {
            _item = item;
            _onClick = onClick;

            if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;
            if (nameText != null) nameText.text = item.itemName;
            if (costText != null) costText.text = $"{item.cost}";

            if (ownedBadge != null) ownedBadge.SetActive(owned);

            if (buyButton != null)
            {
                buyButton.onClick.AddListener(OnClick);
                buyButton.interactable = !owned && canAfford;
            }

            if (costText != null && owned)
                costText.text = "Owned";

            // Dim if can't afford
            if (canvasGroup != null)
                canvasGroup.alpha = (owned || canAfford) ? 1f : 0.5f;
        }

        private void OnClick()
        {
            _onClick?.Invoke(_item);
        }
    }
}
