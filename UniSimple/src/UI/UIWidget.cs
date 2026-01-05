using System;
using System.Threading;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

namespace UniSimple.UI
{
    public abstract class UIWidget : UIBase
    {
        public RectTransform RectTransform { get; private set; }

        internal Action RecycleAction { set; get; }

        public bool Visible
        {
            get => GameObject.activeSelf;
            set => GameObject.SetActive(value);
        }

        public GameObject Owner { set; get; }
        public CancellationTokenRegistration Registration { set; get; }

        public virtual void OnRecycle()
        {
        }

        internal override void InternalCreate()
        {
            RectTransform = GameObject.GetComponent<RectTransform>();
        }

        internal void InternalRecycle()
        {
            Owner = null;
            RecycleAction = null;

            // 防止 Owner 销毁时重复触发回收
            Registration.Dispose();
        }

        internal override void InternalDestroy()
        {
            RectTransform = null;
            RecycleAction = null;
            Owner = null;
            Registration.Dispose();
        }
    }

    public static class UIWidgetExtensions
    {
        public static void Bind(this UIWidget widget, GameObject gameObject)
        {
            // 防止重复绑定导致之前的 Registration 没释放
            widget.Registration.Dispose();

            var token = gameObject.GetAsyncDestroyTrigger().CancellationToken;

            // 如果对象已经销毁了，直接回收
            if (token.IsCancellationRequested)
            {
                widget.RecycleAction?.Invoke();
                return;
            }

            var registration = token.Register(() => { widget.RecycleAction?.Invoke(); });

            widget.Registration = registration;
            widget.Owner = gameObject;
        }
    }
}