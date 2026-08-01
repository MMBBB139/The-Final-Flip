using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 关卡推进管理器 - 负责4层×3关的推进逻辑
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private SettlementManager settlementManager;

    [Header("关卡状态")]
    [SerializeField] private int currentLayer = 1;
    [SerializeField] private int currentStage = 1;

    public UnityEvent<int, int> OnStageChanged;
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

    public void StartNewGame()
    {
        currentLayer = 1;
        currentStage = 1;
        targetHandManager.ClearCompletedTargets();
        StartNewStage();
    }

    public void StartNewStage()
    {
        Debug.Log($"========== 第{currentLayer}层 第{currentStage}关 ==========");

        deck.ShuffleAndInit();
        targetHandManager.SelectRandomTarget(currentLayer);
        settlementManager?.ResetSettlement();

        OnStageChanged?.Invoke(currentLayer, currentStage);
    }

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

    public (int layer, int stage) GetCurrentStageInfo()
    {
        return (currentLayer, currentStage);
    }

    public void ResetAllProgress()
    {
        currentLayer = 1;
        currentStage = 1;
        targetHandManager.ClearCompletedTargets();
    }
}