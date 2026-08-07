using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private GameConfigSO config;

    [Header("组件")]
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

    private int steadyStreak;

    public StrategyCardManager StrategyCardManager => strategyCardManager;

    void Awake()
    {
        OnGameMessage ??= new UnityEvent<string>();
        OnWaitingForInput ??= new UnityEvent();
        OnCardDrawn ??= new UnityEvent<Card>();
        OnTargetAchieved ??= new UnityEvent();
        OnStageEnd ??= new UnityEvent<string>();
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

        if (strategyCardManager.HasApocalypse())
        {
            UseStrategyCard("天启");
        }

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
        int toleranceBonus = strategyCardManager.GetErrorToleranceBonus() + strategyCardManager.GetFateWheelBonus();
        int tolerance = baseTolerance + toleranceBonus;

        if (strategyCardManager.IsAllInMode()) tolerance = 0;

        if (error > tolerance)
        {
            if (strategyCardManager.HasDeathDefy())
            {
                strategyCardManager.ConsumeDeathDefy();
                chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
                OnGameMessage?.Invoke("不死鸟触发！视为存活");
                OnStageEnd?.Invoke("不死鸟触发！存活");
                return;
            }

            OnGameMessage?.Invoke($"游戏失败！误差{error} > 容忍度{tolerance}");
            HandleStageFailure();
            return;
        }

        int extraChips = 0;
        string bonusDetail = "";

        chipsManager.AwardSurviveBonus();
        bonusDetail += "存活 +10\n";

        if (error == 0)
        {
            int zeroBonus = (config != null ? config.zeroErrorBonus : 20) + strategyCardManager.GetZeroErrorBonus();
            float mult = strategyCardManager.GetPerfectMultiplier();
            if (mult > 1f) zeroBonus = Mathf.RoundToInt(zeroBonus * mult);
            if (strategyCardManager.IsAllInMode()) zeroBonus *= 5;
            extraChips += zeroBonus;
            bonusDetail += $"完美猜测 +{zeroBonus}\n";

            if (strategyCardManager.HasCard("修正艺术家") && correctionManager.GetLastGuess() > 0)
                strategyCardManager.AddAccumulatedValue("修正艺术家", 1);
        }

        if (error == 1)
        {
            int oneBonus = (config != null ? config.oneErrorBonus : 5) + strategyCardManager.GetNearErrorBonus();
            extraChips += oneBonus;
            bonusDetail += $"误差1红利 +{oneBonus}\n";
        }

        if (error >= 3 && strategyCardManager.HasCard("偏差大师"))
        {
            strategyCardManager.AddAccumulatedValue("偏差大师", 1);
            int bonus = strategyCardManager.GetDeviationMasterBonus();
            extraChips += bonus;
            bonusDetail += $"偏差大师 +{bonus}\n";
        }

        if (achievedAtCardCount <= strategyCardManager.GetEarlyBirdThreshold())
        {
            int earlyBonus = strategyCardManager.GetEarlyBirdBonus();
            if (earlyBonus > 0)
            {
                extraChips += earlyBonus;
                bonusDetail += $"早鸟优惠 +{earlyBonus}\n";
            }
        }

        if (error <= 2 && strategyCardManager.HasCard("稳扎稳打"))
        {
            steadyStreak++;
            strategyCardManager.AddAccumulatedValue("稳扎稳打", 1);
            int bonus = strategyCardManager.GetSteadyBonus();
            extraChips += bonus;
            bonusDetail += $"稳扎稳打 +{bonus}(连续{steadyStreak}局)\n";
        }
        else if (strategyCardManager.HasCard("稳扎稳打"))
        {
            steadyStreak = 0;
            strategyCardManager.ResetAccumulatedValue("稳扎稳打");
        }

        if (achievedAtCardCount <= 15 && error <= 1 && strategyCardManager.HasCard("速攻"))
        {
            strategyCardManager.AddAccumulatedValue("速攻", 1);
            int bonus = strategyCardManager.GetSpeedRunBonus();
            extraChips += bonus;
            bonusDetail += $"速攻 +{bonus}\n";
        }

        if (extraChips != 0) chipsManager.AddChips(extraChips);

        string resultMsg = $"结算：猜测N={lastGuess}，实际{achievedAtCardCount}张\n误差={error}，容忍度={tolerance}\n{bonusDetail}";
        OnGameMessage?.Invoke(resultMsg);
        OnStageEnd?.Invoke(resultMsg);
    }

    private void HandleStageFailure()
    {
        if (strategyCardManager.HasDeathDefy())
        {
            strategyCardManager.ConsumeDeathDefy();
            chipsManager.SetChips(Mathf.Max(1, chipsManager.GetChips()));
            OnGameMessage?.Invoke("不死鸟触发！视为存活");
            OnStageEnd?.Invoke("不死鸟触发！存活");
            return;
        }

        isGameOver = true;
        isDrawingPhase = false;
        OnGameMessage?.Invoke("游戏失败！");
        OnStageEnd?.Invoke("游戏失败！");
    }

    public void ProceedToNextStage()
    {
        if (isGameOver) return;

        var (layer, stage) = levelManager.GetCurrentStageInfo();
        int totalLayers = config != null ? config.totalLayers : 5;

        if (layer == totalLayers && stage >= 1)
        {
            OnGameMessage?.Invoke("恭喜！全部通关！");
            isGameOver = true;
            return;
        }

        levelManager.CompleteCurrentStage();
        StartNewStage();
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

    public int GetCurrentErrorTolerance()
    {
        int layer = levelManager.GetCurrentStageInfo().layer;
        int baseTolerance = config != null ? config.GetErrorTolerance(layer) : 5;
        int bonus = strategyCardManager.GetErrorToleranceBonus() + strategyCardManager.GetFateWheelBonus();
        if (strategyCardManager.IsAllInMode()) return 0;
        return baseTolerance + bonus;
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

    public (int layer, int stage) GetCurrentLayerStage() => levelManager.GetCurrentStageInfo();

    public bool IsCardOwned(string cardName) => strategyCardManager.HasCard(cardName);

    public bool IsCardUpgradable(string cardName)
    {
        var card = strategyCardManager.GetOwnedCards().Find(c => c.cardName == cardName);
        return card != null && card.IsUpgradable;
    }

    public int GetCardUpgradeCost(string cardName)
    {
        var card = strategyCardManager.GetOwnedCards().Find(c => c.cardName == cardName);
        return card != null ? card.GetUpgradeCost() : -1;
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