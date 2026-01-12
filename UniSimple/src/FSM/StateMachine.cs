using System;
using System.Collections.Generic;

namespace UniSimple.FSM
{
    public sealed class StateMachine
    {
        private readonly Dictionary<Type, IState> _cache = new();
        private readonly Stack<IState> _stack = new();

        public IState CurrentState => _stack.Count > 0 ? _stack.Peek() : null;

        private T GetOrCreateState<T>() where T : IState, new()
        {
            var type = typeof(T);
            if (_cache.TryGetValue(type, out var state))
            {
                return (T)state;
            }

            state = new T();
            state.OnCreate(this);
            _cache[type] = state;

            return (T)state;
        }

        public void Update()
        {
            if (_stack.Count > 0)
            {
                _stack.Peek().OnUpdate();
            }
        }

        /// <summary>
        /// 改变状态
        /// </summary>
        public void ChangeState<T>(object args = null) where T : IState, new()
        {
            while (_stack.Count > 0)
            {
                var topState = _stack.Pop();
                topState.OnExit();
            }

            var newState = GetOrCreateState<T>();
            _stack.Push(newState);
            newState.OnEnter(args);
        }

        /// <summary>
        /// 入栈
        /// </summary>
        public void PushState<T>(object args = null) where T : IState, new()
        {
            if (_stack.Count > 0)
            {
                var currentState = _stack.Peek();
                currentState.OnPause();
            }

            var newState = GetOrCreateState<T>();
            _stack.Push(newState);
            newState.OnEnter(args);
        }

        /// <summary>
        /// 出栈
        /// </summary>
        public void PopState()
        {
            if (_stack.Count > 0)
            {
                var currentState = _stack.Pop();
                currentState.OnExit();

                if (_stack.Count > 0)
                {
                    var previousState = _stack.Peek();
                    previousState.OnResume();
                }
            }
        }
    }
}