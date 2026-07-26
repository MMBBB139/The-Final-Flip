using UnityEngine;

public class SettlementManager : MonoBehaviour
{
    public GameConfigSO config;   // 改为 public，方便 GameManager 访问
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