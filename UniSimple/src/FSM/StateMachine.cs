using System;
using System.Collections.Generic;

namespace UniSimple.FSM
{
    public sealed class StateMachine
    {
        private readonly Dictionary<Type, IState> _cache = new();
        private readonly Stack<IState> _stack = new();

        public IState CurrentState { get; private set; }

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
            CurrentState?.OnUpdate();
        }

        public void ChangeState<T>(object args = null) where T : IState, new()
        {
            while (_stack.Count > 0)
            {
                var topState = _stack.Pop();
                topState.OnExit();
            }

            var newState = GetOrCreateState<T>();
            _stack.Push(newState);
            CurrentState = newState;
            CurrentState.OnEnter(args);
        }

        public void PushState<T>(object args = null) where T : IState, new()
        {
            if (_stack.Count > 0)
            {
                var currentState = _stack.Peek();
                currentState.OnPause();
            }

            var newState = GetOrCreateState<T>();
            _stack.Push(newState);
            CurrentState = newState;
            CurrentState.OnEnter(args);
        }

        public void PopState()
        {
            if (_stack.Count <= 0) return;

            var topState = _stack.Pop();
            topState.OnExit();

            if (_stack.Count > 0)
            {
                var previousState = _stack.Peek();
                CurrentState = previousState;
                CurrentState.OnResume();
            }
        }
    }
}