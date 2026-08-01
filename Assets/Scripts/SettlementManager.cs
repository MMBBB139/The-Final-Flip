using UnityEngine;

/// <summary>
/// 结算管理器 - 根据策划的分层误差容忍度规则进行结算
/// </summary>
public class SettlementManager : MonoBehaviour
{
    public GameConfigSO config;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private LevelManager levelManager;

    private int lastGuessN;
    private bool hasGuessed;

    public void SetLastGuess(int guessN)
    {
        lastGuessN = guessN;
        hasGuessed = true;
    }

    /// <summary>
    /// 结算：误差≤容忍度则存活，误差=0额外+40
    /// 返回是否存活
    /// </summary>
    public bool Settle(int achievedAtCardCount, int errorToleranceBonus = 0)
    {
        if (!hasGuessed) return false;

        int error = Mathf.Abs(lastGuessN - achievedAtCardCount);
        int layer = levelManager != null ? levelManager.GetCurrentStageInfo().layer : 1;
        int baseTolerance = config != null ? config.errorToleranceByLayer[Mathf.Min(layer - 1, 3)] : 4;
        int tolerance = baseTolerance + errorToleranceBonus;

        if (error > tolerance)
        {
            Debug.Log($"结算失败：误差{error} > 容忍度{tolerance}");
            hasGuessed = false;
            return false;
        }

        // 误差为0额外+40
        if (error == 0)
        {
            chipsManager.AddChips(40);
            Debug.Log("完美猜测！额外+40");
        }

        Debug.Log($"结算成功：误差{error} ≤ 容忍度{tolerance}");
        hasGuessed = false;
        return true;
    }

    public void ResetSettlement()
    {
        lastGuessN = 0;
        hasGuessed = false;
    }

    public int GetLastGuess() => lastGuessN;
}