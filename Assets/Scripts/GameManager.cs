using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    [SerializeField] private GameConfigSO config;

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

    // 稳扎稳打连续触发计数
    private int steadyStreak;

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
        steadyStreak = 0;
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

        // 天启效果：自动看顶部44张
        if (strategyCardManager.HasApocalypse())
        {
            UseStrategyCard("天启");
        }

        // 预览效果
        if (strategyCardManager.IsPreviewEnabled())
        {
            bool hits = strategyCardManager.ExecutePreview();
            if (hits)
            {
                OnGameMessage?.Invoke("预览命中！0误差获胜！");
                targetAchieved = true;
                Settle(deck.GetDrawnCount() + strategyCardManager.GetPreviewCount());
                return;
            }
        }

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
        bool wasCorrection = correctionManager.GetLastGuess() > 0;
        correctionManager.UseCorrection(newGuessN);

        // 修正艺术家检查在结算时处理
    }

    public void DrawCard()
    {
        if (!isDrawingPhase || isGameOver) return;
        if (targetAchieved) return;

        if (ruleManager.IsLayer3LimitReached(deck.GetDrawnCount()))
        {
            OnGameMessage?.Invoke($"已翻{deck.GetDrawnCount()}张未达成目标，游戏失败！");
            HandleStageFailure();
            return;
        }

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
        int error = Mathf.Abs(lastGuess - achievedAtCardCount);
        int layer = levelManager.GetCurrentStageInfo().layer;
        int baseTolerance = config != null ? config.GetErrorTolerance(layer) : 5;
        int toleranceBonus = strategyCardManager.GetErrorToleranceBonus()
                            + strategyCardManager.GetFateWheelBonus();
        int tolerance = baseTolerance + toleranceBonus;

        // 孤注一掷模式：容忍度强制为0
        if (strategyCardManager.IsAllInMode())
        {
            tolerance = 0;
        }

        if (error > tolerance)
        {
            if (strategyCardManager.HasDeathDefy())
            {
                strategyCardManager.ConsumeDeathDefy();
                chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
                OnGameMessage?.Invoke("不死鸟触发！视为存活，无筹码奖励");
                EndStage(true);
                return;
            }

            OnGameMessage?.Invoke($"游戏失败！误差{error} > 容忍度{tolerance}");
            HandleStageFailure();
            return;
        }

        int extraChips = 0;

        // 误差为0额外+20
        if (error == 0)
        {
            int zeroBonus = config != null ? config.zeroErrorBonus : 20;
            zeroBonus += strategyCardManager.GetZeroErrorBonus();
            extraChips += zeroBonus;

            // 完美风暴倍数
            float mult = strategyCardManager.GetPerfectMultiplier();
            if (mult > 1f) extraChips = Mathf.RoundToInt(extraChips * mult);

            // 孤注一掷x5
            if (strategyCardManager.IsAllInMode())
            {
                extraChips = extraChips * 5;
                OnGameMessage?.Invoke("孤注一掷！收入x5！");
            }

            OnGameMessage?.Invoke($"完美猜测！+{extraChips}");

            // 修正艺术家累计
            if (strategyCardManager.HasCard("修正艺术家") && correctionManager.GetLastGuess() > 0)
            {
                strategyCardManager.AddAccumulatedValue("修正艺术家", 1);
                Debug.Log($"[修正艺术家] 累计{strategyCardManager.GetAccumulatedValue("修正艺术家")}次");
            }
        }

        // 误差=1额外+5
        if (error == 1)
        {
            int oneBonus = config != null ? config.oneErrorBonus : 5;
            int nearBonus = strategyCardManager.GetNearErrorBonus();
            extraChips += oneBonus + nearBonus;
            OnGameMessage?.Invoke($"误差为1！+{oneBonus + nearBonus}");
        }

        // 偏差大师：误差≥3且存活
        if (error >= 3 && strategyCardManager.HasCard("偏差大师"))
        {
            strategyCardManager.AddAccumulatedValue("偏差大师", 1);
            int bonus = strategyCardManager.GetDeviationMasterBonus();
            extraChips += bonus;
            OnGameMessage?.Invoke($"偏差大师 +{bonus}");
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

        // 稳扎稳打
        if (error <= 2 && strategyCardManager.HasCard("稳扎稳打"))
        {
            steadyStreak++;
            strategyCardManager.AddAccumulatedValue("稳扎稳打", 1);
            int bonus = strategyCardManager.GetSteadyBonus();
            extraChips += bonus;
            OnGameMessage?.Invoke($"稳扎稳打 +{bonus}，连续{steadyStreak}局");
        }
        else
        {
            if (strategyCardManager.HasCard("稳扎稳打"))
            {
                steadyStreak = 0;
                strategyCardManager.ResetAccumulatedValue("稳扎稳打");
                OnGameMessage?.Invoke("稳扎稳打中断，累计重置");
            }
        }

        // 速攻
        if (achievedAtCardCount <= 15 && error <= 1 && strategyCardManager.HasCard("速攻"))
        {
            strategyCardManager.AddAccumulatedValue("速攻", 1);
            int bonus = strategyCardManager.GetSpeedRunBonus();
            extraChips += bonus;
            OnGameMessage?.Invoke($"速攻 +{bonus}");
        }

        // 存活奖励+10
        chipsManager.AwardSurviveBonus();

        if (extraChips != 0)
            chipsManager.AddChips(extraChips);

        OnGameMessage?.Invoke($"结算：猜测N={lastGuess}，实际{achievedAtCardCount}张，误差{error}，额外筹码{(extraChips >= 0 ? "+" : "")}{extraChips}");

        EndStage(true);
    }

    private void HandleStageFailure()
    {
        if (strategyCardManager.HasDeathDefy())
        {
            strategyCardManager.ConsumeDeathDefy();
            chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
            OnGameMessage?.Invoke("不死鸟触发！视为存活");
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

        int totalLayers = config != null ? config.totalLayers : 5;
        if (layer == totalLayers && stage >= 1)
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

    public bool RevealFadedCard(int cardIndex)
    {
        if (isGameOver) return false;
        return ruleManager.RevealFadedCard(cardIndex, chipsManager);
    }

    private void OnBankrupt()
    {
        if (strategyCardManager.HasDeathDefy())
        {
            strategyCardManager.ConsumeDeathDefy();
            chipsManager.SetChips(1);
            OnGameMessage?.Invoke("不死鸟触发！筹码保留1");
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