using UnityEngine;

namespace UniSimple.UI
{
    public abstract class UIParam
    {
    }

    public abstract class UIWindow<TView, TParam> : UIWindow where TView : MonoBehaviour where TParam : UIParam
    {
        protected TView View { get; private set; }

        public override void OnOpen(UIParam param)
        {
            OnParam(param as TParam);
        }

        protected abstract void OnParam(TParam param);

        internal override void InternalCreate()
        {
            base.InternalCreate();
            View = GameObject.GetComponent<TView>();
            if (View == null)
            {
                Debug.LogError($"{GameObject.name} missing component {typeof(TView).Name}.");
            }
        }
    }
}