using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.UI
{
    public class UIModalController
    {
        private readonly GameObject _uiRoot;
        private readonly GameObject _mask;
        private readonly List<UIWindow> _modals = new(100);

        public UIModalController(GameObject uiRoot, GameObject mask)
        {
            _uiRoot = uiRoot;
            _mask = mask;
        }

        public void ShowMask(UIWindow ui)
        {
            if (_uiRoot == null || _mask == null || ui.IsModal == false)
            {
                return;
            }

            _mask.SetActive(true);
            _mask.transform.SetParent(ui.Transform.parent, false);

            // 重置 Mask 布局，防止父节点不同导致的变形
            var rect = _mask.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }

            var index = ui.Transform.GetSiblingIndex();
            _mask.transform.SetSiblingIndex(Mathf.Max(0, index));

            if (ui.ClickMaskClose)
            {
                _mask.RemoveAllClick();
                _mask.AddClick(_ => ui.CloseAction?.Invoke());
            }

            if (!_modals.Contains(ui))
            {
                _modals.Add(ui);
            }
        }

        public void HideMask(UIWindow ui)
        {
            if (_uiRoot == null || _mask == null || ui.IsModal == false)
            {
                return;
            }

            if (!_modals.Remove(ui))
            {
                return;
            }

            if (_modals.Count > 0)
            {
                ShowMask(_modals[^1]);
            }
            else
            {
                _mask.SetActive(false);
                _mask.transform.SetParent(_uiRoot.transform, false);
            }
        }
    }
}