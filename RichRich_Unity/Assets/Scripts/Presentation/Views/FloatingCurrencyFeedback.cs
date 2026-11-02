using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Displays restrained, non-blocking floating currency feedback for income and expenses.
    /// Spawns floating emerald indicators for gains and crimson indicators for costs.
    /// Completely non-blocking: never delays or alters financial transactions.
    /// </summary>
    public class FloatingCurrencyFeedback : MonoBehaviour
    {
        public static FloatingCurrencyFeedback Instance { get; private set; }

        [Header("Anchor & Resources")]
        [SerializeField] private RectTransform _spawnAnchor;
        [SerializeField] private Font _font;

        [Header("Settings")]
        [SerializeField] private bool _enableReducedMotion = false;
        [SerializeField] private float _floatDuration = 1.1f;
        [SerializeField] private float _floatDistance = 75f;

        public bool EnableReducedMotion
        {
            get => _enableReducedMotion;
            set => _enableReducedMotion = value;
        }

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_spawnAnchor == null)
            {
                _spawnAnchor = GetComponent<RectTransform>();
            }

            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        public void ShowIncome(Currency amount, string note = "")
        {
            if (amount.MinorUnits <= 0) return;
            string text = $"+{amount}";
            if (!string.IsNullOrEmpty(note)) text += $" ({note})";
            SpawnIndicator(text, new Color(0.12f, 0.85f, 0.45f, 1f), isIncome: true);
        }

        public void ShowExpense(Currency amount, string note = "")
        {
            if (amount.MinorUnits <= 0) return;
            string text = $"-{amount}";
            if (!string.IsNullOrEmpty(note)) text += $" ({note})";
            SpawnIndicator(text, new Color(0.95f, 0.32f, 0.32f, 1f), isIncome: false);
        }

        private void SpawnIndicator(string message, Color color, bool isIncome)
        {
            if (_spawnAnchor == null) return;

            GameObject item;
            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
                item.SetActive(true);
            }
            else
            {
                item = new GameObject("FloatingCurrencyText", typeof(RectTransform), typeof(Text));
                item.transform.SetParent(_spawnAnchor, false);
                var t = item.GetComponent<Text>();
                t.font = _font;
                t.fontSize = 24;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
            }

            var textComp = item.GetComponent<Text>();
            textComp.text = message;
            textComp.color = color;

            var rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;

            StartCoroutine(AnimateIndicator(item, textComp, rt, color, isIncome));
        }

        private IEnumerator AnimateIndicator(GameObject item, Text text, RectTransform rt, Color baseColor, bool isIncome)
        {
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            float targetY = isIncome ? _floatDistance : -_floatDistance;
            Vector2 targetPos = startPos + new Vector2(UnityEngine.Random.Range(-25f, 25f), targetY);

            if (_enableReducedMotion)
            {
                // Instant brief flash for reduced motion
                yield return new WaitForSeconds(0.4f);
                item.SetActive(false);
                _pool.Enqueue(item);
                yield break;
            }

            while (elapsed < _floatDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _floatDuration;

                // Easing curve
                float easeProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeProgress);

                // Fade alpha out over second half
                float alpha = progress > 0.5f ? Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f) : 1f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                yield return null;
            }

            item.SetActive(false);
            _pool.Enqueue(item);
        }
    }
}
