using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private GameConfigSO config;

    [Header("关卡状态")]
    [SerializeField] private int currentLayer = 1;
    [SerializeField] private int currentStage = 1;

    public UnityEvent<int, int> OnStageChanged;
    public UnityEvent OnGameCompleted;
    public UnityEvent<string> OnGameOver;

    // 是否刚进入新层级（用于商店首张保底）
    private bool justEnteredNewLayer;

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
        justEnteredNewLayer = true;
        targetHandManager.ClearCompletedTargets();
        StartNewStage();
    }

    public void StartNewStage()
    {
        int totalLayers = config != null ? config.totalLayers : 5;
        Debug.Log($"========== 第{currentLayer}层 第{currentStage}关 ==========");

        deck.ShuffleAndInit();
        targetHandManager.SelectRandomTarget(currentLayer);
        settlementManager?.ResetSettlement();

        OnStageChanged?.Invoke(currentLayer, currentStage);
    }

    public void CompleteCurrentStage()
    {
        targetHandManager.MarkCurrentTargetAsCompleted();

        int totalLayers = config != null ? config.totalLayers : 5;
        int stagesPerLayer = config != null ? config.stagesPerLayer : 3;

        // 第5层只有1关
        if (currentLayer == totalLayers && currentStage >= 1)
        {
            Debug.Log("════════════════════════════════");
            Debug.Log("       恭喜！全部通关！");
            Debug.Log("════════════════════════════════");
            OnGameCompleted?.Invoke();
            return;
        }

        int maxStage = (currentLayer == totalLayers) ? 1 : stagesPerLayer;

        if (currentStage < maxStage)
        {
            currentStage++;
            justEnteredNewLayer = false;
        }
        else
        {
            currentLayer++;
            currentStage = 1;
            justEnteredNewLayer = true;
        }

        StartNewStage();
    }

    public (int layer, int stage) GetCurrentStageInfo()
    {
        return (currentLayer, currentStage);
    }

    public bool IsJustEnteredNewLayer() => justEnteredNewLayer;

    public void ResetAllProgress()
    {
        currentLayer = 1;
        currentStage = 1;
        justEnteredNewLayer = true;
        targetHandManager.ClearCompletedTargets();
    }
}