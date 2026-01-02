using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UniSimple.UI
{
    /// <summary>
    /// UI事件监听器
    /// </summary>
    public class UIEventListener : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        #region Events

        public Action<PointerEventData> OnClickCallback;

        public Action<PointerEventData> OnDownCallback;

        public Action<PointerEventData> OnUpCallback;

        public Action<PointerEventData> OnEnterCallback;

        public Action<PointerEventData> OnExitCallback;

        public Action<PointerEventData> OnDragCallback;

        public Action<PointerEventData> OnBeginDragCallback;

        public Action<PointerEventData> OnEndDragCallback;

        #endregion

        public void OnPointerClick(PointerEventData eventData) => OnClickCallback?.Invoke(eventData);

        public void OnPointerDown(PointerEventData eventData) => OnDownCallback?.Invoke(eventData);

        public void OnPointerUp(PointerEventData eventData) => OnUpCallback?.Invoke(eventData);

        public void OnPointerEnter(PointerEventData eventData) => OnEnterCallback?.Invoke(eventData);

        public void OnPointerExit(PointerEventData eventData) => OnExitCallback?.Invoke(eventData);

        public void OnDrag(PointerEventData eventData) => OnDragCallback?.Invoke(eventData);

        public void OnBeginDrag(PointerEventData eventData) => OnBeginDragCallback?.Invoke(eventData);

        public void OnEndDrag(PointerEventData eventData) => OnEndDragCallback?.Invoke(eventData);

        /// <summary>
        /// 静态方法拓展
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static UIEventListener GetOrAdd(GameObject go)
        {
            if (!go.TryGetComponent<UIEventListener>(out var listener))
            {
                listener = go.AddComponent<UIEventListener>();
            }

            return listener;
        }
    }
}