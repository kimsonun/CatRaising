using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;

namespace CatRaising.UI
{
    /// <summary>
    /// Left/right arrows to cycle between unlocked rooms.
    /// Shows current room name.
    /// </summary>
    public class RoomSwitcherUI : MonoBehaviour
    {
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private TextMeshProUGUI roomNameText;

        private void Start()
        {
            if (leftButton != null) leftButton.onClick.AddListener(OnLeft);
            if (rightButton != null) rightButton.onClick.AddListener(OnRight);

            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomChanged += OnRoomChanged;

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomChanged -= OnRoomChanged;
        }

        private void OnLeft()
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.PreviousRoom();
        }

        private void OnRight()
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.NextRoom();
        }

        private void OnRoomChanged(RoomManager.RoomConfig room)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (RoomManager.Instance == null) return;
            var room = RoomManager.Instance.CurrentRoom;
            if (roomNameText != null && room != null)
                roomNameText.text = room.roomName;
        }
    }
}
