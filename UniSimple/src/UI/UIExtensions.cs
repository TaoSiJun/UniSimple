using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniSimple.UI
{
    /// <summary>
    /// UI扩展⽅法
    /// </summary>
    public static class UIExtensions
    {
        public static void AddClick(this Button btn, Action callback)
        {
            btn.onClick.AddListener(() => callback?.Invoke());
        }

        public static void RemoveAllClick(this Button btn)
        {
            btn.onClick.RemoveAllListeners();
        }

        public static void AddClick(this GameObject go, Action<PointerEventData> callback)
        {
            UIEventListener.GetOrAdd(go).OnClickCallback += callback;
        }

        public static void RemoveAllClick(this GameObject go)
        {
            UIEventListener.GetOrAdd(go).OnClickCallback = null;
        }
    }
}