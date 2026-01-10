using System.Threading;
using UnityEngine;

namespace UniSimple.UI
{
    /// <summary>
    /// UI 组件基类
    /// </summary>
    public abstract class UIComponent : UIBase
    {
        public GameObject Owner { set; get; }
        public RectTransform RectTransform { get; internal set; }

        public CancellationTokenRegistration Registration { set; get; }

        public bool Visible
        {
            get => GameObject.activeSelf;
            set
            {
                if (GameObject.activeSelf == value)
                    return;

                GameObject.SetActive(value);
            }
        }

        public virtual void OnGet()
        {
        }

        public virtual void OnRecycle()
        {
        }

        internal override void CreateInternal()
        {
            base.CreateInternal();
            RectTransform = (RectTransform)Transform;
        }

        internal override void DestroyInternal()
        {
            base.DestroyInternal();
            Registration.Dispose();
        }
    }
}