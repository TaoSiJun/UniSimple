using UnityEngine;
using UnityEngine.UI;

namespace UniSimple.UI
{
    public partial class UIManager
    {
        private static GameObject Root { set; get; }
        private static GameObject Mask { set; get; }

        public static void CreateRoot(Vector2 resolution)
        {
            var root = new GameObject("UIRoot");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            root.AddComponent<GraphicRaycaster>();
            Object.DontDestroyOnLoad(root);
        }

        public static void CreateModalMask()
        {
            var mask = new GameObject("Modal_Mask");
            mask.transform.SetParent(Root.transform, false);
            var img = mask.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            img.raycastTarget = true;
            var rt = mask.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Object.DontDestroyOnLoad(mask);
        }

        public static void EnsureEventSystem()
        {
        }
    }
}