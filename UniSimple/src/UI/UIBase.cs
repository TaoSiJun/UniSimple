
using UnityEngine;

namespace UniSimple.UI
{
    /// <summary>
    /// 传递参数基类
    /// </summary>
    public abstract class UIParam
    {
    }

    public abstract class UIBase
    {
        protected const int LAYER = 5; // Unity默认UI层级
        public GameObject GameObject { get; internal set; }
        public Transform Transform => GameObject?.transform;

        public virtual void OnCreate()
        {
        }

        public virtual void OnDestroy()
        {
        }

        internal virtual void CreateInternal()
        {
        }

        internal virtual void DestroyInternal()
        {
        }
    }
}