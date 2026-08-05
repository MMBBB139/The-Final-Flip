using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "地下赌场/游戏配置")]
public class GameConfigSO : ScriptableObject
{
    [Header("筹码")]
    public int initialChips = 30;
    public int surviveReward = 10;

    [Header("修正系统")]
    public int maxCorrectionsPerRound = 1;
    public int correctionCost = 10;

    [Header("商店")]
    public int shopSlotCount = 2;
    public int refreshCost = 10;
    public int maxCarryCards = 4;

    [Header("关卡")]
    public int totalLayers = 5;
    public int stagesPerLayer = 3;

    [Header("误差容忍度(按层级1-5)")]
    public int[] errorToleranceByLayer = { 5, 4, 3, 2, 0 };

    [Header("结算奖励")]
    public int zeroErrorBonus = 20;
    public int oneErrorBonus = 5;

    [Header("特殊规则参数")]
    public int layer2CorrectionWindowOffset = 3;
    public int layer3MaxDrawLimit = 25;
    public int layer4FadedBaseCost = 10;

    /// <summary>
    /// 获取指定层级的误差容忍度
    /// </summary>
    public int GetErrorTolerance(int layer)
    {
        int index = Mathf.Min(layer - 1, errorToleranceByLayer.Length - 1);
        return errorToleranceByLayer[index];
    }
}