using CatRaising.Cat;
using CatRaising.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CatRaising.Systems
{
    /// <summary>
    /// Custom cursor manager. Replaces the default OS cursor with:
    ///   - A triangle cursor with click animation (3 frames)
    ///   - A hand cursor when hovering over the cat or petting it
    ///
    /// SETUP:
    /// 1. Attach to a persistent GameObject in the scene.
    /// 2. Import your 3 triangle cursor sprites as Textures (Texture Type: Cursor)
    ///    and assign them to triangleCursorFrames[0..2].
    /// 3. Import your hand cursor sprite as a Texture and assign to handCursor.
    /// 4. Assign the cat's root GameObject to catObject.
    /// 5. Set hotspot offsets for each cursor type.
    ///
    /// TEXTURE IMPORT SETTINGS:
    ///   - Texture Type: Cursor (or Default with Read/Write enabled)
    ///   - Filter Mode: Point (for pixel art) or Bilinear
    ///   - Max Size: actual cursor size (e.g., 32 or 64)
    /// </summary>
    public class CustomCursorManager : MonoBehaviour
    {
        public static CustomCursorManager Instance { get; private set; }

        [Header("Triangle Cursor (Default)")]
        [Tooltip("3 frames for the click animation: idle, pressing, pressed")]
        [SerializeField] private Texture2D[] triangleCursorFrames;
        [Tooltip("Hotspot offset for the triangle cursor (tip of the arrow)")]
        [SerializeField] private Vector2 triangleHotspot = Vector2.zero;

        [Header("Hand Cursor (Cat Hover)")]
        [Tooltip("Hand/grab cursor texture for hovering over the cat")]
        [SerializeField] private Texture2D handCursor;
        [Tooltip("Hotspot offset for the hand cursor (center of palm)")]
        [SerializeField] private Vector2 handHotspot = new Vector2(8f, 4f);

        [Header("Cat Reference")]
        [Tooltip("The cat GameObject (must have a Collider2D)")]
        [SerializeField] private GameObject catObject;

        [Header("Click Animation")]
        [Tooltip("How long each click animation frame is shown (seconds)")]
        [SerializeField] private float clickFrameDuration = 0.05f;

        private enum CursorState
        {
            Triangle,       // Default triangle cursor
            TriangleClick,  // Playing click animation
            Hand            // Hovering over / petting cat
        }

        private CursorState _currentState = CursorState.Triangle;
        private int _clickFrame = 0;
        private float _clickTimer = 0f;
        private bool _wasPressed = false;
        private CatInteraction _catInteraction;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Add this line
        }

        private void Start()
        {
            // Hide the default OS cursor and use our own
            Cursor.visible = true;

            // Set initial cursor
            SetTriangleCursor(0);

            // Cache cat interaction
            if (catObject != null)
                _catInteraction = catObject.GetComponent<CatInteraction>();
        }

        private void Update()
        {
            bool isOverCat = IsPointerOverCat();
            bool isPetting = IsCatBeingPet();
            bool shouldShowHand = isOverCat || isPetting;

            // Determine desired state
            if (shouldShowHand)
            {
                if (_currentState != CursorState.Hand)
                {
                    _currentState = CursorState.Hand;
                    ApplyHandCursor();
                }
            }
            else
            {
                // Handle click animation
                HandleClickAnimation();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Find the cat in the new scene by tag or name
            catObject = GameObject.FindWithTag("Cat");
            if (catObject != null)
                _catInteraction = catObject.GetComponent<CatInteraction>();
        }
        // ─── Click Animation ────────────────────────────────────

        private void HandleClickAnimation()
        {
            var pointer = Pointer.current;
            bool isPressed = pointer != null && pointer.press.isPressed;
            bool justPressed = pointer != null && pointer.press.wasPressedThisFrame;
            bool justReleased = pointer != null && pointer.press.wasReleasedThisFrame;

            if (justPressed)
            {
                // Start click animation: go to frame 1 (pressing)
                _currentState = CursorState.TriangleClick;
                _clickFrame = 1;
                _clickTimer = 0f;
                SetTriangleCursor(1);
            }
            else if (_currentState == CursorState.TriangleClick)
            {
                _clickTimer += Time.deltaTime;

                if (isPressed)
                {
                    // While held, show frame 2 (pressed) after a short delay
                    if (_clickFrame == 1 && _clickTimer >= clickFrameDuration)
                    {
                        _clickFrame = 2;
                        SetTriangleCursor(2);
                    }
                }
                else if (justReleased || !isPressed)
                {
                    // Released — animate back: frame 1 briefly, then frame 0
                    if (_clickFrame == 2)
                    {
                        _clickFrame = 1;
                        _clickTimer = 0f;
                        SetTriangleCursor(1);
                    }
                    else if (_clickFrame == 1 && _clickTimer >= clickFrameDuration)
                    {
                        // Back to idle
                        _currentState = CursorState.Triangle;
                        _clickFrame = 0;
                        SetTriangleCursor(0);
                    }
                }
            }
            else
            {
                // Idle — ensure frame 0
                if (_currentState != CursorState.Triangle)
                {
                    _currentState = CursorState.Triangle;
                    _clickFrame = 0;
                    SetTriangleCursor(0);
                }
            }
        }

        // ─── Cursor Application ─────────────────────────────────

        private void SetTriangleCursor(int frameIndex)
        {
            if (triangleCursorFrames == null || triangleCursorFrames.Length == 0) return;

            int idx = Mathf.Clamp(frameIndex, 0, triangleCursorFrames.Length - 1);
            if (triangleCursorFrames[idx] != null)
            {
                Cursor.SetCursor(triangleCursorFrames[idx], triangleHotspot, CursorMode.Auto);
            }
        }

        private void ApplyHandCursor()
        {
            if (handCursor != null)
            {
                Cursor.SetCursor(handCursor, handHotspot, CursorMode.Auto);
            }
        }

        // ─── Cat Detection ──────────────────────────────────────

        private bool IsPointerOverCat()
        {
            if (catObject == null) return false;

            // Use Physics2D raycast at pointer position
            Camera cam = Camera.main;
            if (cam == null) return false;

            var pointer = Pointer.current;
            if (pointer == null) return false;

            Vector2 screenPos = pointer.position.ReadValue();
            Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);

            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            return hit != null && hit.gameObject == catObject;
        }

        private bool IsCatBeingPet()
        {
            if (catObject == null) return false;

            // Check if the cat is in the BeingPet state
            var catController = catObject.GetComponent<CatController>();
            return catController != null && catController.CurrentState == CatController.CatState.BeingPet;
        }

        // ─── Public API ─────────────────────────────────────────

        /// <summary>
        /// Force the cursor to a specific cursor type (e.g., from other scripts).
        /// Call ResetCursor() to go back to normal behavior.
        /// </summary>
        public void ForceHandCursor()
        {
            _currentState = CursorState.Hand;
            ApplyHandCursor();
        }

        /// <summary>
        /// Reset cursor back to normal triangle behavior.
        /// </summary>
        public void ResetCursor()
        {
            _currentState = CursorState.Triangle;
            _clickFrame = 0;
            SetTriangleCursor(0);
        }

        private void OnDisable()
        {
            // Restore default OS cursor when disabled
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void OnDestroy()
        {
            // Restore default OS cursor when destroyed
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
