using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int chips = 30;
    public int guessFlips;
    public int actualFlips;
    public bool usedCorrectionThisRound;
    public bool isGoalAchieved;

    public List<StrategyCard> ownedCards = new List<StrategyCard>();
    public GoalChecker currentGoal;

    public enum GamePhase
    {
        Preview,
        Guess,
        FlipCards,
        Result
    }
    public GamePhase currentPhase;

    void Awake() => Instance = this;

    void Start()
    {
        StartGame();
    }

    // ==================== 游戏入口 ====================
    public void StartGame()
    {
        chips = 30;
        ownedCards.Clear();
        StageManager.Instance.StartStage(1, 1);
    }

    // ==================== 每关开始 ====================
    public void StartStage()
    {
        usedCorrectionThisRound = false;
        isGoalAchieved = false;
        actualFlips = 0;

        // 重置主动牌
        foreach (var card in ownedCards)
            card.ResetForRound();

        // 选目标
        currentGoal = GoalPool.GetRandomGoal(StageManager.Instance.currentStage);

        // 洗牌
        DeckManager.Instance.BuildDeck();
        DeckManager.Instance.Shuffle();

        // 预览
        currentPhase = GamePhase.Preview;
        StageManager.Instance.RunPreview(this);
    }

    // 预览完成后回调
    public void OnPreviewComplete(bool directWin)
    {
        if (directWin)
        {
            // ForceZeroErrorWin 已经设了 forceZeroError，直接结算
            currentPhase = GamePhase.Result;
            EndStage();
            return;
        }

        currentPhase = GamePhase.Guess;
    }

    // ==================== 猜测 ====================
    public void SubmitGuess(int number)
    {
        if (currentPhase != GamePhase.Guess) return;
        if (number < 1 || number > 52) return;

        guessFlips = number;
        CorrectionManager.Instance.StartRound(guessFlips);
        currentPhase = GamePhase.FlipCards;
    }

    // ==================== 翻牌 ====================
    public void Flip()
    {
        if (currentPhase != GamePhase.FlipCards) return;

        int drawCount = GetDrawCount();
        actualFlips += drawCount;

        DeckManager.Instance.Draw(drawCount);

        // 检查目标
        var result = currentGoal.Check(DeckManager.Instance.drawnCards);
        if (result.isAchieved)
        {
            isGoalAchieved = true;
            StageManager.Instance.isGoalAchieved = true;
        }

        // 层级规则
        StageManager.Instance.OnFlip(actualFlips, guessFlips);
        CorrectionManager.Instance.OnFlip(actualFlips, guessFlips);

        // 失败检查
        if (StageManager.Instance.CheckFailCondition(actualFlips))
        {
            currentPhase = GamePhase.Result;
            EndStage();
            return;
        }

        // 牌堆翻完
        if (DeckManager.Instance.deck.Count == 0)
        {
            currentPhase = GamePhase.Result;
            EndStage();
        }
    }

    private int GetDrawCount()
    {
        foreach (var card in ownedCards)
        {
            if (card is SC_ControlSpeed cs)
                return cs.DrawCount;
        }
        return 4; // 默认一次翻4张
    }

    // ==================== 策略牌 ====================
    public void UseStrategyCard(StrategyCard card)
    {
        if (currentPhase != GamePhase.FlipCards) return;

        if (card is ActiveCard active)
        {
            if (active.Execute())
                StageManager.Instance.OnStrategyUsed(card);
        }
    }

    // ==================== 修正 ====================
    public void UseCorrection(int newGuess)
    {
        if (currentPhase != GamePhase.FlipCards) return;

        string errorMsg;
        if (CorrectionManager.Instance.UseCorrection(newGuess, out errorMsg))
        {
            guessFlips = newGuess;
            usedCorrectionThisRound = true;
        }
        else
        {
            Debug.Log(errorMsg);
        }
    }

    // ==================== 结算 ====================
    public void EndStage()
    {
        currentPhase = GamePhase.Result;

        var context = new SettlementContext
        {
            guess = guessFlips,
            actual = actualFlips,
            stage = StageManager.Instance.currentStage,
            error = Mathf.Abs(guessFlips - actualFlips),
            usedCorrection = usedCorrectionThisRound
        };

        var result = SettlementManager.Instance.Settle(context);

        if (result.isSurvive)
        {
            chips += result.totalReward;

            if (result.savedFromFail)
                Debug.Log("不死鸟救了你！存活，无奖励。");
            else
                Debug.Log($"存活！误差{result.error}，获得{result.totalReward}筹码。当前筹码：{chips}");
        }
        else
        {
            Debug.Log($"失败！误差{result.error}，容忍度{result.tolerance}。游戏结束。");
            // TODO: 游戏结束逻辑
            return;
        }

        // 下一关或商店
        if (ShouldOpenShop())
        {
            OpenShop();
        }
        else
        {
            StageManager.Instance.NextLevel();
            StartStage();
        }
    }

    private bool ShouldOpenShop()
    {
        // 每层第3关后开商店，第5层只有1关，结束后不开
        return StageManager.Instance.currentLevel == 3 && StageManager.Instance.currentStage < 5;
    }

    public void OpenShop()
    {
        ShopManager.Instance.OpenShop(ownedCards, StageManager.Instance.currentStage);
    }

    public void OnShopClosed()
    {
        StageManager.Instance.NextLevel();
        StartStage();
    }

    // ==================== 0误差直接获胜 ====================
    public void ForceZeroErrorWin()
    {
        SettlementManager.Instance.ForceZeroError();
        guessFlips = DeckManager.Instance.drawnCards.Count;
        actualFlips = guessFlips;
        isGoalAchieved = true;
    }
}