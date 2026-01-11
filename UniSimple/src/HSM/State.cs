using System;

namespace UniSimple.HSM
{
    public interface IState
    {
        string Name { get; }
        void OnEnter();
        void OnUpdate(float deltaTime);
        void OnExit();
    }

    /// <summary>
    /// 状态类
    /// </summary>
    public sealed class State : IState
    {
        public string Name { get; }

        private readonly Action _onEnter;
        private readonly Action<float> _onUpdate;
        private readonly Action _onExit;

        public State(string name, Action onEnter = null, Action<float> onUpdate = null, Action onExit = null)
        {
            Name = name;
            _onEnter = onEnter;
            _onUpdate = onUpdate;
            _onExit = onExit;
        }

        public void OnEnter()
        {
            _onEnter?.Invoke();
        }

        public void OnUpdate(float deltaTime)
        {
            _onUpdate?.Invoke(deltaTime);
        }

        public void OnExit()
        {
            _onExit?.Invoke();
        }
    }
}