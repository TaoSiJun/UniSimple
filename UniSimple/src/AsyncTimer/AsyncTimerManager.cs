using System;
using System.Collections.Generic;

namespace UniSimple.AsyncTimer
{
    public static class AsyncTimerManager
    {
        // 用于追踪所有活跃的计时器
        private static readonly HashSet<AsyncTimer> ActiveTimers = new();
        private static readonly object Lock = new();

        static AsyncTimerManager()
        {
            // 订阅 Timer 的生命周期事件
            AsyncTimer.OnCreated += OnTimerCreated;
            AsyncTimer.OnRecycled += OnTimerRecycled;
        }

        #region Management Internal

        private static void OnTimerCreated(AsyncTimer timer)
        {
            lock (Lock)
            {
                ActiveTimers.Add(timer);
            }
        }

        private static void OnTimerRecycled(AsyncTimer timer)
        {
            lock (Lock)
            {
                ActiveTimers.Remove(timer);
            }
        }

        /// <summary>
        /// 停止并清理所有运行中的计时器
        /// </summary>
        public static void DisposeAll()
        {
            lock (Lock)
            {
                // 复制一份列表防止在遍历时修改集合报错
                var toDispose = new List<AsyncTimer>(ActiveTimers);
                ActiveTimers.Clear();
                foreach (var timer in toDispose)
                {
                    timer.Dispose();
                }
            }
        }

        /// <summary>
        /// 停止并清理带标签的计时器
        /// </summary>
        /// <param name="tag"></param>
        public static void DisposeByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            lock (Lock)
            {
                // 找出所有匹配 Tag
                var toDispose = new List<AsyncTimer>();
                foreach (var timer in ActiveTimers)
                {
                    if (timer.Tag == tag)
                    {
                        toDispose.Add(timer);
                    }
                }

                ActiveTimers.Clear();
                foreach (var timer in toDispose)
                {
                    timer.Dispose();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 延迟执行一次
        /// </summary>
        public static AsyncTimer Delay(float time, Action onComplete, bool ignoreTimeScale = false)
        {
            var timer = AsyncTimer.Create();
            timer.Start(time, 1, null, onComplete, ignoreTimeScale);
            return timer;
        }

        /// <summary>
        /// 循环执行
        /// </summary>
        /// <param name="interval">间隔时间</param>
        /// <param name="onTick">回调(次数)</param>
        /// <param name="ignoreTimeScale"></param>
        public static AsyncTimer Interval(float interval, Action<int> onTick, bool ignoreTimeScale = false)
        {
            var timer = AsyncTimer.Create();
            timer.Start(interval, -1, onTick, null, ignoreTimeScale);
            return timer;
        }

        /// <summary>
        /// 循环执行 (带完成回调)
        /// </summary>
        /// <param name="interval">间隔时间</param>
        /// <param name="loopCount">回调(次数)</param>
        /// <param name="onTick"></param>
        /// <param name="onComplete"></param>
        /// <param name="ignoreTimeScale"></param>
        public static AsyncTimer Interval(float interval, int loopCount, Action<int> onTick, Action onComplete, bool ignoreTimeScale = false)
        {
            var timer = AsyncTimer.Create();
            timer.Start(interval, loopCount, onTick, onComplete, ignoreTimeScale);
            return timer;
        }

        /// <summary>
        /// 延迟帧数执行
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="onComplete"></param>
        public static AsyncTimer DelayFrame(int frames, Action onComplete)
        {
            var timer = AsyncTimer.Create();
            timer.StartFrame(frames, onComplete);
            return timer;
        }

        /// <summary>
        /// 等待条件满足
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="onComplete"></param>
        public static AsyncTimer WaitUntil(Func<bool> predicate, Action onComplete)
        {
            var timer = AsyncTimer.Create();
            timer.StartWaitUntil(predicate, onComplete);
            return timer;
        }

        #endregion
    }
}