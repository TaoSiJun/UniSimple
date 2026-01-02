using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace UniSimple.GameState
{
    public enum EGameStateAssetType
    {
        None,
        Scene,
        Prefab,
        PrefabCached
    }

    public enum EGameStateStatus
    {
        None,
        Inactive,
        Loading,
        Active
    }

    /// <summary>
    /// 游戏状态的参数
    /// </summary>
    public interface IGameStateParam
    {
    }

    /// <summary>
    /// 过渡处理器
    /// </summary>
    public interface ITransitionHandler
    {
        UniTask OnStart();

        UniTask OnEnd();

        void OnProgress(float progress);
    }

    /// <summary>
    /// 游戏状态的接口
    /// </summary>
    public interface IGameState
    {
        string Name { get; }

        string AssetPath { get; }

        EGameStateAssetType AssetType { get; }

        EGameStateStatus Status { internal set; get; }

        IGameStateParam Param { set; get; }

        SceneHandle SceneHandle { set; get; }

        AssetHandle AssetHandle { set; get; }

        GameObject GameObject { set; get; }

        void OnInit();

        void OnLoaded();

        UniTask OnEnter();

        UniTask OnExit();

        void OnUpdate(float deltaTime);

        void OnDispose();
    }
}