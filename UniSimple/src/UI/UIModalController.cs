using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.UI
{
    /// <summary>
    /// UI 模态控制器
    /// </summary>
    internal class UIModalController
    {
        private readonly GameObject _root;
        private readonly GameObject _mask;
        private readonly List<UIWindow> _modals = new();

        internal Action ClickMaskAction;

        public UIModalController(GameObject root, GameObject mask)
        {
            _root = root;
            _mask = mask;
        }

        public void ShowMask(UIWindow window)
        {
            if (!window.IsModal) return;

            _mask.SetActive(true);
            _mask.transform.SetParent(window.Transform.parent, false);
            var index = window.Transform.GetSiblingIndex();
            // _mask.transform.SetSiblingIndex(Mathf.Max(0, index));
            _mask.transform.SetSiblingIndex(index);

            var rt = (RectTransform)_mask.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.anchoredPosition3D = Vector3.zero;

            if (window.CloseOnClickMask)
            {
                _mask.RemoveAllClick();
                _mask.AddClick(_ => ClickMaskAction?.Invoke());
            }

            if (!_modals.Contains(window))
            {
                _modals.Add(window);
            }
        }

        public void HideMask(UIWindow window)
        {
            if (!window.IsModal) return;

            if (!_modals.Remove(window))
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
                _mask.transform.SetParent(_root.transform, false);
            }
        }
    }
}