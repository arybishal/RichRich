using System;
using System.Collections;
using UnityEngine;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Controls the stylized 3D player character avatar in the metropolitan plaza.
    /// Handles idle breathing, walking animation, and responsive navigation towards
    /// target buildings with a safety timeout fallback to ensure modal opening is never blocked.
    /// </summary>
    public class PlayerAvatarController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 6.0f;
        [SerializeField] private float _arrivalDistance = 1.6f;
        [SerializeField] private float _maxMoveTimeout = 0.5f; // Responsive guarantee

        [Header("Character Visual Limbs")]
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private Transform _bodyRoot;

        public bool IsMoving { get; private set; }
        public Vector3 CurrentPosition => transform.position;

        private Coroutine _currentMoveRoutine;
        private float _animTime;

        private void Update()
        {
            AnimateLimbs();
        }

        private void AnimateLimbs()
        {
            if (IsMoving)
            {
                _animTime += Time.deltaTime * 12f;
                float legAngle = Mathf.Sin(_animTime) * 28f;
                float armAngle = Mathf.Cos(_animTime) * 22f;

                if (_leftLeg != null) _leftLeg.localRotation = Quaternion.Euler(legAngle, 0, 0);
                if (_rightLeg != null) _rightLeg.localRotation = Quaternion.Euler(-legAngle, 0, 0);
                if (_leftArm != null) _leftArm.localRotation = Quaternion.Euler(-armAngle, 0, 0);
                if (_rightArm != null) _rightArm.localRotation = Quaternion.Euler(armAngle, 0, 0);
                if (_bodyRoot != null) _bodyRoot.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_animTime * 2f)) * 0.08f, 0);
            }
            else
            {
                // Idle breathing bob
                _animTime += Time.deltaTime * 2.5f;
                float bob = Mathf.Sin(_animTime) * 0.02f;
                if (_bodyRoot != null) _bodyRoot.localPosition = new Vector3(0, bob, 0);
                if (_leftLeg != null) _leftLeg.localRotation = Quaternion.identity;
                if (_rightLeg != null) _rightLeg.localRotation = Quaternion.identity;
                if (_leftArm != null) _leftArm.localRotation = Quaternion.identity;
                if (_rightArm != null) _rightArm.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Moves character towards target building and executes callback on arrival.
        /// Includes strict timeout fallback to ensure subsystem modals always open responsively.
        /// </summary>
        public void WalkToBuilding(CityBuilding building, Action onArrived)
        {
            if (building == null)
            {
                onArrived?.Invoke();
                return;
            }

            if (_currentMoveRoutine != null)
            {
                StopCoroutine(_currentMoveRoutine);
            }

            Vector3 targetPos = building.transform.position;
            targetPos.y = transform.position.y; // Keep on ground level

            _currentMoveRoutine = StartCoroutine(MoveRoutine(targetPos, onArrived));
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, Action onArrived)
        {
            IsMoving = true;
            float elapsed = 0f;

            // Face target immediately
            Vector3 direction = (targetPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            while (elapsed < _maxMoveTimeout)
            {
                elapsed += Time.deltaTime;
                float dist = Vector3.Distance(transform.position, targetPos);

                if (dist <= _arrivalDistance)
                {
                    break;
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                yield return null;
            }

            IsMoving = false;
            _currentMoveRoutine = null;

            // Always execute callback cleanly
            onArrived?.Invoke();
        }
    }
}
