using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 修正系统管理器 - 负责管理修正机会、处理修正逻辑
/// </summary>
public class CorrectionManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private RuleManager ruleManager;

    [Header("修正参数")]
    [SerializeField] private int maxCorrectionsPerRound = 1;
    [SerializeField] private int correctionCost = 20;

    private int remainingCorrections;
    private int lastGuessN;
    private bool hasGuessed;
    private int cardsRevealed;
    private bool correctionWindowOpen;

    public UnityEvent<int, int> OnCorrectionUsed;
    public UnityEvent<string> OnCorrectionFailed;
    public UnityEvent<int> OnNewGuess;
    public UnityEvent OnCorrectionWindowClosed;

    void Awake()
    {
        OnCorrectionUsed ??= new UnityEvent<int, int>();
        OnCorrectionFailed ??= new UnityEvent<string>();
        OnNewGuess ??= new UnityEvent<int>();
        OnCorrectionWindowClosed ??= new UnityEvent();

        ResetCorrections();
    }

    public void ResetCorrections()
    {
        remainingCorrections = maxCorrectionsPerRound;
        lastGuessN = 0;
        hasGuessed = false;
        cardsRevealed = 0;
        correctionWindowOpen = true;
        Debug.Log($"修正系统已重置，可用次数: {remainingCorrections}");
    }

    public bool MakeInitialGuess(int guessN)
    {
        if (guessN < 1 || guessN > 52)
        {
            OnCorrectionFailed?.Invoke("猜测数字必须在1-52之间");
            return false;
        }

        if (hasGuessed)
        {
            OnCorrectionFailed?.Invoke("已经进行过猜测，请使用修正更改");
            return false;
        }

        lastGuessN = guessN;
        hasGuessed = true;
        correctionWindowOpen = true;

        settlementManager?.SetLastGuess(guessN);

        Debug.Log($"初始猜测: N={guessN}");
        return true;
    }

    public void OnCardRevealed()
    {
        cardsRevealed++;

        int windowOffset = 0;
        if (ruleManager != null)
            windowOffset = ruleManager.GetCorrectionWindowOffset();

        if (correctionWindowOpen && cardsRevealed >= lastGuessN - windowOffset)
        {
            correctionWindowOpen = false;
            OnCorrectionWindowClosed?.Invoke();
            Debug.Log($"已翻到第{cardsRevealed}张，修正窗口关闭（猜测N={lastGuessN}，偏移={windowOffset}）");
        }
    }

    public bool UseCorrection(int newGuessN)
    {
        if (!hasGuessed)
        {
            OnCorrectionFailed?.Invoke("尚未进行初始猜测，无法修正");
            return false;
        }

        if (remainingCorrections <= 0)
        {
            OnCorrectionFailed?.Invoke("本局修正次数已用完");
            return false;
        }

        if (!correctionWindowOpen)
        {
            OnCorrectionFailed?.Invoke($"已翻过第{lastGuessN}张，修正窗口已关闭");
            return false;
        }

        if (newGuessN < 1 || newGuessN > 52)
        {
            OnCorrectionFailed?.Invoke("新猜测数字必须在1-52之间");
            return false;
        }

        if (newGuessN == lastGuessN)
        {
            OnCorrectionFailed?.Invoke("新猜测不能与当前猜测相同");
            return false;
        }

        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < correctionCost)
        {
            OnCorrectionFailed?.Invoke($"筹码不足！需要{correctionCost}筹码，当前仅有{currentChips}筹码");
            return false;
        }

        chipsManager?.AddChips(-correctionCost);

        int oldGuessN = lastGuessN;
        lastGuessN = newGuessN;
        remainingCorrections--;

        settlementManager?.SetLastGuess(newGuessN);

        int windowOffset = 0;
        if (ruleManager != null)
            windowOffset = ruleManager.GetCorrectionWindowOffset();

        if (cardsRevealed >= lastGuessN - windowOffset)
        {
            correctionWindowOpen = false;
            OnCorrectionWindowClosed?.Invoke();
            Debug.Log($"新猜测N={lastGuessN}，但已翻{cardsRevealed}张，修正窗口立即关闭");
        }

        Debug.Log($"修正成功！{oldGuessN} → {newGuessN}，消耗{correctionCost}筹码，剩余次数: {remainingCorrections}");
        OnCorrectionUsed?.Invoke(remainingCorrections, correctionCost);
        OnNewGuess?.Invoke(newGuessN);

        return true;
    }

    public void AddCorrectionChances(int additionalChances)
    {
        remainingCorrections += additionalChances;
        Debug.Log($"修正次数增加{additionalChances}，当前总次数: {remainingCorrections}");
    }

    public void SetCorrectionCost(int newCost)
    {
        correctionCost = Mathf.Max(0, newCost);
        Debug.Log($"修正消耗变更为: {correctionCost}");
    }

    public void ExtendCorrectionWindow(bool extend)
    {
        if (extend)
        {
            correctionWindowOpen = true;
            Debug.Log("修正窗口已扩展，可在翻过N后继续修正");
        }
    }

    public void SetExtendedCorrectionCost(int cost)
    {
        correctionCost = Mathf.Max(0, cost);
    }

    public CorrectionStatus GetStatus()
    {
        int windowOffset = 0;
        if (ruleManager != null)
            windowOffset = ruleManager.GetCorrectionWindowOffset();

        return new CorrectionStatus
        {
            currentGuess = lastGuessN,
            remainingCorrections = remainingCorrections,
            correctionCost = correctionCost,
            cardsRevealed = cardsRevealed,
            windowOpen = correctionWindowOpen,
            canCorrect = hasGuessed && remainingCorrections > 0 && correctionWindowOpen && cardsRevealed < lastGuessN - windowOffset
        };
    }

    public int GetLastGuess()
    {
        return lastGuessN;
    }

    public bool CanCorrect()
    {
        return hasGuessed && remainingCorrections > 0 && correctionWindowOpen;
    }
}

[System.Serializable]
public struct CorrectionStatus
{
    public int currentGuess;
    public int remainingCorrections;
    public int correctionCost;
    public int cardsRevealed;
    public bool windowOpen;
    public bool canCorrect;
}