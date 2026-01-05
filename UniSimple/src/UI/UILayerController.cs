using System.Collections.Generic;

namespace UniSimple.UI
{
    public class UILayerController
    {
        private readonly Dictionary<UILayer, List<UIWindow>> _layers = new();

        private void RefreshOrder(UILayer layer)
        {
            if (!_layers.TryGetValue(layer, out var list))
            {
                return;
            }

            // 更新层级
            var order = (int)layer * 1000;
            foreach (var ui in list)
            {
                order += 5; // 每级 +5
                ui.Canvas.sortingOrder = order;
            }

            var hideNext = false;
            for (var i = list.Count - 1; i > -1; i--)
            {
                list[i].Visible = hideNext == false;

                // 检测 IsFullScreen
                if (list[i].IsFullScreen)
                {
                    hideNext = true;
                }
            }
        }

        public void AddToLayer(UIWindow ui)
        {
            var layer = ui.Setting.Layer;
            if (!_layers.TryGetValue(layer, out var list))
            {
                list = new List<UIWindow>();
                _layers[layer] = list;
            }

            if (!list.Contains(ui))
            {
                list.Add(ui);
            }

            RefreshOrder(layer);
        }

        public void RemoveFromLayer(UIWindow ui)
        {
            var layer = ui.Setting.Layer;
            if (!_layers.TryGetValue(layer, out var list))
            {
                return;
            }

            list.Remove(ui);
            RefreshOrder(layer);
        }
    }
}