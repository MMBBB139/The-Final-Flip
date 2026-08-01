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
        if (targetAchieved) return;

        // 第3层特殊：前25张限制
        if (ruleManager.IsLayer3LimitReached(deck.GetDrawnCount()))
        {
            OnGameMessage?.Invoke($"已翻{deck.GetDrawnCount()}张未达成目标，游戏失败！");
            HandleStageFailure();
            return;
        }

        // 第4层特殊：判断是否该翻褪色牌
        bool isFaded = ruleManager.ShouldDrawFadedCard();
        Card card = deck.DrawTopCard(isFaded);

        if (card == null)
        {
            HandleStageFailure();
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
        var target = targetHandManager.GetCurrentTarget();

        if (target != null && target.checkCondition(drawnCards))
        {
            targetAchieved = true;
            OnTargetAchieved?.Invoke();
            Settle(deck.GetDrawnCount());
        }
    }

    private void Settle(int achievedAtCardCount)
    {
        int lastGuess = correctionManager.GetLastGuess();

        // 保留猜测处理
        int bestGuess = lastGuess;
        if (strategyCardManager.IsKeepPreviousGuess())
        {
            int prevGuess = strategyCardManager.GetPreviousGuess();
            int prevError = Mathf.Abs(prevGuess - achievedAtCardCount);
            int currError = Mathf.Abs(lastGuess - achievedAtCardCount);
            if (prevError < currError)
            {
                bestGuess = prevGuess;
                OnGameMessage?.Invoke($"保留猜测误差更小，使用修正前猜测 N={prevGuess}");
            }
        }

        int finalError = Mathf.Abs(bestGuess - achievedAtCardCount);
        int layer = levelManager.GetCurrentStageInfo().layer;
        int baseTolerance = settlementManager.config != null
            ? settlementManager.config.errorToleranceByLayer[Mathf.Min(layer - 1, 3)] : 4;
        int toleranceBonus = strategyCardManager.GetErrorToleranceBonus();
        int tolerance = baseTolerance + toleranceBonus;

        if (finalError > tolerance)
        {
            // 绝处逢生检查
            if (strategyCardManager.HasDeathDefy())
            {
                strategyCardManager.ConsumeDeathDefy();
                chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
                OnGameMessage?.Invoke("绝处逢生触发！视为存活，无筹码奖励");
                EndStage(true);
                return;
            }

            OnGameMessage?.Invoke($"游戏失败！误差{finalError} > 容忍度{tolerance}");
            HandleStageFailure();
            return;
        }

        int extraChips = 0;

        // 误差为0额外+40
        if (finalError == 0)
        {
            int zeroBonus = strategyCardManager.GetZeroErrorBonus();
            extraChips += 40 + zeroBonus;
            OnGameMessage?.Invoke($"完美猜测！+{40 + zeroBonus}");
        }

        // 近误差红利
        if (finalError == 1)
        {
            int nearBonus = strategyCardManager.GetNearErrorBonus();
            if (nearBonus > 0)
            {
                extraChips += nearBonus;
                OnGameMessage?.Invoke($"近误差红利 +{nearBonus}");
            }
        }

        // 早鸟优惠
        if (achievedAtCardCount <= strategyCardManager.GetEarlyBirdThreshold())
        {
            int earlyBonus = strategyCardManager.GetEarlyBirdBonus();
            if (earlyBonus > 0)
            {
                extraChips += earlyBonus;
                OnGameMessage?.Invoke($"早鸟优惠 +{earlyBonus}");
            }
        }

        // 存活奖励+20
        chipsManager.AwardSurviveBonus();

        // 应用额外筹码
        if (extraChips != 0)
            chipsManager.AddChips(extraChips);

        OnGameMessage?.Invoke($"结算：猜测N={bestGuess}，实际{achievedAtCardCount}张，误差{finalError}，额外筹码{(extraChips >= 0 ? "+" : "")}{extraChips}");

        EndStage(true);
    }

    private void HandleStageFailure()
    {
        // 绝处逢生检查
        if (strategyCardManager.HasDeathDefy())
        {
            strategyCardManager.ConsumeDeathDefy();
            chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
            OnGameMessage?.Invoke("绝处逢生触发！视为存活");
            EndStage(true);
            return;
        }

        isGameOver = true;
        isDrawingPhase = false;
        OnGameMessage?.Invoke("游戏结束");
    }

    private void EndStage(bool survived)
    {
        isDrawingPhase = false;

        if (isGameOver) return;

        targetHandManager.MarkCurrentTargetAsCompleted();

        var (layer, stage) = levelManager.GetCurrentStageInfo();
        OnStageEnd?.Invoke($"第{layer}层第{stage}关结束");

        if (!survived)
        {
            isGameOver = true;
            return;
        }

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
        return strategyCardManager.SellCard(cardName);
    }

    public bool RefreshShop()
    {
        if (isGameOver) return false;
        return shopManager.RefreshShop();
    }

    /// <summary>
    /// 恢复褪色牌（第4层特殊规则）
    /// </summary>
    public bool RevealFadedCard(int cardIndex)
    {
        if (isGameOver) return false;
        return ruleManager.RevealFadedCard(cardIndex, chipsManager);
    }

    private void OnBankrupt()
    {
        // 绝处逢生检查
        if (strategyCardManager.HasDeathDefy())
        {
            strategyCardManager.ConsumeDeathDefy();
            chipsManager.SetChips(1);
            OnGameMessage?.Invoke("绝处逢生触发！筹码保留1");
            return;
        }

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
            secondTarget = null,
            targetAchieved = this.targetAchieved,
            secondTargetAchieved = false,
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