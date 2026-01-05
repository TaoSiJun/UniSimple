using UnityEngine;

namespace UniSimple.UI
{
    /// <summary>
    /// 窗口带视图
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    public abstract class UIWindowWithView<TView> : UIWindow where TView : MonoBehaviour
    {
        protected TView View { get; private set; }

        internal override void InternalCreate()
        {
            base.InternalCreate();
            View = GameObject.GetComponent<TView>();
        }
    }
}