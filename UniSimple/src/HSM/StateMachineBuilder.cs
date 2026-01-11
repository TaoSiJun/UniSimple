using System;
using System.Collections.Generic;

namespace UniSimple.HSM
{
    public sealed class StateMachineBuilder
    {
        private readonly string _stateMachineName;
        private readonly Dictionary<string, bool> _states;
        private readonly Dictionary<string, Action> _enterActions;
        private readonly Dictionary<string, Action<float>> _updateActions;
        private readonly Dictionary<string, Action> _exitActions;
        private readonly Dictionary<(string From, string To), Func<bool>> _transitions;
        private readonly Dictionary<string, Func<bool>> _anyTransitions;
        private readonly Dictionary<StateMachine, bool> _subStateMachine;

        public StateMachineBuilder(string name)
        {
            _stateMachineName = name;
            _states = new Dictionary<string, bool>();
            _enterActions = new Dictionary<string, Action>();
            _updateActions = new Dictionary<string, Action<float>>();
            _exitActions = new Dictionary<string, Action>();
            _transitions = new Dictionary<(string From, string To), Func<bool>>();
            _anyTransitions = new Dictionary<string, Func<bool>>();
            _subStateMachine = new Dictionary<StateMachine, bool>();
        }


        /// <summary>
        /// 添加简单状态
        /// </summary>
        public StateMachineBuilder State(string stateName, bool isDefault = false)
        {
            if (!_states.TryAdd(stateName, isDefault))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] State {stateName} already exists");
            }

            return this;
        }

        /// <summary>
        /// 设置状态进入回调
        /// </summary>
        public StateMachineBuilder OnEnter(string stateName, Action action)
        {
            if (!_enterActions.TryAdd(stateName, action))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] OnEnter {stateName} already exists");
            }

            return this;
        }

        /// <summary>
        /// 设置状态更新回调
        /// </summary>
        public StateMachineBuilder OnUpdate(string stateName, Action<float> action)
        {
            if (!_updateActions.TryAdd(stateName, action))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] OnUpdate {stateName} already exists");
            }

            return this;
        }

        /// <summary>
        /// 设置状态退出回调
        /// </summary>
        public StateMachineBuilder OnExit(string stateName, Action action)
        {
            if (!_exitActions.TryAdd(stateName, action))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] OnExit {stateName} already exists");
            }

            return this;
        }

        /// <summary>
        /// 添加子状态机
        /// </summary>
        public StateMachineBuilder SubStateMachine(StateMachine subMachine, bool isDefault = false)
        {
            if (!_subStateMachine.TryAdd(subMachine, isDefault))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] SubStateMachine {subMachine?.Name} already exists");
            }

            return this;
        }

        /// <summary>
        /// 添加转换
        /// </summary>
        public StateMachineBuilder Transition(string from, string to, Func<bool> condition)
        {
            if (!_transitions.TryAdd((from, to), condition))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] Transition '{from} > {to}' already exists");
            }

            return this;
        }

        /// <summary>
        /// 添加任意状态转换
        /// </summary>
        public StateMachineBuilder AnyTransition(string to, Func<bool> condition)
        {
            if (!_anyTransitions.TryAdd(to, condition))
            {
                UnityEngine.Debug.LogWarning($"[StateMachineBuilder] AnyTransition '{to}' already exists");
            }

            return this;
        }

        /// <summary>
        /// 构建状态机
        /// </summary>
        public StateMachine Build()
        {
            var stateMachine = new StateMachine(_stateMachineName);

            // 创建所有状态
            foreach (var (name, isDefault) in _states)
            {
                _enterActions.TryGetValue(name, out var enterAction);
                _updateActions.TryGetValue(name, out var updateAction);
                _exitActions.TryGetValue(name, out var exitAction);
                var state = new State(name, enterAction, updateAction, exitAction);
                stateMachine.AddState(state, isDefault);
            }

            // 添加子状态
            foreach (var (subStateMachine, isDefault) in _subStateMachine)
            {
                stateMachine.AddState(subStateMachine, isDefault);
            }

            // 创建任意转换
            foreach (var (to, condition) in _anyTransitions)
            {
                stateMachine.AddAnyTransition(to, condition);
            }

            // 创建转换
            foreach (var (transition, condition) in _transitions)
            {
                stateMachine.AddTransition(transition.From, transition.To, condition);
            }

            return stateMachine;
        }

        /// <summary>
        /// 清空状态
        /// </summary>
        public void Clear()
        {
            _states.Clear();
            _enterActions.Clear();
            _updateActions.Clear();
            _exitActions.Clear();
            _subStateMachine.Clear();
            _transitions.Clear();
            _anyTransitions.Clear();
        }
    }
}