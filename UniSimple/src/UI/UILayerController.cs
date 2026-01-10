using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.UI
{
    public enum UILayer
    {
        /// <summary>
        /// 背景层
        /// </summary>
        Background,

        /// <summary>
        /// 抬头显示层
        /// </summary>
        HUD,

        /// <summary>
        /// 普通层
        /// </summary>
        Normal,

        /// <summary>
        /// 弹窗层
        /// </summary>
        Popup,

        /// <summary>
        /// 顶层
        /// </summary>
        Top,

        /// <summary>
        /// 系统层
        /// </summary>
        System,
    }

    /// <summary>
    /// 内部层级控制
    /// </summary>
    internal class UILayerController
    {
        // 每层排序间隔
        private const int ORDER_PER_LAYER = 1000;

        // 每层步进
        private const int ORDER_INCREMENT = 5;

        private readonly GameObject _root;
        private readonly Dictionary<UILayer, List<UIWindow>> _layers;
        private readonly Dictionary<UILayer, GameObject> _layerRoots;
        private readonly Dictionary<UILayer, int> _layerSortingOrders;

        public UILayerController(GameObject root)
        {
            var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
            _root = root;
            _layers = new Dictionary<UILayer, List<UIWindow>>(layers.Length);
            _layerSortingOrders = new Dictionary<UILayer, int>(layers.Length);
            _layerRoots = new Dictionary<UILayer, GameObject>(layers.Length);

            foreach (var layer in layers)
            {
                CreateLayer(layer);
            }
        }

        public void BringToFront(UIWindow ui)
        {
            var layer = ui.WindowSetting.Layer;
            if (!_layers.TryGetValue(layer, out var list))
            {
                list = new List<UIWindow>();
                _layers[layer] = list;
            }

            if (!list.Remove(ui))
            {
                ui.Transform.SetParent(_layerRoots[layer].transform, false);
            }

            list.Add(ui);
            RefreshOrders(layer);
        }

        public void RemoveFromLayer(UIWindow ui)
        {
            var layer = ui.WindowSetting.Layer;
            if (_layers.TryGetValue(layer, out var list))
            {
                if (list.Remove(ui))
                {
                    ui.Transform.SetParent(_root.transform, false);
                    RefreshOrders(layer);
                }
            }
        }

        private void RefreshOrders(UILayer layer)
        {
            if (_layers.TryGetValue(layer, out var list))
            {
                var sortingOrder = _layerSortingOrders[layer];
                for (var i = 0; i < list.Count; i++)
                {
                    var order = ORDER_INCREMENT * i;
                    list[i].Canvas.sortingOrder = sortingOrder + order;
                }
            }
        }

        private void CreateLayer(UILayer layer)
        {
            var sortingOrder = ORDER_PER_LAYER * (int)layer;

            // 创建 GameObject
            var go = new GameObject($"Layer_{layer.ToString()}");
            go.transform.SetParent(_root.transform, false);

            // 添加 Canvas 组件
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            // 设置 RectTransform 属性
            // var rt = go.GetComponent<RectTransform>();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _layerSortingOrders[layer] = sortingOrder;
            _layerRoots[layer] = go;
        }
    }
}