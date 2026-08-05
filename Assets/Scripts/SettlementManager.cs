using UnityEngine;

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

    public bool Settle(int achievedAtCardCount, int errorToleranceBonus = 0)
    {
        if (!hasGuessed) return false;

        int error = Mathf.Abs(lastGuessN - achievedAtCardCount);
        int layer = levelManager != null ? levelManager.GetCurrentStageInfo().layer : 1;
        int baseTolerance = config != null ? config.GetErrorTolerance(layer) : 5;
        int tolerance = baseTolerance + errorToleranceBonus;

        if (error > tolerance)
        {
            Debug.Log($"结算失败：误差{error} > 容忍度{tolerance}");
            hasGuessed = false;
            return false;
        }

        // 误差=0额外+20
        if (error == 0)
        {
            int bonus = config != null ? config.zeroErrorBonus : 20;
            chipsManager.AddChips(bonus);
            Debug.Log($"完美猜测！额外+{bonus}");
        }

        // 误差=1额外+5
        if (error == 1)
        {
            int bonus = config != null ? config.oneErrorBonus : 5;
            chipsManager.AddChips(bonus);
            Debug.Log($"误差为1！额外+{bonus}");
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