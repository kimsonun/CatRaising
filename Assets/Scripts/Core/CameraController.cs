using UnityEngine;
using CatRaising.Cat;

namespace CatRaising.Core
{
    /// <summary>
    /// Controls the main camera:
    /// - Horizontal swipe to pan the view
    /// - Smooth zoom toward cat during petting
    /// - Tap on empty ground to call the cat
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera cam;
        [SerializeField] private CatAI catAI;

        [Header("Pan Settings")]
        [Tooltip("Min and max X position the camera can pan to")]
        [SerializeField] private float panMinX = -5f;
        [SerializeField] private float panMaxX = 5f;
        [Tooltip("Pixels of drag before it counts as a pan (not a tap)")]
        [SerializeField] private float dragThreshold = 15f;
        [Tooltip("Pan sensitivity multiplier")]
        [SerializeField] private float panSensitivity = 1f;

        [Header("Pet Zoom Settings")]
        [SerializeField] private float petZoomSize = 3f;
        [SerializeField] private float zoomSpeed = 3f;
        [Tooltip("How far above the cat to center when zooming")]
        [SerializeField] private float zoomYOffset = 0.5f;

        // Internal state
        private float _defaultOrthoSize;
        private Vector3 _defaultPosition;
        private float _targetOrthoSize;
        private Vector3 _targetPosition;

        // Pan tracking
        private bool _isTouchActive = false;
        private bool _isPanning = false;
        private Vector2 _touchStartScreen;
        private Vector3 _camStartPos;
        private float _totalDragPixels;

        // Petting zoom
        private bool _isPetZooming = false;
        private Transform _zoomTarget;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            _defaultOrthoSize = cam.orthographicSize;
            _defaultPosition = transform.position;
            _targetOrthoSize = _defaultOrthoSize;
            _targetPosition = _defaultPosition;
        }

        private void Update()
        {
            if (!_isPetZooming)
            {
                HandlePanInput();
            }

            // Smooth zoom/position transitions
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, _targetOrthoSize, zoomSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _targetPosition, zoomSpeed * Time.deltaTime);
        }

        // ─── Pan & Ground Tap ───────────────────────────────────

        private void HandlePanInput()
        {
            // Only pan in Hand mode; in Feather mode, DraggableToy handles empty-space input
            if (ToolModeManager.Instance != null && ToolModeManager.Instance.IsFeatherMode)
                return;

            if (TouchInput.WasPressedThisFrame)
            {
                // Check if touch is on an interactable — if so, don't process
                Collider2D hit = Physics2D.OverlapPoint(TouchInput.WorldPosition);
                if (hit != null) return;

                _isTouchActive = true;
                _isPanning = false;
                _touchStartScreen = TouchInput.Position;
                _camStartPos = transform.position;
                _totalDragPixels = 0f;
            }

            if (TouchInput.IsPressed && _isTouchActive)
            {
                Vector2 currentScreen = TouchInput.Position;
                Vector2 delta = currentScreen - _touchStartScreen;
                _totalDragPixels = delta.magnitude;

                if (_totalDragPixels > dragThreshold)
                {
                    _isPanning = true;

                    // Convert screen pixel delta to world units
                    float worldDeltaX = -delta.x * (cam.orthographicSize * 2f / Screen.height) * panSensitivity;

                    float newX = Mathf.Clamp(_camStartPos.x + worldDeltaX, panMinX, panMaxX);
                    _targetPosition = new Vector3(newX, _defaultPosition.y, _defaultPosition.z);
                }
            }

            if (TouchInput.WasReleasedThisFrame && _isTouchActive)
            {
                _isTouchActive = false;

                // Short tap on empty ground → call the cat
                if (!_isPanning && _totalDragPixels < dragThreshold)
                {
                    OnGroundTap();
                }

                _isPanning = false;
            }
        }

        /// <summary>
        /// Called when the player taps on empty ground (no collider hit).
        /// Tells the cat to walk to that world position.
        /// </summary>
        private void OnGroundTap()
        {
            Vector2 worldPos = TouchInput.WorldPosition;

            if (catAI != null)
            {
                catAI.WalkToPosition(worldPos);
                Debug.Log($"[CameraController] Ground tap → calling cat to {worldPos}");
            }
        }

        // ─── Pet Zoom ───────────────────────────────────────────

        /// <summary>
        /// Start zooming the camera toward the cat (called by CatInteraction when petting starts).
        /// </summary>
        public void StartPetZoom(Transform target)
        {
            _isPetZooming = true;
            _zoomTarget = target;
            _targetOrthoSize = petZoomSize;

            if (_zoomTarget != null)
            {
                _targetPosition = new Vector3(
                    _zoomTarget.position.x,
                    _zoomTarget.position.y + zoomYOffset,
                    _defaultPosition.z
                );
            }
        }

        /// <summary>
        /// Stop zooming and return camera to default (called by CatInteraction when petting ends).
        /// </summary>
        public void StopPetZoom()
        {
            _isPetZooming = false;
            _zoomTarget = null;
            _targetOrthoSize = _defaultOrthoSize;
            _targetPosition = new Vector3(
                Mathf.Clamp(transform.position.x, panMinX, panMaxX),
                _defaultPosition.y,
                _defaultPosition.z
            );
        }
    }
}
