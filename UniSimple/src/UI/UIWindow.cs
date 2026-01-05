using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UniSimple.UI
{
    /// <summary>
    /// UI 层级
    /// </summary>
    public enum UILayer
    {
        Scene, // 场景层 (如主界⾯背景)

        HUD, // 游戏场景HUD

        Normal, // 普通层 (如主界⾯、背包)

        Popup, // 弹窗层 (如确认框、提⽰)

        Top, // 顶层 (如Loading)

        System // 系统层 (如Debug控制台)
    }

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

    public abstract class UIWindow
    {
        /// <summary>
        /// 是否模态
        /// </summary>
        public virtual bool IsModal => false;

        /// <summary>
        /// 点击遮罩关闭 (Modal才生效)
        /// </summary>
        public virtual bool ClickMaskClose => false;

        /// <summary>
        /// 是否进栈
        /// </summary>
        public virtual bool IsStack => true;

        /// <summary>
        /// 关闭时销毁
        /// </summary>
        public virtual bool DestroyWhenClose => false;

        /// <summary>
        /// 是否全屏
        /// </summary>
        public virtual bool IsFullScreen => false;

        private readonly int _layer = LayerMask.NameToLayer("UI");

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

        internal Action CloseAction { set; get; }

        // ---------- UI GameObject ----------

        public UIState State { get; set; } = UIState.None;
        public GameObject GameObject { get; set; }
        public Transform Transform => GameObject?.transform;
        public Canvas Canvas { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }
        public GraphicRaycaster Raycaster { get; private set; }

        private bool _visible;

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                    return;

                Canvas.enabled = value;
                Raycaster.enabled = value;
                _visible = value;
            }
        }

        public bool Interactable
        {
            get => CanvasGroup.interactable && CanvasGroup.blocksRaycasts;
            set
            {
                CanvasGroup.interactable = value;
                CanvasGroup.blocksRaycasts = value;
            }
        }

        #region 生命周期

        public virtual void OnCreate()
        {
        }

        public virtual void OnOpen(UIParam param)
        {
        }

        public virtual void OnClose()
        {
        }

        public virtual void OnResume()
        {
        }

        public virtual void OnPause()
        {
        }

        public virtual void OnDestroy()
        {
        }

        public virtual async UniTask OpenAnimationAsync()
        {
            await UniTask.CompletedTask;
        }

        public virtual async UniTask CloseAnimationAsync()
        {
            await UniTask.CompletedTask;
        }

        #endregion

        #region 内部方法

        // ---------- 内部方法 ----------

        internal virtual void InternalCreate()
        {
            GameObject.name = Setting.Name;
            GameObject.layer = _layer;

            // 处理Canvas
            if (!GameObject.TryGetComponent<Canvas>(out var canvas))
            {
                canvas = GameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
            }

            if (Setting.Layer > UILayer.Scene)
            {
                Canvas.sortingOrder = (int)Setting.Layer;
                Canvas.overrideSorting = true;
            }

            Canvas = canvas;

            // 处理 CanvasGroup
            CanvasGroup = GameObject.TryGetComponent<CanvasGroup>(out var group) ? group : GameObject.AddComponent<CanvasGroup>();

            // 处理交互
            if (!GameObject.TryGetComponent<GraphicRaycaster>(out _))
            {
                Raycaster = GameObject.AddComponent<GraphicRaycaster>();
            }

            OnCreate();
        }

        internal void InternalDestroy()
        {
            OnDestroy();
        }

        #endregion
    }
}