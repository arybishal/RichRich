using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Interactive controller for the original 3D metropolitan environment.
    /// Manages raycasting against 3D building colliders, perspective camera framing,
    /// and switching between 2D Dashboard mode and immersive 3D City exploration mode.
    /// Optimized for 60 FPS mobile portrait rendering.
    /// </summary>
    public class CityInteractionController : MonoBehaviour
    {
        [Header("Camera & Scene Objects")]
        [SerializeField] private Camera _cityCamera;
        [SerializeField] private GameObject _hudScrollContent;
        [SerializeField] private GameObject _cityViewHUDOverlay;
        [SerializeField] private UnityEngine.UI.Button _returnToDashboardButton;

        [Header("Camera Framing Settings")]
        [SerializeField] private Vector3 _dashboardCamPos = new Vector3(0f, 14f, -16f);
        [SerializeField] private Vector3 _dashboardCamRot = new Vector3(36f, 0f, 0f);
        [SerializeField] private Vector3 _cityExploreCamPos = new Vector3(0f, 10f, -13f);
        [SerializeField] private Vector3 _cityExploreCamRot = new Vector3(32f, 0f, 0f);

        public bool IsInCityExploreMode { get; private set; }

        public event Action<bool> OnCityViewToggled;

        private void Awake()
        {
            if (_cityCamera == null)
            {
                _cityCamera = Camera.main;
            }

            if (_returnToDashboardButton != null)
            {
                _returnToDashboardButton.onClick.AddListener(() => SetCityExploreMode(false));
            }
        }

        private void Update()
        {
            if (IsInCityExploreMode && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                SetCityExploreMode(false);
                return;
            }

            HandleTouchOrClick();
        }

        private void HandleTouchOrClick()
        {
            // Do not raycast if pointer is interacting with UI elements
            if (EventSystem.current != null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    return;
                }
#else
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
#endif
            }

            // Check if any modal is currently covering the screen
            if (ModalManager.Instance != null && ModalManager.Instance.HasActiveModals)
            {
                return;
            }

            bool isTriggered = false;
            Vector3 screenPos = Vector3.zero;

            if (Input.GetMouseButtonDown(0))
            {
                isTriggered = true;
                screenPos = Input.mousePosition;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                isTriggered = true;
                screenPos = Input.GetTouch(0).position;
            }

            if (!isTriggered || _cityCamera == null) return;

            Ray ray = _cityCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var building = hit.collider.GetComponentInParent<CityBuilding>();
                if (building != null)
                {
                    building.OnBuildingTapped();
                }
            }
        }

        public void SetCityExploreMode(bool enableExplore)
        {
            IsInCityExploreMode = enableExplore;

            if (_hudScrollContent != null)
            {
                _hudScrollContent.SetActive(!enableExplore);
            }

            if (_cityViewHUDOverlay != null)
            {
                _cityViewHUDOverlay.SetActive(enableExplore);
            }

            if (_cityCamera != null)
            {
                _cityCamera.transform.position = enableExplore ? _cityExploreCamPos : _dashboardCamPos;
                _cityCamera.transform.rotation = Quaternion.Euler(enableExplore ? _cityExploreCamRot : _dashboardCamRot);
            }

            OnCityViewToggled?.Invoke(IsInCityExploreMode);
        }

        public void ToggleCityExploreMode()
        {
            SetCityExploreMode(!IsInCityExploreMode);
        }
    }
}
