using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple
{
    /// <summary>
    /// 异步计时器
    /// </summary>
    public sealed class AsyncTimer : IDisposable
    {
        private static readonly object Lock = new();
        private static readonly Stack<AsyncTimer> Pool = new();

        private CancellationTokenSource _cts;
        private Action<int> _onTick;
        private Action _onComplete;

        // 标记当前对象是否处于回收状态，防止多次 Dispose
        public bool IsDisposed { get; private set; }

        // 标记当前运行状态
        public bool IsRunning { get; private set; }

        private AsyncTimer()
        {
        }

        #region Static functions

        public static AsyncTimer Create()
        {
            lock (Lock)
            {
                if (Pool.Count > 0)
                {
                    var timer = Pool.Pop();
                    timer.IsDisposed = false; // 复活
                    return timer;
                }
            }

            return new AsyncTimer();
        }

        private static void Recycle(AsyncTimer timer)
        {
            lock (Lock)
            {
                // 防止重复入池
                if (timer.IsDisposed)
                    return;

                timer.IsDisposed = true;
                timer.Reset();
                Pool.Push(timer);
            }
        }

        #endregion

        /// <summary>
        /// 开启计时器
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="loopCount">重复次数 (-1 为无限循环)</param>
        /// <param name="onTick">每次间隔触发的回调 (参数：当前已执行的次数，从 1 开始)</param>
        /// <param name="onComplete">所有次数完成后触发的回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void Start(float interval, int loopCount, Action<int> onTick = null, Action onComplete = null, bool ignoreTimeScale = false)
        {
            Stop(); // 确保清理旧状态

            _onTick = onTick;
            _onComplete = onComplete;
            IsRunning = true;

            _cts = new CancellationTokenSource();

            // 启动任务
            RunTimerAsync(interval, loopCount, ignoreTimeScale, _cts.Token).Forget();
        }

        private async UniTaskVoid RunTimerAsync(float interval, int loopCount, bool ignoreTimeScale, CancellationToken token)
        {
            var isInfinite = loopCount < 0;
            var currentLoop = 0;

            try
            {
                // 缓存 TimeSpan 避免在循环中重复创建 Struct
                var delayTimeSpan = TimeSpan.FromSeconds(interval);
                var delayType = ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;

                while (!token.IsCancellationRequested)
                {
                    if (!isInfinite && currentLoop >= loopCount)
                        break;

                    // 等待
                    await UniTask.Delay(delayTimeSpan, delayType, PlayerLoopTiming.Update, token);

                    if (token.IsCancellationRequested) return;

                    currentLoop++;

                    try
                    {
                        // 当前是第几次执行 (1, 2, 3...)
                        _onTick?.Invoke(currentLoop);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AsyncTimer] OnTick Exception: {ex}");
                    }
                }

                IsRunning = false;

                // 只有正常结束才调用 Complete，被 Cancel 不调用
                if (!token.IsCancellationRequested)
                {
                    try
                    {
                        _onComplete?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AsyncTimer] OnComplete Exception: {ex}");
                    }

                    // 完成后自动回收方便使用
                    Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                IsRunning = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncTimer] System Exception: {ex}");
                IsRunning = false;
            }
            finally
            {
                // 只有当前的 CTS 还是原来那个时才清理
                // 避免 Start 重入导致的新 CTS 被清理
                if (_cts != null && _cts.Token == token)
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }

        /// <summary>
        /// 停止计时器 (不会触发 onComplete)
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            _onTick = null;
            _onComplete = null;

            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private void Reset()
        {
            Stop();
            // 其他字段重置
        }

        public void Dispose()
        {
            // 如果已经回收过，直接跳过
            if (IsDisposed) return;

            Recycle(this);
        }
    }
}