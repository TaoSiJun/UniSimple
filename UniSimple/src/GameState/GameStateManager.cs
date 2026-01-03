using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniSimple.Singleton;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace UniSimple.GameState
{
    internal class StateHistory
    {
        public Type Type;

        public IGameStateParam Param;
    }

    public class GameStateManager : Singleton<GameStateManager>, IUpdatable, IDisposable
    {
        private readonly Dictionary<Type, IGameState> _states = new();
        private readonly List<StateHistory> _histories = new();
        private bool _isTransition;

        public override int Priority => 1000;
        public IGameState CurrentState { get; private set; }
        public ITransitionHandler TransitionHandler { get; set; }


        /// <summary>
        /// 更改状态
        /// </summary>
        /// <param name="param"></param>
        /// <typeparam name="T"></typeparam>
        public async UniTask ChangeState<T>(IGameStateParam param) where T : IGameState, new()
        {
            if (_isTransition)
            {
                Debug.LogWarning("[GameStateManager] Transition in progress");
                return;
            }

            var type = typeof(T);
            if (type == CurrentState?.GetType())
            {
                return;
            }

            _isTransition = true;

            try
            {
                if (TransitionHandler != null)
                {
                    await TransitionHandler.OnStart();
                }

                // 退出当前状态
                if (CurrentState != null)
                {
                    try
                    {
                        await HandleExitAsync(CurrentState);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameStateManager] Exit {CurrentState.Name} error: {e}");
                        UnloadAsset(CurrentState);
                    }

                    _histories.Add(new StateHistory
                    {
                        Type = CurrentState.GetType(),
                        Param = CurrentState.Param
                    });

                    CurrentState = null;
                }

                // 获取或创建新状态
                var isFirstEnter = !_states.TryGetValue(type, out var state);
                if (isFirstEnter)
                {
                    state = new T();
                    state.Status = EGameStateStatus.None;
                    state.OnInit();
                    _states[type] = state;
                }

                // 进入新状态
                await HandleEnterAsync(state, param);
                CurrentState = state;

                if (TransitionHandler != null)
                {
                    await TransitionHandler.OnEnd();
                }
            }
            finally
            {
                _isTransition = false;
            }
        }

        /// <summary>
        /// 退到上一个状态
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> BackState()
        {
            if (_isTransition || _histories.Count <= 0)
            {
                return false;
            }

            _isTransition = true;

            if (TransitionHandler != null)
            {
                await TransitionHandler.OnStart();
            }

            try
            {
                if (CurrentState != null)
                {
                    await HandleExitAsync(CurrentState);
                    CurrentState = null;
                }

                var history = _histories[^1];
                _histories.RemoveAt(_histories.Count - 1);

                if (_states.TryGetValue(history.Type, out var state))
                {
                    await HandleEnterAsync(state, history.Param);
                    CurrentState = state;

                    if (TransitionHandler != null)
                    {
                        await TransitionHandler.OnEnd();
                    }

                    return true;
                }

                return false;
            }
            finally
            {
                _isTransition = false;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            CurrentState?.OnUpdate(deltaTime);
        }

        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                state.OnDispose();
                state.SceneHandle?.UnloadAsync();
                state.SceneHandle?.Release();
                state.SceneHandle = null;
                if (state.GameObject != null)
                {
                    Object.Destroy(state.GameObject);
                    state.GameObject = null;
                }

                state.AssetHandle?.Release();
                state.AssetHandle = null;
            }

            _states.Clear();
            _histories.Clear();

            CurrentState = null;
        }

        private async UniTask HandleEnterAsync(IGameState state, IGameStateParam param)
        {
            state.Status = EGameStateStatus.Loading;
            await LoadAssetAsync(state);
            state.Param = param;
            await state.OnEnter();
            state.Status = EGameStateStatus.Active;
        }

        private async UniTask HandleExitAsync(IGameState state)
        {
            await state.OnExit();
            state.Status = EGameStateStatus.Inactive;
            UnloadAsset(state);
        }

        private async UniTask LoadAssetAsync(IGameState state)
        {
            switch (state.AssetType)
            {
                case EGameStateAssetType.None:
                    break;
                case EGameStateAssetType.Scene:
                {
                    var handle = YooAssets.LoadSceneAsync(state.AssetPath);
                    while (!handle.IsDone)
                    {
                        TransitionHandler?.OnProgress(handle.Progress);
                        await UniTask.NextFrame();
                    }

                    if (handle.Status == EOperationStatus.Succeed)
                    {
                        state.SceneHandle = handle;
                        state.OnLoaded();
                    }
                    else
                    {
                        handle.Release();
                        throw new Exception($"[GameStateManager] Failed to load asset for state: {state.GetType().Name}");
                    }

                    break;
                }
                case EGameStateAssetType.Prefab:
                case EGameStateAssetType.PrefabCached:
                {
                    // 缓存了的预制体
                    if (state.GameObject != null)
                    {
                        state.GameObject.SetActive(true);
                        return;
                    }

                    var handle = YooAssets.LoadAssetAsync<GameObject>(state.AssetPath);
                    while (!handle.IsDone)
                    {
                        TransitionHandler?.OnProgress(handle.Progress);
                        await UniTask.NextFrame();
                    }

                    if (handle.Status == EOperationStatus.Succeed)
                    {
                        state.AssetHandle = handle;
                        state.GameObject = Object.Instantiate(handle.GetAssetObject<GameObject>());
                        state.OnLoaded();
                    }
                    else
                    {
                        handle.Release();
                        throw new Exception($"[GameStateManager] Failed to load asset for state: {state.GetType().Name}");
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void UnloadAsset(IGameState state)
        {
            switch (state.AssetType)
            {
                case EGameStateAssetType.Scene:
                {
                    if (state.SceneHandle != null)
                    {
                        state.SceneHandle.UnloadAsync();
                        state.SceneHandle = null;
                    }

                    break;
                }
                case EGameStateAssetType.Prefab:
                {
                    if (state.GameObject != null)
                    {
                        Object.Destroy(state.GameObject);
                        state.GameObject = null;
                    }

                    state.AssetHandle?.Release();
                    state.AssetHandle = null;
                    break;
                }
                case EGameStateAssetType.PrefabCached:
                {
                    if (state.GameObject != null)
                    {
                        state.GameObject.SetActive(false);
                    }

                    break;
                }
            }
        }
    }
}