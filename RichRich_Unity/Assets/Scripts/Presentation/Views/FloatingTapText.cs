using UnityEngine;
using UnityEngine.UI;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Visual tap feedback: Floating currency label that drifts upwards and fades out.
    /// Zero external plugin dependencies (pure UnityEngine math).
    /// </summary>
    public class FloatingTapText : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private float _floatDuration = 0.75f;
        [SerializeField] private float _floatDistance = 120f;

        private float _elapsed;
        private Vector3 _startPosition;
        private Color _initialColor;

        public void Initialize(string text)
        {
            if (_label == null) _label = GetComponent<Text>();
            if (_label != null)
            {
                _label.text = text;
                _initialColor = _label.color;
            }

            _startPosition = transform.localPosition;
            // Add slight random horizontal jitter for organic feel
            float jitterX = UnityEngine.Random.Range(-30f, 30f);
            _startPosition.x += jitterX;
            transform.localPosition = _startPosition;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _floatDuration);

            // Ease out cubic
            float ease = 1f - Mathf.Pow(1f - t, 3);
            transform.localPosition = _startPosition + new Vector3(0, ease * _floatDistance, 0);

            if (_label != null)
            {
                Color c = _initialColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                _label.color = c;
            }

            if (_elapsed >= _floatDuration)
            {
                Destroy(gameObject);
            }
        }
    }
}
