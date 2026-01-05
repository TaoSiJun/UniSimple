using System.Reflection;
using UnityEngine;

namespace UniSimple.UI
{
    /// <summary>
    /// UI 状态
    /// </summary>
    public enum UIState
    {
        None,
        Loading,
        Opening,
        Opened,
        Closing,
        Closed,
    }

    public abstract class UIBase
    {
        public UIState State { get; internal set; } = UIState.None;

        protected int Layer { get; } = LayerMask.NameToLayer("UI");

        private UISettingAttribute _setting;

        public UISettingAttribute Setting
        {
            get
            {
                if (_setting == null)
                    _setting = GetType().GetCustomAttribute<UISettingAttribute>();

                if (_setting == null)
                    Debug.LogError($"{GetType().Name} missing the 'UISetting'");

                return _setting;
            }
        }

        // ---------- 游戏对象 ----------

        public GameObject GameObject { get; internal set; }
        public Transform Transform => GameObject?.transform;

        // ---------- 生命周期 ----------

        public virtual void OnCreate()
        {
        }

        public virtual void OnOpen(UIParam param)
        {
        }

        public virtual void OnClose()
        {
        }

        public virtual void OnDestroy()
        {
        }

        // ---------- 内部方法 ----------

        internal virtual void InternalCreate()
        {
        }

        internal virtual void InternalDestroy()
        {
        }
    }
}