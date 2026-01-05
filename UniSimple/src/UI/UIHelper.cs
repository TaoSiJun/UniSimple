using UnityEngine;
using UnityEngine.UI;


namespace UniSimple.UI
{
    public static class UIHelper
    {
        public static GameObject CreateUIRoot(Vector2 resolution)
        {
            var layer = LayerMask.NameToLayer("UI");
            var uiRoot = new GameObject("UI Root");
            uiRoot.layer = layer;

            // 创建Canvas
            var rootCanvas = uiRoot.gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            rootCanvas.worldCamera = Camera.main;
            rootCanvas.sortingOrder = 0;

            // 创建Canvas缩放
            var scaler = uiRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return uiRoot;
        }
    }

}