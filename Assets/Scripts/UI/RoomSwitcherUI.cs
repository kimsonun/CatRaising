using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Room selection panel. Shows all rooms with current/locked/unlocked states.
    /// Switching rooms triggers a black fade transition.
    /// 
    /// SETUP:
    /// 1. Create a Panel "RoomSelectionPanel" under Canvas
    /// 2. Add a ScrollView or vertical layout for room buttons
    /// 3. Create a room button prefab (RoomSlot) with: room name text, status text, Button
    /// 4. Add a close button and a HUD button to call Open()
    /// </summary>
    public class RoomSwitcherUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomSlotPrefab; // Prefab with Button, 2x TMP_Text
        [SerializeField] private TextMeshProUGUI roomTextUI;

        private List<GameObject> _roomSlots = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); Close(); });
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        public void Open()
        {
            if (selectionPanel != null) selectionPanel.SetActive(true);
            RefreshRoomList();
        }

        public void Close()
        {
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        private void RefreshRoomList()
        {
            // Clear existing
            foreach (var slot in _roomSlots)
                if (slot != null) Destroy(slot);
            _roomSlots.Clear();

            if (RoomManager.Instance == null || roomListContent == null || roomSlotPrefab == null) return;

            var allRooms = RoomManager.Instance.AllRooms;
            string currentId = RoomManager.Instance.CurrentRoomId;

            foreach (var room in allRooms)
            {
                bool isCurrent = room.roomId == currentId;
                bool isUnlocked = RoomManager.Instance.IsRoomUnlocked(room.roomId);

                var slotObj = Instantiate(roomSlotPrefab, roomListContent);
                _roomSlots.Add(slotObj);
                slotObj.transform.GetChild(1).GetComponent<Image>().sprite = room.backgroundSprite; // Optional: show room image

                var texts = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 1) texts[0].text = room.roomName;

                if (texts.Length >= 2)
                {
                    if (isCurrent)
                        texts[1].text = "Current";
                    else if (isUnlocked)
                        texts[1].text = "Go";
                    else
                        texts[1].text = $"{room.unlockCost} ";
                }

                var btn = slotObj.GetComponent<Button>();
                if (btn != null)
                {
                    // Disable if current room or locked
                    btn.interactable = !isCurrent && isUnlocked;

                    if (!isCurrent && isUnlocked)
                    {
                        string capturedId = room.roomId;
                        btn.onClick.AddListener(() => OnRoomSelected(capturedId));
                    }
                }

                // Dim locked rooms
                var cg = slotObj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    if (isCurrent) cg.alpha = 0.7f;
                    else if (!isUnlocked) cg.alpha = 0.4f;
                    else cg.alpha = 1f;
                }
            }
        }

        private void OnRoomSelected(string roomId)
        {
            Close();

            // Fade transition
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeAndExecute(() =>
                {
                    if (RoomManager.Instance != null)
                        RoomManager.Instance.SwitchRoom(roomId);
                    roomTextUI.text = RoomManager.Instance.CurrentRoom.roomName;
                });
            }
            else
            {
                // No fader — switch immediately
                if (RoomManager.Instance != null)
                    RoomManager.Instance.SwitchRoom(roomId);
            }
        }
    }
}
