using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.AsyncTimer
{
    /// <summary>
    /// 异步计时器
    /// </summary>
    public sealed class AsyncTimer : IDisposable
    {
        private const int MAX_POOL_SIZE = 128;

        private static readonly object Lock = new();
        private static readonly Stack<AsyncTimer> Pool = new();
        private static int _increment = 1;

        private CancellationTokenSource _cts;
        private Action<int> _onTick; // 参数: 循环次数
        private Action _onComplete;
        private CancellationTokenRegistration _registration;

        public bool IsDisposed { get; private set; }
        public bool IsRunning { get; private set; }
        public string Tag { get; private set; }
        public int Id { get; private set; }

        public static event Action<AsyncTimer> OnCreated;
        public static event Action<AsyncTimer> OnRecycled;

        private AsyncTimer()
        {
        }

        #region Static Pool

        public static AsyncTimer Create()
        {
            AsyncTimer timer;
            lock (Lock)
            {
                if (Pool.Count > 0)
                {
                    timer = Pool.Pop();
                    timer.IsDisposed = false;
                }
                else
                {
                    timer = new AsyncTimer();
                }

                timer.Id = Interlocked.Increment(ref _increment);
            }

            OnCreated?.Invoke(timer);
            return timer;
        }

        private static void Recycle(AsyncTimer timer)
        {
            lock (Lock)
            {
                if (timer.IsDisposed) return;

                timer.IsDisposed = true;
                timer.Reset();
                if (Pool.Count < MAX_POOL_SIZE)
                {
                    Pool.Push(timer);
                }
            }

            OnRecycled?.Invoke(timer);
        }

        #endregion

        #region Start Methods (Time / Frame / Condition)

        public AsyncTimer WithTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogWarning("Tag cannot be null or empty.");
                return this;
            }

            Tag = tag;
            return this;
        }

        /// <summary>
        /// 时间模式 (Delay / Interval)
        /// </summary>
        public void Start(float interval, int loopCount, Action<int> onTick, Action onComplete, bool ignoreTimeScale = false)
        {
            PrepareStart(onTick, onComplete);
            RunTimerAsync(interval, loopCount, ignoreTimeScale, _cts.Token).Forget();
        }

        /// <summary>
        /// 帧数模式 (DelayFrame)
        /// </summary>
        public void StartFrame(int frames, Action onComplete)
        {
            PrepareStart(null, onComplete);
            RunFrameAsync(frames, _cts.Token).Forget();
        }

        /// <summary>
        /// 条件模式 (WaitUntil)
        /// </summary>
        public void StartWaitUntil(Func<bool> predicate, Action onComplete)
        {
            PrepareStart(null, onComplete);
            RunWaitUntilAsync(predicate, _cts.Token).Forget();
        }

        #endregion

        #region Internal Logic

        private void PrepareStart(Action<int> onTick, Action onComplete)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(AsyncTimer));

            Stop(); // 清理旧状态
            IsRunning = true;
            _onTick = onTick;
            _onComplete = onComplete;
            _cts = new CancellationTokenSource();
        }

        private async UniTaskVoid RunTimerAsync(float interval, int loopCount, bool ignoreTimeScale, CancellationToken token)
        {
            var isInfinite = loopCount < 0;
            var currentLoop = 0;
            var delayTimeSpan = TimeSpan.FromSeconds(interval);
            var delayType = ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!isInfinite && currentLoop >= loopCount) break;

                    await UniTask.Delay(delayTimeSpan, delayType, PlayerLoopTiming.Update, token);
                    if (token.IsCancellationRequested) return;

                    currentLoop++;
                    SafeTick(currentLoop);
                }

                SafeComplete();
            }
            catch (OperationCanceledException)
            {
                /* Ignored */
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                CleanupToken(token);
            }
        }

        private async UniTaskVoid RunFrameAsync(int frames, CancellationToken token)
        {
            try
            {
                // DelayFrame 默认也是 PlayerLoopTiming.Update
                await UniTask.DelayFrame(frames, PlayerLoopTiming.Update, token);
                if (!token.IsCancellationRequested)
                {
                    SafeComplete();
                }
            }
            catch (OperationCanceledException)
            {
                /* Ignored */
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                CleanupToken(token);
            }
        }

        private async UniTaskVoid RunWaitUntilAsync(Func<bool> predicate, CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, token);
                if (!token.IsCancellationRequested)
                {
                    SafeComplete();
                }
            }
            catch (OperationCanceledException)
            {
                /* Ignored */
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                CleanupToken(token);
            }
        }

        private void SafeTick(int loop)
        {
            try
            {
                _onTick?.Invoke(loop);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OnTick Error: {ex}");
            }
        }

        private void SafeComplete()
        {
            IsRunning = false;
            try
            {
                _onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"OnComplete Error: {ex}");
            }

            Dispose();
        }

        private void HandleException(Exception ex)
        {
            Debug.LogError($"Internal Error: {ex}");
            IsRunning = false;
            Dispose();
        }

        private void CleanupToken(CancellationToken token)
        {
            if (_cts == null || _cts.Token != token) return;

            _cts.Dispose();
            _cts = null;
        }

        #endregion

        public void Stop()
        {
            IsRunning = false;
            _onTick = null;
            _onComplete = null;
            _registration.Dispose();

            if (_cts == null || _cts.IsCancellationRequested) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void Reset()
        {
            Stop();
            Tag = null;
        }

        internal void SetRegistration(CancellationTokenRegistration reg)
        {
            _registration.Dispose();
            _registration = reg;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            Recycle(this);
        }
    }
}