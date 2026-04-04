using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CatRaising.Core
{
    /// <summary>
    /// Unified input helper that works with the New Input System for both
    /// mobile touch and editor mouse input via the Pointer abstraction.
    /// 
    /// Usage:
    ///   if (TouchInput.WasPressedThisFrame) { ... }
    ///   if (TouchInput.IsPressed) { ... }
    ///   if (TouchInput.IsOverUI) { ... }  // check before world interactions
    ///   Vector2 pos = TouchInput.Position;
    /// </summary>
    public static class TouchInput
    {
        /// <summary>
        /// Whether a press started this frame (touch down or mouse click).
        /// Equivalent to old Input.GetMouseButtonDown(0).
        /// </summary>
        public static bool WasPressedThisFrame
        {
            get
            {
                var pointer = Pointer.current;
                return pointer != null && pointer.press.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Whether the pointer is currently over a UI element (Canvas/EventSystem).
        /// Check this BEFORE processing world-space input to avoid clicks
        /// "going through" UI buttons to the game world.
        /// </summary>
        public static bool IsOverUI
        {
            get
            {
                var eventSystem = EventSystem.current;
                return eventSystem != null && eventSystem.IsPointerOverGameObject();
            }
        }

        /// <summary>
        /// Whether a press is currently being held (touch held or mouse held).
        /// Equivalent to old Input.GetMouseButton(0).
        /// </summary>
        public static bool IsPressed
        {
            get
            {
                var pointer = Pointer.current;
                return pointer != null && pointer.press.isPressed;
            }
        }

        /// <summary>
        /// Whether a press was released this frame (touch up or mouse release).
        /// Equivalent to old Input.GetMouseButtonUp(0).
        /// </summary>
        public static bool WasReleasedThisFrame
        {
            get
            {
                var pointer = Pointer.current;
                return pointer != null && pointer.press.wasReleasedThisFrame;
            }
        }

        /// <summary>
        /// Current pointer screen position (works for both touch and mouse).
        /// Equivalent to old Input.mousePosition.
        /// </summary>
        public static Vector2 Position
        {
            get
            {
                var pointer = Pointer.current;
                return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
            }
        }

        /// <summary>
        /// Get the current pointer position as a world point (2D).
        /// </summary>
        public static Vector2 WorldPosition
        {
            get
            {
                Camera cam = Camera.main;
                if (cam == null) return Vector2.zero;
                return cam.ScreenToWorldPoint(Position);
            }
        }

        /// <summary>
        /// Check if the pointer is over a specific collider this frame.
        /// Uses Physics2D.OverlapPoint.
        /// </summary>
        public static bool IsOverCollider(Collider2D collider)
        {
            if (collider == null) return false;
            Collider2D hit = Physics2D.OverlapPoint(WorldPosition);
            return hit == collider;
        }

        /// <summary>
        /// Check if the pointer is over a specific GameObject this frame.
        /// </summary>
        public static bool IsOverGameObject(GameObject gameObject)
        {
            Collider2D hit = Physics2D.OverlapPoint(WorldPosition);
            return hit != null && hit.gameObject == gameObject;
        }
    }
}
