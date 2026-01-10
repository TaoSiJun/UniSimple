using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniSimple.UI
{
    /// <summary>
    /// UI 窗口基类
    /// </summary>
    public abstract class UIWindow : UIBase
    {
        /// <summary>
        /// 是否模态
        /// </summary>
        public virtual bool IsModal => false;

        /// <summary>
        /// 点击遮罩是否关闭（Modal才生效）
        /// </summary>
        public virtual bool CloseOnClickMask => false;

        /// <summary>
        /// 关闭时是否销毁
        /// </summary>
        public virtual bool DestroyOnClose => false;

        /// <summary>
        /// 是否进栈
        /// </summary>
        public virtual bool IsStack => false;

        // /// <summary>
        // /// 是否全屏
        // /// </summary>
        // public virtual bool IsFullScreen => false;

        // ---------- 窗口组件 ----------
        public Canvas Canvas { get; private set; }
        public CanvasGroup Group { get; private set; }
        public GraphicRaycaster Raycaster { get; private set; }

        internal Action OpenAction { set; get; }
        internal Action CloseAction { set; get; }

        public bool Interactable
        {
            get => Group.interactable && Group.blocksRaycasts;
            set
            {
                Group.interactable = value;
                Group.blocksRaycasts = value;
            }
        }

        public bool Visible
        {
            get => GameObject.activeSelf;
            set
            {
                if (GameObject.activeSelf == value) return;

                GameObject.SetActive(value);
            }
        }

        private UIWindowSettingAttribute _windowSetting;

        public UIWindowSettingAttribute WindowSetting
        {
            get
            {
                if (_windowSetting == null)
                {
                    _windowSetting = GetType().GetCustomAttribute<UIWindowSettingAttribute>();

                    if (_windowSetting == null)
                    {
                        throw new InvalidOperationException($"{GetType().Name} missing UISettingAttribute");
                    }
                }

                return _windowSetting;
            }
        }


        // ---------- 生命周期 ----------

        public virtual void OnOpen(UIParam param)
        {
        }

        public virtual void OnClose()
        {
        }

        public virtual void OnRefresh()
        {
        }

        public virtual void OnOpenedComplete()
        {
        }

        public virtual void OnClosedComplete()
        {
        }

        /// <summary>
        /// 覆盖方法重写打开动画
        /// 动画结束时需调用 OpenImmediate 方法
        /// </summary>
        public virtual void DoOpenAnimation()
        {
            OpenImmediate();
        }

        /// <summary>
        /// 覆盖方法重写关闭动画
        /// 动画结束时需调用 CloseImmediate 方法
        /// </summary>
        public virtual void DoCloseAnimation()
        {
            CloseImmediate();
        }

        // ---------- 内部方法 ----------

        public void OpenImmediate()
        {
            OpenAction?.Invoke();
            OnOpenedComplete();
        }

        public void CloseImmediate()
        {
            CloseAction?.Invoke();
            OnClosedComplete();
        }

        internal override void CreateInternal()
        {
            GameObject.name = WindowSetting.Name;
            GameObject.layer = LAYER;

            // 处理 Canvas 组件
            if (!GameObject.TryGetComponent<Canvas>(out var canvas))
            {
                canvas = GameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
            }

            Canvas = canvas;

            // CanvasGroup
            if (!GameObject.TryGetComponent<CanvasGroup>(out var group))
            {
                group = GameObject.AddComponent<CanvasGroup>();
            }

            Group = group;

            // GraphicRaycaster
            if (!GameObject.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                raycaster = GameObject.AddComponent<GraphicRaycaster>();
            }

            Raycaster = raycaster;
            OnCreate();
        }

        internal override void DestroyInternal()
        {
            OpenAction = null;
            CloseAction = null;
            Object.Destroy(GameObject);
            OnDestroy();
        }
    }
}