using System.Collections.Generic;
using UnityEngine;

public class SettlementManager : MonoBehaviour
{
    public static SettlementManager Instance { get; private set; }

    private Dictionary<int, int> defaultTolerance = new Dictionary<int, int>
    {
        {1, 5}, {2, 4}, {3, 3}, {4, 2}, {5, 0}
    };

    public bool forceZeroError { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ForceZeroError()
    {
        forceZeroError = true;
    }

    public int GetTolerance(int stage)
    {
        int tolerance = defaultTolerance.ContainsKey(stage) ? defaultTolerance[stage] : 5;

        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_ToleranceUp t)
                tolerance += t.ToleranceBonus;
            else if (card is SC_HighStakes h)
                tolerance += h.ToleranceBonus;
            else if (card is SC_Absolution a)
                tolerance += a.ToleranceBonus;
        }

        return tolerance;
    }

    private int GetBaseReward(int error)
    {
        if (error == 0) return 20;
        if (error == 1) return 5;
        return 0;
    }

    public SettlementResult Settle(SettlementContext context)
    {
        SettlementResult result = new SettlementResult();

        if (forceZeroError)
        {
            result.error = 0;
            result.tolerance = GetTolerance(context.stage);
            result.isSurvive = true;
            result.baseReward = GetBaseReward(0);
            result.surviveBonus = 10;
            result.passiveBonus = CalculatePassiveBonus(new SettlementContext
            {
                guess = context.guess,
                actual = context.actual,
                stage = context.stage,
                error = 0,
                usedCorrection = context.usedCorrection
            });
            result.totalReward = result.baseReward + result.surviveBonus + result.passiveBonus;
            forceZeroError = false;
            return result;
        }

        result.error = context.error;
        result.tolerance = GetTolerance(context.stage);
        result.isSurvive = result.error <= result.tolerance;

        if (result.isSurvive)
        {
            result.baseReward = GetBaseReward(result.error);
            result.surviveBonus = 10;
            result.passiveBonus = CalculatePassiveBonus(context);
            result.totalReward = result.baseReward + result.surviveBonus + result.passiveBonus;
        }
        else
        {
            if (TryFailSave())
            {
                result.isSurvive = true;
                result.totalReward = 0;
                result.savedFromFail = true;
            }
        }

        return result;
    }

    private int CalculatePassiveBonus(SettlementContext context)
    {
        int bonus = 0;

        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_EarlyBird early)
            {
                if (context.actual <= early.Threshold)
                    bonus += early.Bonus;
            }
            else if (card is SC_CloseBonus close)
            {
                if (close.RequireExactOne && context.error == 1)
                    bonus += close.Bonus;
                else if (!close.RequireExactOne && context.error <= 1)
                    bonus += close.Bonus;
            }
            else if (card is SC_DeviationMaster dm)
            {
                if (context.error >= 3)
                {
                    int reward = dm.BaseBonus + dm.accumulatedBonus;
                    dm.accumulatedBonus += dm.StackPerTrigger;
                    bonus += reward;

                    if (HasCard<SC_HighStakes>())
                        bonus += reward;
                }
            }
            else if (card is SC_Steady steady)
            {
                if (context.error <= 2)
                {
                    int reward = steady.BaseBonus + steady.streak * steady.StackPerStreak;
                    steady.streak++;
                    bonus += reward;
                }
                else
                {
                    steady.streak = 0;
                }
            }
            else if (card is SC_ZeroBonus zero)
            {
                if (zero.RequireExactZero && context.error == 0)
                    bonus += zero.Bonus;
                else if (!zero.RequireExactZero && context.error <= 1)
                    bonus += zero.Bonus;
            }
            else if (card is SC_SpeedRun speed)
            {
                if (context.actual <= speed.Threshold && context.error <= 1)
                {
                    int reward = speed.BaseBonus + speed.accumulatedBonus;
                    speed.accumulatedBonus += speed.StackPerTrigger;
                    bonus += reward;
                }
            }
            else if (card is SC_CorrectionArtist artist)
            {
                if (!context.usedCorrection) continue;
                if (artist.RequireExactZero && context.error != 0) continue;
                if (!artist.RequireExactZero && context.error > 1) continue;

                int reward = artist.BaseBonus + artist.accumulatedBonus;
                artist.accumulatedBonus += artist.StackPerTrigger;
                bonus += reward;
            }
        }

        return bonus;
    }

    private bool TryFailSave()
    {
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_Phoenix phoenix)
            {
                if (!phoenix.isGoneForever)
                {
                    phoenix.isGoneForever = true;
                    return true;
                }
            }
        }
        return false;
    }

    private bool HasCard<T>() where T : StrategyCard
    {
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is T) return true;
        }
        return false;
    }
}

public class SettlementContext
{
    public int guess;
    public int actual;
    public int stage;
    public int error;
    public bool usedCorrection;
}

public class SettlementResult
{
    public int error;
    public int tolerance;
    public bool isSurvive;
    public int baseReward;
    public int surviveBonus;
    public int passiveBonus;
    public int totalReward;
    public bool savedFromFail;
}