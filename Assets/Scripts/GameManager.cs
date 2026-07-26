using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 游戏主循环 - 串联翻牌→检测→结算→推进流程
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private CorrectionManager correctionManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private RuleManager ruleManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private ChipsManager chipsManager;

    [Header("游戏状态")]
    private bool isWaitingForGuess;
    private bool isDrawingPhase;
    private bool isGameOver;
    private bool targetAchieved;
    private bool secondTargetAchieved;

    public UnityEvent<string> OnGameMessage;
    public UnityEvent OnWaitingForInput;
    public UnityEvent<Card> OnCardDrawn;
    public UnityEvent OnTargetAchieved;
    public UnityEvent<string> OnStageEnd;

    void Awake()
    {
        OnGameMessage ??= new UnityEvent<string>();
        OnWaitingForInput ??= new UnityEvent();
        OnCardDrawn ??= new UnityEvent<Card>();
        OnTargetAchieved ??= new UnityEvent();
        OnStageEnd ??= new UnityEvent<string>();

        if (chipsManager != null)
            chipsManager.OnBankrupt.AddListener(OnBankrupt);
    }

    void Start()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        isGameOver = false;
        chipsManager.ResetChips();
        strategyCardManager.ResetAllForNewGame();
        levelManager.ResetAllProgress();
        ruleManager.ResetAll();
        StartNewStage();
    }

    private void StartNewStage()
    {
        targetAchieved = false;
        secondTargetAchieved = false;

        levelManager.StartNewStage();

        var (layer, stage) = levelManager.GetCurrentStageInfo();
        ruleManager.InitializeForStage(layer, stage);

        strategyCardManager.ResetAllForNewStage();
        correctionManager.ResetCorrections();
        settlementManager.ResetSettlement();

        shopManager.RefreshShopForNewStage();

        OnGameMessage?.Invoke($"第{layer}层 第{stage}关");
        EnterGuessPhase();
    }

    private void EnterGuessPhase()
    {
        isWaitingForGuess = true;
        isDrawingPhase = false;
        OnWaitingForInput?.Invoke();
        OnGameMessage?.Invoke("请给出你的猜测数字 N");
    }

    public void SubmitGuess(int guessN)
    {
        if (!isWaitingForGuess || isGameOver) return;

        if (!correctionManager.MakeInitialGuess(guessN))
            return;

        isWaitingForGuess = false;
        isDrawingPhase = true;
        OnGameMessage?.Invoke($"猜测 N={guessN}，开始翻牌");
    }

    public void UseCorrection(int newGuessN)
    {
        if (!isDrawingPhase || isGameOver) return;
        correctionManager.UseCorrection(newGuessN);
    }

    public void DrawCard()
    {
        if (!isDrawingPhase || isGameOver) return;
        if (targetAchieved && !ruleManager.IsDoubleTargetMode()) return;
        if (targetAchieved && secondTargetAchieved) return;

        bool isFaded = ruleManager.ShouldDrawFadedCard();
        Card card = deck.DrawTopCard(isFaded);

        if (card == null)
        {
            ForceSettle();
            return;
        }

        correctionManager.OnCardRevealed();
        ruleManager.OnCardDrawn(isFaded);
        OnCardDrawn?.Invoke(card);

        CheckTargetAchieved();
    }

    private void CheckTargetAchieved()
    {
        var drawnCards = deck.GetDrawnCards();

        if (ruleManager.IsDoubleTargetMode())
        {
            if (!targetAchieved)
            {
                var primaryTarget = targetHandManager.GetCurrentTarget();
                targetAchieved = primaryTarget != null && primaryTarget.checkCondition(drawnCards);
                if (targetAchieved)
                {
                    OnGameMessage?.Invoke($"目标一 [{primaryTarget.handName}] 达成！");
                }
            }

            if (!secondTargetAchieved)
            {
                var secondTarget = ruleManager.GetSecondTarget();
                secondTargetAchieved = secondTarget != null && secondTarget.checkCondition(drawnCards);
                if (secondTargetAchieved)
                {
                    OnGameMessage?.Invoke($"目标二 [{secondTarget.handName}] 达成！");
                }
            }

            if (targetAchieved && secondTargetAchieved)
            {
                OnTargetAchieved?.Invoke();
                Settle(deck.GetDrawnCount());
            }
        }
        else
        {
            if (!targetAchieved)
            {
                var target = targetHandManager.GetCurrentTarget();
                targetAchieved = target != null && target.checkCondition(drawnCards);
                if (targetAchieved)
                {
                    OnTargetAchieved?.Invoke();
                    Settle(deck.GetDrawnCount());
                }
            }
        }
    }

    private void Settle(int achievedAtCardCount)
    {
        int lastGuess = correctionManager.GetLastGuess();
        int error = Mathf.Abs(lastGuess - achievedAtCardCount);
        bool isEarly = lastGuess < achievedAtCardCount;

        int bestError = error;
        bool bestIsEarly = isEarly;
        if (strategyCardManager.IsKeepPreviousGuess())
        {
            int prevGuess = strategyCardManager.GetPreviousGuess();
            int prevError = Mathf.Abs(prevGuess - achievedAtCardCount);
            bool prevIsEarly = prevGuess < achievedAtCardCount;
            if (prevError < error)
            {
                bestError = prevError;
                bestIsEarly = prevIsEarly;
                OnGameMessage?.Invoke($"保留猜测误差更小，使用修正前猜测 N={prevGuess}");
            }
        }

        int errorKey = Mathf.Min(bestError, 5);
        var errorTable = new Dictionary<int, (int early, int late)>
        {
            { 0, (60, 60) },
            { 1, (30, 20) },
            { 2, (-20, -40) },
            { 3, (-40, -60) },
            { 4, (-60, -80) },
            { 5, (-80, -100) }
        };

        var (earlyVal, lateVal) = errorTable[errorKey];
        int chipChange = bestIsEarly ? earlyVal : lateVal;

        // 1. 应用容错
        if (strategyCardManager.IsWithinTolerance(bestError))
        {
            chipChange = 0;
            OnGameMessage?.Invoke($"误差{bestError}在容错范围内，不扣不加");
        }

        // 2. 应用结算倍率
        float multiplier = strategyCardManager.GetSettlementMultiplier();
        chipChange = Mathf.RoundToInt(chipChange * multiplier);

        // 3. 应用全押
        chipChange = strategyCardManager.ApplyAllInSettlement(chipChange);

        // 4. 应用亏损封顶
        chipChange = strategyCardManager.ApplyLossCap(chipChange);

        // 5. 应用筹码变动
        chipsManager.AddChips(chipChange);

        string direction = chipChange >= 0 ? "+" : "";
        OnGameMessage?.Invoke($"结算：猜测N={lastGuess}，实际{achievedAtCardCount}张，误差{bestError}，筹码{direction}{chipChange}");

        EndStage();
    }

    private void ForceSettle()
    {
        int drawnCount = deck.GetDrawnCount();
        OnGameMessage?.Invoke("牌堆耗尽！强制结算");
        Settle(drawnCount);
    }

    private void EndStage()
    {
        isDrawingPhase = false;

        if (isGameOver) return;

        targetHandManager.MarkCurrentTargetAsCompleted();

        var (layer, stage) = levelManager.GetCurrentStageInfo();
        OnStageEnd?.Invoke($"第{layer}层第{stage}关结束");

        if (layer == 4 && stage == 3)
        {
            OnGameMessage?.Invoke("恭喜！全部通关！");
            isGameOver = true;
        }
        else
        {
            levelManager.CompleteCurrentStage();
            StartNewStage();
        }
    }

    public bool UseStrategyCard(string cardName)
    {
        if (isGameOver) return false;
        return strategyCardManager.UseCard(cardName);
    }

    public bool BuyStrategyCard(int slotIndex)
    {
        if (isGameOver) return false;
        return shopManager.BuyCard(slotIndex);
    }

    public bool SellStrategyCard(string cardName)
    {
        if (isGameOver) return false;
        return shopManager.SellCard(cardName);
    }

    public bool RefreshShop()
    {
        if (isGameOver) return false;
        return shopManager.RefreshShop();
    }

    private void OnBankrupt()
    {
        isGameOver = true;
        isDrawingPhase = false;
        isWaitingForGuess = false;
        OnGameMessage?.Invoke("筹码归零，游戏结束");
    }

    public GameState GetCurrentState()
    {
        return new GameState
        {
            chips = chipsManager != null ? chipsManager.GetChips() : 0,
            drawnCards = deck != null ? deck.GetDrawnCards() : new List<Card>(),
            remainingCount = deck != null ? deck.GetRemainingCount() : 0,
            drawnCount = deck != null ? deck.GetDrawnCount() : 0,
            currentGuess = correctionManager != null ? correctionManager.GetLastGuess() : 0,
            canCorrect = correctionManager != null && correctionManager.CanCorrect(),
            remainingCorrections = correctionManager != null ? correctionManager.GetStatus().remainingCorrections : 0,
            isWaitingForGuess = this.isWaitingForGuess,
            isDrawingPhase = this.isDrawingPhase,
            isGameOver = this.isGameOver,
            shopItems = shopManager != null ? shopManager.GetCurrentShopItems() : new List<StrategyCard>(),
            ownedCards = strategyCardManager != null ? strategyCardManager.GetOwnedCards() : new List<StrategyCard>(),
            currentTarget = targetHandManager != null ? targetHandManager.GetCurrentTarget() : null,
            secondTarget = ruleManager != null ? ruleManager.GetSecondTarget() : null,
            targetAchieved = this.targetAchieved,
            secondTargetAchieved = this.secondTargetAchieved,
        };
    }
}

[System.Serializable]
public struct GameState
{
    public int chips;
    public List<Card> drawnCards;
    public int remainingCount;
    public int drawnCount;
    public int currentGuess;
    public bool canCorrect;
    public int remainingCorrections;
    public bool isWaitingForGuess;
    public bool isDrawingPhase;
    public bool isGameOver;
    public List<StrategyCard> shopItems;
    public List<StrategyCard> ownedCards;
    public TargetHand currentTarget;
    public TargetHand secondTarget;
    public bool targetAchieved;
    public bool secondTargetAchieved;
}