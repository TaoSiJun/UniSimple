using System;

namespace UniSimple.HSM
{
    /// <summary>
    /// 转换类
    /// </summary>
    public sealed class Transition
    {
        public IState From { get; }
        public IState To { get; }
        private Func<bool> Condition { get; }

        public Transition(IState from, IState to, Func<bool> condition)
        {
            From = from;
            To = to;
            Condition = condition;
        }

        public bool CanTransition()
        {
            try
            {
                return Condition?.Invoke() ?? false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[StateMachine] Transition condition error: {e}");
                return false;
            }
        }
    }
}