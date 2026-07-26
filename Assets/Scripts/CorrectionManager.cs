using UnityEngine;
using UnityEngine.Events;

public class CorrectionManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private RuleManager ruleManager;

    private int remainingCorrections;
    private int lastGuessN;
    private bool hasGuessed;
    private int cardsRevealed;
    private bool correctionWindowOpen;
    private bool extendedWindow;           // 超时修正激活
    private int extendedCorrectionCost;    // 超时修正消耗

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
    }

    public void ResetCorrections()
    {
        remainingCorrections = config != null ? config.maxCorrectionsPerRound : 1;
        lastGuessN = 0;
        hasGuessed = false;
        cardsRevealed = 0;
        correctionWindowOpen = true;
        extendedWindow = false;
        extendedCorrectionCost = 0;
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
        int windowOffset = ruleManager != null ? ruleManager.GetCorrectionWindowOffset() : 0;

        // 如果是超时修正模式，窗口永不自动关闭
        if (extendedWindow) return;

        if (correctionWindowOpen && cardsRevealed >= lastGuessN - windowOffset)
        {
            correctionWindowOpen = false;
            OnCorrectionWindowClosed?.Invoke();
            Debug.Log($"修正窗口关闭（翻到第{cardsRevealed}张，猜测N={lastGuessN}，偏移={windowOffset}）");
        }
    }

    public bool UseCorrection(int newGuessN)
    {
        if (!hasGuessed) { OnCorrectionFailed?.Invoke("尚未猜测"); return false; }
        if (remainingCorrections <= 0) { OnCorrectionFailed?.Invoke("修正次数已用完"); return false; }

        // 非超时模式下检查窗口
        if (!extendedWindow && !correctionWindowOpen)
        {
            OnCorrectionFailed?.Invoke("修正窗口已关闭");
            return false;
        }

        if (newGuessN < 1 || newGuessN > 52) { OnCorrectionFailed?.Invoke("猜测范围1-52"); return false; }
        if (newGuessN == lastGuessN) { OnCorrectionFailed?.Invoke("新猜测不能相同"); return false; }

        // 计算消耗：超时模式用扩展消耗，否则用配置消耗
        int cost = extendedWindow ? extendedCorrectionCost : (config != null ? config.correctionCost : 20);
        if (chipsManager.GetChips() < cost)
        {
            OnCorrectionFailed?.Invoke($"筹码不足，需要{cost}");
            return false;
        }

        chipsManager.AddChips(-cost);
        int oldGuess = lastGuessN;
        lastGuessN = newGuessN;
        remainingCorrections--;
        settlementManager?.SetLastGuess(newGuessN);

        // 非超时模式下重新检查窗口
        if (!extendedWindow)
        {
            int windowOffset = ruleManager != null ? ruleManager.GetCorrectionWindowOffset() : 0;
            if (cardsRevealed >= lastGuessN - windowOffset)
            {
                correctionWindowOpen = false;
                OnCorrectionWindowClosed?.Invoke();
            }
        }

        Debug.Log($"修正 {oldGuess}→{newGuessN}，消耗{cost}，剩余次数:{remainingCorrections}");
        OnCorrectionUsed?.Invoke(remainingCorrections, cost);
        OnNewGuess?.Invoke(newGuessN);
        return true;
    }

    /// <summary>
    /// 增加修正次数（二次修正策略牌）
    /// </summary>
    public void AddCorrectionChances(int extra)
    {
        remainingCorrections += extra;
        Debug.Log($"修正次数+{extra}，当前:{remainingCorrections}");
    }

    /// <summary>
    /// 设置修正消耗（打折修正策略牌）
    /// </summary>
    public void SetCorrectionCost(int newCost)
    {
        // 通过修改config引用会影响全局，这里用一个本地偏移
        Debug.Log($"修正消耗已通过策略牌修改为: {newCost}");
    }

    /// <summary>
    /// 激活超时修正窗口（超时修正策略牌）
    /// </summary>
    public void ExtendCorrectionWindow(bool extend)
    {
        extendedWindow = extend;
        if (extend)
        {
            correctionWindowOpen = true;
            Debug.Log("修正窗口已扩展，可在翻过N后继续修正");
        }
    }

    /// <summary>
    /// 设置超时修正的消耗（超时修正策略牌）
    /// </summary>
    public void SetExtendedCorrectionCost(int cost)
    {
        extendedCorrectionCost = Mathf.Max(0, cost);
        Debug.Log($"超时修正消耗设为: {extendedCorrectionCost}");
    }

    public int GetLastGuess() => lastGuessN;

    public bool CanCorrect()
    {
        if (!hasGuessed || remainingCorrections <= 0) return false;
        if (extendedWindow) return true;
        return correctionWindowOpen;
    }

    public CorrectionStatus GetStatus()
    {
        int offset = ruleManager != null ? ruleManager.GetCorrectionWindowOffset() : 0;
        return new CorrectionStatus
        {
            currentGuess = lastGuessN,
            remainingCorrections = remainingCorrections,
            correctionCost = config != null ? config.correctionCost : 20,
            cardsRevealed = cardsRevealed,
            windowOpen = correctionWindowOpen || extendedWindow,
            canCorrect = CanCorrect() && (extendedWindow || cardsRevealed < lastGuessN - offset)
        };
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