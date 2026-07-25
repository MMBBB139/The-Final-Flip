// SettlementManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结算管理器 - 仅负责根据误差表计算并更新玩家筹码
/// </summary>
public class SettlementManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private ChipsManager chipsManager;

    [Header("结算参数")]
    private int lastGuessN;          // 最后一次猜测数字N
    private int targetAchievedAt;    // 目标实际达成时的翻牌张数
    private bool hasGuessed = false; // 是否已进行过猜测

    // 误差结算表（行：误差值，列：[猜早, 猜晚]）
    private static readonly Dictionary<int, (int earlyPenalty, int latePenalty)> ErrorTable =
        new Dictionary<int, (int, int)>
        {
            { 0, (60, 60) },
            { 1, (30, 20) },
            { 2, (-20, -40) },
            { 3, (-40, -60) },
            { 4, (-60, -80) },
            { 5, (-80, -100) } // 5+误差都按此处理
        };

    void Awake()
    {
        if (chipsManager == null)
            chipsManager = GetComponent<ChipsManager>();

        if (chipsManager == null)
            Debug.LogError("SettlementManager: 未找到ChipsManager组件！");
    }

    /// <summary>
    /// 记录玩家最后一次猜测（修正时更新）
    /// </summary>
    public void SetLastGuess(int guessN)
    {
        lastGuessN = guessN;
        hasGuessed = true;
        Debug.Log($"记录最后一次猜测: N={guessN}");
    }

    /// <summary>
    /// 目标达成时调用，传入目标实际达成时的翻牌张数
    /// </summary>
    public void OnTargetAchieved(int achievedAtCardCount)
    {
        if (!hasGuessed)
        {
            Debug.LogError("结算失败：玩家尚未进行猜测");
            return;
        }

        targetAchievedAt = achievedAtCardCount;
        Debug.Log($"目标达成时翻牌张数: {targetAchievedAt}, 猜测N={lastGuessN}");

        // 计算误差
        int error = CalculateError(lastGuessN, targetAchievedAt);
        bool isEarly = lastGuessN < targetAchievedAt; // 猜早

        // 获取筹码变动量
        int chipChange = GetChipChange(error, isEarly);

        // 应用筹码变动
        ApplySettlement(chipChange, error, isEarly);

        // 结算完成，重置猜测状态
        hasGuessed = false;
    }

    /// <summary>
    /// 计算误差值 = |猜测N - 实际达成张数|
    /// </summary>
    private int CalculateError(int guessN, int achievedAt)
    {
        return Mathf.Abs(guessN - achievedAt);
    }

    /// <summary>
    /// 根据误差和猜早/猜晚获取筹码变动量
    /// </summary>
    private int GetChipChange(int error, bool isEarly)
    {
        // 误差≥5统一按5处理
        int errorKey = Mathf.Min(error, 5);

        if (!ErrorTable.ContainsKey(errorKey))
        {
            Debug.LogError($"结算表未找到误差{errorKey}的数据");
            return 0;
        }

        var (earlyValue, lateValue) = ErrorTable[errorKey];
        int chipChange = isEarly ? earlyValue : lateValue;

        Debug.Log($"误差={error}({errorKey}), {(isEarly ? "猜早" : "猜晚")}, 筹码变动={chipChange:+0;-0}");
        return chipChange;
    }

    /// <summary>
    /// 应用筹码变动（正数增加，负数减少）
    /// </summary>
    private void ApplySettlement(int chipChange, int error, bool isEarly)
    {
        if (chipChange > 0)
        {
            chipsManager.AddChips(chipChange);
            Debug.Log($"完美成功！误差={error}，奖励+{chipChange}筹码");
        }
        else if (chipChange < 0)
        {
            bool success = chipsManager.SpendChips(-chipChange);
            if (success)
            {
                Debug.Log($"预测失败！误差={error}，惩罚{chipChange}筹码");
            }
        }
        else
        {
            Debug.Log($"误差={error}，筹码不变");
        }
    }

    /// <summary>
    /// 手动结算（不依赖目标达成回调）
    /// </summary>
    public void ManualSettle(int finalAchievedAt)
    {
        OnTargetAchieved(finalAchievedAt);
    }

    /// <summary>
    /// 获取当前猜测值（供其他组件查询）
    /// </summary>
    public int GetLastGuess()
    {
        return lastGuessN;
    }

    /// <summary>
    /// 重置结算状态（新关卡开始时调用）
    /// </summary>
    public void ResetSettlement()
    {
        lastGuessN = 0;
        targetAchievedAt = 0;
        hasGuessed = false;
        Debug.Log("结算状态已重置");
    }
}