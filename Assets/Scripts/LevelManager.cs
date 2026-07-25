// LevelManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 关卡推进管理器 - 仅负责4层×3关的推进逻辑
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private SettlementManager settlementManager;

    [Header("关卡状态")]
    [SerializeField] private int currentLayer = 1;    // 当前层级（1-4）
    [SerializeField] private int currentStage = 1;    // 当前关卡序号（1-3）

    // 事件
    public UnityEvent<int, int> OnStageChanged;       // (layer, stage)
    public UnityEvent OnGameCompleted;
    public UnityEvent<string> OnGameOver;

    void Awake()
    {
        OnStageChanged ??= new UnityEvent<int, int>();
        OnGameCompleted ??= new UnityEvent();
        OnGameOver ??= new UnityEvent<string>();

        if (TryGetComponent<ChipsManager>(out var chips))
        {
            chips.OnBankrupt.AddListener(() => OnGameOver?.Invoke("筹码归零"));
        }
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        currentLayer = 1;
        currentStage = 1;
        targetHandManager.ClearCompletedTargets();
        StartNewStage();
    }

    /// <summary>
    /// 开始新的一关
    /// </summary>
    public void StartNewStage()
    {
        Debug.Log($"========== 第{currentLayer}层 第{currentStage}关 ==========");

        deck.ShuffleAndInit();
        targetHandManager.SelectRandomTarget(currentLayer);
        settlementManager?.ResetSettlement();

        OnStageChanged?.Invoke(currentLayer, currentStage);
    }

    /// <summary>
    /// 完成当前关卡，推进到下一关
    /// </summary>
    public void CompleteCurrentStage()
    {
        targetHandManager.MarkCurrentTargetAsCompleted();

        if (currentLayer == 4 && currentStage == 3)
        {
            Debug.Log("════════════════════════════════");
            Debug.Log("       恭喜！全部通关！");
            Debug.Log("════════════════════════════════");
            OnGameCompleted?.Invoke();
            return;
        }

        if (currentStage < 3)
        {
            currentStage++;
        }
        else
        {
            currentLayer++;
            currentStage = 1;
        }

        StartNewStage();
    }

    /// <summary>
    /// 获取当前关卡信息
    /// </summary>
    public (int layer, int stage) GetCurrentStageInfo()
    {
        return (currentLayer, currentStage);
    }

    /// <summary>
    /// 重置所有进度
    /// </summary>
    public void ResetAllProgress()
    {
        currentLayer = 1;
        currentStage = 1;
        targetHandManager.ClearCompletedTargets();
    }
}