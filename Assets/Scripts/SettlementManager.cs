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

    // 结算时收集的所有被动加成
    private struct SettlementModifiers
    {
        public int toleranceBonus;
        public int earlyBirdBonus;
        public int closeBonus;
        public int zeroBonus;
        public int speedRunBonus;
        public int speedRunStack;
        public int deviationBonus;
        public int deviationStack;
        public int steadyBonus;
        public int steadyStack;
        public bool steadyReset;
        public int correctionArtistBonus;
        public int correctionArtistStack;
        public bool hasHighStakes;
        public bool hasPhoenix;
    }

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
        tolerance += GetModifiers(new SettlementContext { error = 0, actual = 0, stage = stage, usedCorrection = false }).toleranceBonus;
        return tolerance;
    }

    public SettlementResult Settle(SettlementContext context)
    {
        SettlementResult result = new SettlementResult();

        int error = forceZeroError ? 0 : context.error;
        forceZeroError = false;

        result.error = error;
        result.tolerance = defaultTolerance.ContainsKey(context.stage) ? defaultTolerance[context.stage] : 5;

        var mods = GetModifiers(new SettlementContext
        {
            error = error,
            actual = context.actual,
            stage = context.stage,
            usedCorrection = context.usedCorrection
        });

        result.tolerance += mods.toleranceBonus;
        result.isSurvive = result.error <= result.tolerance;

        if (result.isSurvive)
        {
            result.baseReward = GetBaseReward(result.error);
            result.surviveBonus = 10;
            result.passiveBonus = mods.earlyBirdBonus + mods.closeBonus + mods.zeroBonus
                                + mods.deviationBonus + mods.steadyBonus
                                + mods.speedRunBonus + mods.correctionArtistBonus;
            result.totalReward = result.baseReward + result.surviveBonus + result.passiveBonus;

            ApplyStacks(mods);
        }
        else
        {
            if (mods.hasPhoenix)
            {
                result.isSurvive = true;
                result.totalReward = 0;
                result.savedFromFail = true;
            }
        }

        return result;
    }

    private int GetBaseReward(int error)
    {
        if (error == 0) return 20;
        if (error == 1) return 5;
        return 0;
    }

    private SettlementModifiers GetModifiers(SettlementContext ctx)
    {
        SettlementModifiers mods = new SettlementModifiers();
        SC_DeviationMaster dm = null;

        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_ToleranceUp t)
                mods.toleranceBonus += t.ToleranceBonus;
            else if (card is SC_HighStakes h)
            {
                mods.toleranceBonus += h.ToleranceBonus;
                mods.hasHighStakes = true;
            }
            else if (card is SC_Absolution a)
                mods.toleranceBonus += a.ToleranceBonus;
            else if (card is SC_EarlyBird early)
            {
                if (ctx.actual <= early.Threshold)
                    mods.earlyBirdBonus += early.Bonus;
            }
            else if (card is SC_CloseBonus close)
            {
                if (close.RequireExactOne && ctx.error == 1)
                    mods.closeBonus += close.Bonus;
                else if (!close.RequireExactOne && ctx.error <= 1)
                    mods.closeBonus += close.Bonus;
            }
            else if (card is SC_DeviationMaster d)
            {
                dm = d;
                if (ctx.error >= 3)
                {
                    mods.deviationBonus += d.BaseBonus + d.accumulatedBonus;
                    mods.deviationStack = d.StackPerTrigger;
                }
            }
            else if (card is SC_Steady steady)
            {
                if (ctx.error <= 2)
                {
                    mods.steadyBonus += steady.BaseBonus + steady.streak * steady.StackPerStreak;
                    mods.steadyStack = steady.StackPerStreak;
                }
                else
                {
                    mods.steadyReset = true;
                }
            }
            else if (card is SC_ZeroBonus zero)
            {
                if (zero.RequireExactZero && ctx.error == 0)
                    mods.zeroBonus += zero.Bonus;
                else if (!zero.RequireExactZero && ctx.error <= 1)
                    mods.zeroBonus += zero.Bonus;
            }
            else if (card is SC_SpeedRun speed)
            {
                if (ctx.actual <= speed.Threshold && ctx.error <= 1)
                {
                    mods.speedRunBonus += speed.BaseBonus + speed.accumulatedBonus;
                    mods.speedRunStack = speed.StackPerTrigger;
                }
            }
            else if (card is SC_CorrectionArtist artist)
            {
                if (ctx.usedCorrection)
                {
                    if (artist.RequireExactZero && ctx.error != 0) continue;
                    if (!artist.RequireExactZero && ctx.error > 1) continue;
                    mods.correctionArtistBonus += artist.BaseBonus + artist.accumulatedBonus;
                    mods.correctionArtistStack = artist.StackPerTrigger;
                }
            }
            else if (card is SC_Phoenix phoenix)
            {
                mods.hasPhoenix = !phoenix.isGoneForever;
            }
        }

        // 豪赌翻倍
        if (mods.hasHighStakes && dm != null && ctx.error >= 3)
            mods.deviationBonus += dm.BaseBonus + dm.accumulatedBonus;

        return mods;
    }

    // 成长牌
    private void ApplyStacks(SettlementModifiers mods)
    {
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_DeviationMaster dm && mods.deviationStack > 0)
                dm.accumulatedBonus += mods.deviationStack;
            else if (card is SC_Steady steady)
            {
                if (mods.steadyReset)
                    steady.streak = 0;
                else if (mods.steadyStack > 0)
                    steady.streak++;
            }
            else if (card is SC_SpeedRun speed && mods.speedRunStack > 0)
                speed.accumulatedBonus += mods.speedRunStack;
            else if (card is SC_CorrectionArtist artist && mods.correctionArtistStack > 0)
                artist.accumulatedBonus += mods.correctionArtistStack;
            else if (card is SC_Phoenix phoenix && mods.hasPhoenix)
                phoenix.isGoneForever = true;
        }
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