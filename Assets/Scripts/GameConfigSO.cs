using UnityEngine;

/// <summary>
/// 游戏全局配置 - 所有可调整的数值都在这里
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "地下赌场/游戏配置")]
public class GameConfigSO : ScriptableObject
{
    [Header("筹码")]
    public int initialChips = 30;
    public int surviveReward = 20;

    [Header("修正系统")]
    public int maxCorrectionsPerRound = 1;
    public int correctionCost = 10;

    [Header("商店")]
    public int shopSlotCount = 2;
    public int refreshCost = 5;
    public int maxCarryCards = 4;

    [Header("误差容忍度(按层级1-4)")]
    public int[] errorToleranceByLayer = { 4, 3, 2, 1 };

    [Header("特殊规则参数")]
    public int layer1ReturnCardCount = 3;
    public int layer2CorrectionWindowOffset = 3;
    public int layer3MaxDrawLimit = 25;
    public int layer4RevealFadedCost = 20;
}