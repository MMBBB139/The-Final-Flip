// CorrectionManager.cs
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

    [Header("修正参数")]
    [SerializeField] private int maxCorrectionsPerRound = 1;  // 每局默认修正次数
    [SerializeField] private int correctionCost = 20;          // 每次修正消耗筹码

    private int remainingCorrections;    // 本局剩余修正次数
    private int lastGuessN;              // 最后一次猜测N
    private bool hasGuessed;             // 是否已进行过猜测
    private int cardsRevealed;           // 已翻牌张数
    private bool correctionWindowOpen;   // 修正窗口是否还开放

    // 事件
    public UnityEvent<int, int> OnCorrectionUsed;     // (剩余次数, 消耗筹码)
    public UnityEvent<string> OnCorrectionFailed;     // (失败原因)
    public UnityEvent<int> OnNewGuess;                // (新猜测N)
    public UnityEvent OnCorrectionWindowClosed;       // 修正窗口关闭

    void Awake()
    {
        if (deck == null) deck = GetComponent<Deck>();
        if (chipsManager == null) chipsManager = GetComponent<ChipsManager>();
        if (settlementManager == null) settlementManager = GetComponent<SettlementManager>();

        OnCorrectionUsed ??= new UnityEvent<int, int>();
        OnCorrectionFailed ??= new UnityEvent<string>();
        OnNewGuess ??= new UnityEvent<int>();
        OnCorrectionWindowClosed ??= new UnityEvent();

        ResetCorrections();
    }

    /// <summary>
    /// 重置修正状态（新关卡开始时调用）
    /// </summary>
    public void ResetCorrections()
    {
        remainingCorrections = maxCorrectionsPerRound;
        lastGuessN = 0;
        hasGuessed = false;
        cardsRevealed = 0;
        correctionWindowOpen = true;
        Debug.Log($"修正系统已重置，可用次数: {remainingCorrections}");
    }

    /// <summary>
    /// 玩家进行初始猜测
    /// </summary>
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

        // 通知结算管理器记录猜测
        settlementManager?.SetLastGuess(guessN);

        Debug.Log($"初始猜测: N={guessN}");
        return true;
    }

    /// <summary>
    /// 翻牌时更新已翻牌计数，并检查修正窗口
    /// </summary>
    public void OnCardRevealed()
    {
        cardsRevealed++;

        // 检查是否已翻到第N张，如果是则关闭修正窗口
        if (correctionWindowOpen && cardsRevealed >= lastGuessN)
        {
            correctionWindowOpen = false;
            OnCorrectionWindowClosed?.Invoke();
            Debug.Log($"已翻到第{cardsRevealed}张，修正窗口关闭（猜测N={lastGuessN}）");
        }
    }

    /// <summary>
    /// 尝试使用修正
    /// </summary>
    public bool UseCorrection(int newGuessN)
    {
        // 检查是否可以进行修正
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

        // 检查筹码是否足够
        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < correctionCost)
        {
            OnCorrectionFailed?.Invoke($"筹码不足！需要{correctionCost}筹码，当前仅有{currentChips}筹码");
            return false;
        }

        // 消耗筹码
        chipsManager?.AddChips(-correctionCost);

        // 记录旧猜测
        int oldGuessN = lastGuessN;

        // 更新猜测
        lastGuessN = newGuessN;
        remainingCorrections--;

        // 通知结算管理器更新猜测
        settlementManager?.SetLastGuess(newGuessN);

        // 检查修正窗口是否需要更新
        // 如果新猜测大于已翻牌数，窗口继续开放
        // 如果新猜测小于等于已翻牌数，窗口立即关闭
        if (cardsRevealed >= lastGuessN)
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

    /// <summary>
    /// 增加修正次数（用于底牌效果）
    /// </summary>
    public void AddCorrectionChances(int additionalChances)
    {
        remainingCorrections += additionalChances;
        Debug.Log($"修正次数增加{additionalChances}，当前总次数: {remainingCorrections}");
    }

    /// <summary>
    /// 设置修正消耗（用于底牌效果）
    /// </summary>
    public void SetCorrectionCost(int newCost)
    {
        correctionCost = Mathf.Max(0, newCost);
        Debug.Log($"修正消耗变更为: {correctionCost}");
    }

    /// <summary>
    /// 扩展修正窗口（用于超时修正底牌）
    /// </summary>
    public void ExtendCorrectionWindow(bool extend)
    {
        if (extend)
        {
            correctionWindowOpen = true;
            Debug.Log("修正窗口已扩展，可在翻过N后继续修正");
        }
    }

    /// <summary>
    /// 设置扩展修正窗口的消耗（配合超时修正底牌）
    /// </summary>
    public void SetExtendedCorrectionCost(int cost)
    {
        correctionCost = Mathf.Max(0, cost);
    }

    /// <summary>
    /// 获取当前修正状态信息
    /// </summary>
    public CorrectionStatus GetStatus()
    {
        return new CorrectionStatus
        {
            currentGuess = lastGuessN,
            remainingCorrections = remainingCorrections,
            correctionCost = correctionCost,
            cardsRevealed = cardsRevealed,
            windowOpen = correctionWindowOpen,
            canCorrect = hasGuessed && remainingCorrections > 0 && correctionWindowOpen
        };
    }

    /// <summary>
    /// 获取最后一次猜测
    /// </summary>
    public int GetLastGuess()
    {
        return lastGuessN;
    }

    /// <summary>
    /// 是否可以进行修正
    /// </summary>
    public bool CanCorrect()
    {
        return hasGuessed && remainingCorrections > 0 && correctionWindowOpen;
    }
}

/// <summary>
/// 修正状态数据结构
/// </summary>
[System.Serializable]
public struct CorrectionStatus
{
    public int currentGuess;           // 当前猜测N
    public int remainingCorrections;   // 剩余修正次数
    public int correctionCost;         // 当前修正消耗
    public int cardsRevealed;          // 已翻牌数
    public bool windowOpen;            // 修正窗口是否开放
    public bool canCorrect;            // 当前是否可以修正
}