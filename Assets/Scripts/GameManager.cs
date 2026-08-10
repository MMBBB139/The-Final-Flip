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

    public void StartGame()
    {
        chips = 30;
        ownedCards.Clear();
        StageManager.Instance.StartStage(1, 1);
    }

    public void StartStage()
    {
        usedCorrectionThisRound = false;
        isGoalAchieved = false;
        actualFlips = 0;

        foreach (var card in ownedCards)
            card.ResetForRound();

        currentGoal = GoalPool.GetRandomGoal(StageManager.Instance.currentStage);

        DeckManager.Instance.BuildDeck();
        DeckManager.Instance.Shuffle();

        currentPhase = GamePhase.Preview;
        StageManager.Instance.RunPreview(this);
    }

    public void OnPreviewComplete(bool directWin)
    {
        if (directWin)
        {
            currentPhase = GamePhase.Result;
            EndStage();
            return;
        }

        currentPhase = GamePhase.Guess;
    }

    public void SubmitGuess(int number)
    {
        if (currentPhase != GamePhase.Guess) return;
        if (number < 1 || number > 52) return;

        guessFlips = number;
        CorrectionManager.Instance.StartRound(guessFlips);
        currentPhase = GamePhase.FlipCards;
    }

    public void Flip()
    {
        if (currentPhase != GamePhase.FlipCards) return;

        int drawCount = GetDrawCount();
        actualFlips += drawCount;

        DeckManager.Instance.Draw(drawCount);

        var result = currentGoal.Check(DeckManager.Instance.drawnCards);
        if (result.isAchieved)
        {
            isGoalAchieved = true;
            StageManager.Instance.isGoalAchieved = true;
        }

        CorrectionManager.Instance.OnFlip(actualFlips, guessFlips);

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
        return 4;
    }

    public void UseStrategyCard(StrategyCard card)
    {
        if (currentPhase != GamePhase.FlipCards) return;

        if (card is ActiveCard active)
        {
            active.Execute();
        }
    }

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
            return;
        }

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

    public void ForceZeroErrorWin()
    {
        SettlementManager.Instance.ForceZeroError();
        guessFlips = DeckManager.Instance.drawnCards.Count;
        actualFlips = guessFlips;
        isGoalAchieved = true;
    }
}