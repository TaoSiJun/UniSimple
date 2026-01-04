using System;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks.Triggers;

namespace UniSimple.AsyncTimer
{
    public static class AsyncTimerExtensions
    {
        /// <summary>
        /// 计时器绑定在 GameObject
        /// </summary>
        public static AsyncTimer AttachTo(this AsyncTimer timer, GameObject gameObject)
        {
            if (timer == null || timer.IsDisposed) return timer;

            if (gameObject == null)
            {
                timer.Dispose();
                return timer;
            }

            // 这会自动在 GameObject 上挂载一个 AsyncDestroyTrigger 组件（如果还没有的话）
            var token = gameObject.GetAsyncDestroyTrigger().CancellationToken;
            RegisterCancellation(timer, token);
            return timer;
        }

        /// <summary>
        /// 计时器绑定在 Component
        /// </summary>
        public static AsyncTimer AttachTo(this AsyncTimer timer, Component component)
        {
            if (timer == null || timer.IsDisposed) return timer;

            if (component == null)
            {
                timer.Dispose();
                return timer;
            }

            var token = component.GetAsyncDestroyTrigger().CancellationToken;
            RegisterCancellation(timer, token);
            return timer;
        }

        private static void RegisterCancellation(AsyncTimer timer, CancellationToken token)
        {
            // 如果物体已经销毁，直接停止
            if (token.IsCancellationRequested)
            {
                timer.Dispose();
                return;
            }

            var registration = token.Register(() =>
            {
                if (timer.IsRunning && !timer.IsDisposed)
                {
                    timer.Dispose();
                }
            });
            timer.SetRegistration(registration);
        }
    }
}