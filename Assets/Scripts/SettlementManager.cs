using UnityEngine;

/// <summary>
/// 结算管理器 - 根据玩家猜测和实际达成张数计算筹码变动。
/// 仅负责基础误差结算（查表得出筹码变化），倍率、容错、全押等额外结算效果由 StrategyCardManager 和 GameManager 处理。
/// </summary>
public class SettlementManager : MonoBehaviour
{
    public GameConfigSO config;
    [SerializeField] private ChipsManager chipsManager;

    private int lastGuessN;
    private int targetAchievedAt;
    private bool hasGuessed;

    public void SetLastGuess(int guessN)
    {
        lastGuessN = guessN;
        hasGuessed = true;
    }

    public void OnTargetAchieved(int achievedAtCardCount)
    {
        if (!hasGuessed) return;
        targetAchievedAt = achievedAtCardCount;

        int error = Mathf.Abs(lastGuessN - targetAchievedAt);
        bool isEarly = lastGuessN < targetAchievedAt;
        int chipChange = config.GetChipChange(error, isEarly);

        chipsManager.AddChips(chipChange);
        Debug.Log($"结算：误差{error}，{(isEarly ? "猜早" : "猜晚")}，筹码{chipChange:+0;-0}");
        hasGuessed = false;
    }

    public void ResetSettlement()
    {
        lastGuessN = 0;
        hasGuessed = false;
    }

    public int GetLastGuess() => lastGuessN;
}