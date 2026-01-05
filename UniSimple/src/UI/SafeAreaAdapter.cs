using UnityEngine;


namespace UniSimple.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdapter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _currentSafeArea;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_currentSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            _currentSafeArea = Screen.safeArea;

            var min = new Vector2(
                _currentSafeArea.x / Screen.width,
                _currentSafeArea.y / Screen.height
            );

            var max = new Vector2(
                (_currentSafeArea.x + _currentSafeArea.width) / Screen.width,
                (_currentSafeArea.y + _currentSafeArea.height) / Screen.height
            );

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
        }
    }
}