using System;
using System.Collections;
using UnityEngine;

namespace RichRich.Presentation.Views
{
    public enum BuildingDistrictType
    {
        StockExchange,
        Bank,
        RealEstate,
        Corporation,
        Residential,
        University
    }

    /// <summary>
    /// Interactive 3D building point-of-interest in the metropolitan cityscape.
    /// Handles touch/click detection, bounce animation feedback, and subsystem modal opening.
    /// </summary>
    public class CityBuilding : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string _buildingName = "Commercial District";
        [SerializeField] private BuildingDistrictType _districtType = BuildingDistrictType.StockExchange;
        [SerializeField] private GameObject _targetModalRoot;
        [SerializeField] private Color _accentColor = new Color(0.2f, 0.6f, 1.0f);

        public string BuildingName => _buildingName;
        public BuildingDistrictType DistrictType => _districtType;
        public GameObject TargetModalRoot => _targetModalRoot;

        private Vector3 _originalScale;
        private Coroutine _bounceRoutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void BindTargetModal(GameObject modalRoot, string name, BuildingDistrictType type, Color accent)
        {
            _targetModalRoot = modalRoot;
            _buildingName = name;
            _districtType = type;
            _accentColor = accent;
        }

        public void OnBuildingTapped()
        {
            if (_bounceRoutine != null)
            {
                StopCoroutine(_bounceRoutine);
            }
            _bounceRoutine = StartCoroutine(BounceAnimation());

            Action openAction = () =>
            {
                if (_targetModalRoot != null && ModalManager.Instance != null)
                {
                    var cityController = FindObjectOfType<CityInteractionController>();
                    if (cityController != null && cityController.IsInCityExploreMode)
                    {
                        cityController.SetCityExploreMode(false);
                    }

                    Debug.Log($"[CityBuilding] Point of interest '{_buildingName}' selected. Opening {_targetModalRoot.name}...");
                    ModalManager.Instance.OpenRoot(_targetModalRoot);
                }
            };

            var avatar = FindObjectOfType<PlayerAvatarController>();
            if (avatar != null)
            {
                avatar.WalkToBuilding(this, openAction);
            }
            else
            {
                openAction();
            }
        }

        private IEnumerator BounceAnimation()
        {
            Vector3 targetPunch = new Vector3(_originalScale.x * 1.08f, _originalScale.y * 0.94f, _originalScale.z * 1.08f);
            float elapsed = 0f;
            float duration = 0.12f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(_originalScale, targetPunch, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(targetPunch, _originalScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = _originalScale;
            _bounceRoutine = null;
        }
    }
}
