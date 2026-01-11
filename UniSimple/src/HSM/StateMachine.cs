using System;
using System.Collections.Generic;

namespace UniSimple.HSM
{
    public sealed class StateMachine : IState
    {
        public string Name { get; private set; }
        public IState CurrentState { get; private set; }

        private readonly Dictionary<string, IState> _states;
        private readonly Dictionary<IState, List<Transition>> _transitionTable;
        private readonly List<Transition> _anyTransitions; // 任意状态转换
        private IState _defaultState;
        private bool _isRunning;

        public StateMachine(string name = "StateMachine")
        {
            Name = name;
            _states = new Dictionary<string, IState>();
            _transitionTable = new Dictionary<IState, List<Transition>>();
            _anyTransitions = new List<Transition>();
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        public StateMachine AddState(IState state, bool isDefault = false)
        {
            if (state == null)
            {
                UnityEngine.Debug.LogError($"[{Name}] Cannot add null state");
                return this;
            }

            if (!_states.TryAdd(state.Name, state))
            {
                UnityEngine.Debug.LogWarning($"[{Name}] State '{state.Name}' already exists");
                return this;
            }

            if (isDefault || _defaultState == null)
            {
                _defaultState = state;
            }

            return this;
        }

        /// <summary>
        /// 添加转换条件
        /// </summary>
        public StateMachine AddTransition(string fromState, string toState, Func<bool> condition)
        {
            if (!_states.TryGetValue(fromState, out var from) || !_states.TryGetValue(toState, out var to))
            {
                UnityEngine.Debug.LogError($"[{Name}] State '{fromState}' not found");
                return this;
            }

            var newTransition = new Transition(from, to, condition);
            if (!_transitionTable.TryGetValue(from, out var list))
            {
                list = new List<Transition>();
                _transitionTable[from] = list;
            }

            list.Add(newTransition);
            return this;
        }

        /// <summary>
        /// 添加任意状态转换（可从任何状态转换）
        /// </summary>
        public StateMachine AddAnyTransition(string toState, Func<bool> condition)
        {
            if (!_states.TryGetValue(toState, out var to))
            {
                UnityEngine.Debug.LogError($"[{Name}] State '{toState}' not found");
                return this;
            }

            _anyTransitions.Add(new Transition(null, to, condition));
            return this;
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        public IState GetState(string stateName)
        {
            _states.TryGetValue(stateName, out var state);
            return state;
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        public bool ChangeState(string stateName)
        {
            if (!_states.TryGetValue(stateName, out var newState))
            {
                UnityEngine.Debug.LogError($"[{Name}] State '{stateName}' not found");
                return false;
            }

            return ChangeState(newState);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        private bool ChangeState(IState newState)
        {
            if (newState == CurrentState)
            {
                return false;
            }

            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState?.OnEnter();

            return true;
        }

        /// <summary>
        /// 检查并执行状态转换
        /// </summary>
        private void CheckTransitions()
        {
            // 优先检查任意状态转换
            foreach (var transition in _anyTransitions)
            {
                if (transition.To != CurrentState && transition.CanTransition())
                {
                    ChangeState(transition.To);
                    return;
                }
            }

            // 检查当前状态的转换
            if (CurrentState != null && _transitionTable.TryGetValue(CurrentState, out var transitionList))
            {
                // 只遍历当前状态相关的转换
                foreach (var transition in transitionList)
                {
                    if (transition.CanTransition())
                    {
                        ChangeState(transition.To);
                        return;
                    }
                }
            }
        }

        public void OnEnter()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;

            if (_defaultState != null)
            {
                ChangeState(_defaultState);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isRunning)
            {
                return;
            }

            // 先转换后执行
            CheckTransitions(); // 检查转换条件
            CurrentState?.OnUpdate(deltaTime); // 更新当前状态
        }

        public void OnExit()
        {
            if (!_isRunning)
            {
                return;
            }

            CurrentState?.OnExit();
            CurrentState = null;
            _isRunning = false;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Start()
        {
            OnEnter();
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Stop()
        {
            OnExit();
        }

        /// <summary>
        /// 重置状态机
        /// </summary>
        public void Reset()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// 是否在指定状态
        /// </summary>
        public bool IsInState(string stateName)
        {
            return CurrentState?.Name == stateName;
        }
    }
}