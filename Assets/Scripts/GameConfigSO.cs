using UnityEngine;

/// <summary>
/// 游戏全局配置 - 所有可调整的数值都在这里
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "地下赌场/游戏配置")]
public class GameConfigSO : ScriptableObject
{
    [Header("筹码")]
    public int initialChips = 200;

    [Header("修正系统")]
    public int maxCorrectionsPerRound = 1;
    public int correctionCost = 20;

    [Header("商店")]
    public int shopSlotCount = 3;
    public int refreshCost = 20;
    public int maxCarryCards = 4;

    [Header("特殊规则参数")]
    public int layer1ReturnCardCount = 3;         // 第1层特殊：洗回已翻牌最后N张
    public int layer2CorrectionWindowOffset = 3;  // 第2层特殊：修正窗口提前关闭张数

    [Header("结算误差表 (误差值, [猜早, 猜晚])")]
    public ErrorEntry[] errorTable = new ErrorEntry[]
    {
        new ErrorEntry { error = 0, early = 60, late = 60 },
        new ErrorEntry { error = 1, early = 30, late = 20 },
        new ErrorEntry { error = 2, early = -20, late = -40 },
        new ErrorEntry { error = 3, early = -40, late = -60 },
        new ErrorEntry { error = 4, early = -60, late = -80 },
        new ErrorEntry { error = 5, early = -80, late = -100 } // 5+误差统一用此条
    };

    [System.Serializable]
    public struct ErrorEntry
    {
        public int error;
        public int early;
        public int late;
    }

    /// <summary>
    /// 根据误差获取筹码变动值（猜早/猜晚）
    /// </summary>
    public int GetChipChange(int error, bool isEarly)
    {
        int key = Mathf.Min(error, 5);
        foreach (var entry in errorTable)
        {
            if (entry.error == key)
                return isEarly ? entry.early : entry.late;
        }
        return 0;
    }
}