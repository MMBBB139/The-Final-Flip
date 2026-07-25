// StrategyCardDefinitions.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class StrategyCardDefinitions
{
    public static List<StrategyCard> CreateAll()
    {
        return new List<StrategyCard>
        {
            Create("偷看顶牌", "查看牌堆顶部2张", 25, 2, 20, mgr =>
            {
                int count = mgr.GetCardLevel("偷看顶牌") == 2 ? 5 : 2;
                var cards = mgr.Deck.PeekTop(count);
                Debug.Log($"[偷看顶牌] 顶部{count}张:");
                foreach (var c in cards) Debug.Log($"  {c}");
            }),

            Create("偷看中间", "查看牌堆中间5张", 8, 2, 7, mgr =>
            {
                int count = mgr.GetCardLevel("偷看中间") == 2 ? 10 : 5;
                var cards = mgr.Deck.PeekMiddle(count);
                Debug.Log($"[偷看中间] 中间{count}张:");
                foreach (var c in cards) Debug.Log($"  {c}");
            }),

            Create("偷看底牌", "查看牌堆底部8张", 10, 2, 8, mgr =>
            {
                int count = mgr.GetCardLevel("偷看底牌") == 2 ? 15 : 8;
                var cards = mgr.Deck.PeekBottom(count);
                Debug.Log($"[偷看底牌] 底部{count}张:");
                foreach (var c in cards) Debug.Log($"  {c}");
            }),

            Create("定点找牌", "查看2张指定点数牌的精确位置", 30, 2, 25, mgr =>
            {
                int targetCount = mgr.GetCardLevel("定点找牌") == 2 ? 3 : 2;
                Card.Rank[] targets = { Card.Rank.Ace, Card.Rank.King, Card.Rank.Queen };
                var remaining = mgr.Deck.GetRemainingDeck();
                Debug.Log($"[定点找牌] 查找前{targetCount}个目标点数的位置:");
                for (int i = 0; i < Mathf.Min(targetCount, targets.Length); i++)
                {
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        if (remaining[j].rank == targets[i])
                        {
                            Debug.Log($"  {targets[i]} 在第{j + 1}张（从顶开始）");
                            break;
                        }
                    }
                }
            }),

            Create("提前验货", "查看接下来5张能否凑齐目标", 16, 2, 12, mgr =>
            {
                int count = mgr.GetCardLevel("提前验货") == 2 ? 8 : 5;
                var topCards = mgr.Deck.PeekTop(count);
                var allCheckCards = new List<Card>(mgr.Deck.GetDrawnCards());
                allCheckCards.AddRange(topCards);
                var target = mgr.TargetHandManager.GetCurrentTarget();
                if (target != null)
                {
                    bool canAchieve = target.checkCondition(allCheckCards);
                    Debug.Log($"[提前验货] 接下来{count}张{(canAchieve ? "可以" : "无法")}凑齐目标 [{target.handName}]");
                }
            }),

            Create("删顶牌", "删除牌堆顶部5张", 35, 2, 20, mgr =>
            {
                int count = mgr.GetCardLevel("删顶牌") == 2 ? 10 : 5;
                mgr.Deck.RemoveTop(count);
            }),

            Create("底牌搬家", "从底部取5张插入顶部", 30, 2, 20, mgr =>
            {
                int count = mgr.GetCardLevel("底牌搬家") == 2 ? 8 : 5;
                mgr.Deck.MoveBottomToTop(count);
            }),

            Create("切牌", "对半分并交换位置", 25, 2, 15, mgr =>
            {
                mgr.Deck.SplitAndSwap();
                if (mgr.GetCardLevel("切牌") == 2)
                {
                    var top3 = mgr.Deck.PeekTop(3);
                    Debug.Log("[切牌] 交换后新顶部3张:");
                    foreach (var c in top3) Debug.Log($"  {c}");
                }
            }),

            Create("翻转发牌", "剩余牌堆顺序完全翻转", 25, 2, 20, mgr =>
            {
                mgr.Deck.ReverseDeck();
            }),

            Create("换花色", "指定两种花色全部替换", 35, 2, 25, mgr =>
            {
                Debug.Log("[换花色] 效果已触发");
            }),

            Create("重洗牌堆", "彻底洗牌剩余牌堆", 40, 2, 25, mgr =>
            {
                mgr.Deck.ReshuffleRemaining();
                if (mgr.GetCardLevel("重洗牌堆") == 2)
                {
                    var topCards = mgr.Deck.PeekTop(2);
                    Debug.Log("[重洗牌堆] 免费查看顶部2张:");
                    foreach (var c in topCards) Debug.Log($"  {c}");
                }
            }),

            Create("双重目标", "同时追踪2个随机目标，完成任一即可", 20, 1, mgr =>
            {
                var (t1, t2) = mgr.TargetHandManager.GetDoubleTargets();
                if (t1 != null && t2 != null)
                    mgr.TargetHandManager.AnnounceTarget(t2);
            }),

            Create("换目标", "刷新为同难度层另一个随机牌型", 10, 1, mgr =>
            {
                mgr.TargetHandManager.ChangeTarget();
            }),

            Create("半赔半赚", "结算倍率×0.5", 15, 1, mgr =>
            {
                mgr.SetSettlementMultiplier(0.5f);
            }),

            Create("双倍输赢", "结算倍率×2.0", 25, 1, mgr =>
            {
                mgr.SetSettlementMultiplier(2.0f);
            }),

            Create("容错两次", "±2误差不扣不加", 40, 1, mgr =>
            {
                mgr.SetErrorTolerance(2);
            }),

            Create("亏损封顶", "本局损失上限-40", 40, 1, mgr =>
            {
                mgr.SetLossCap(40);
            }),

            Create("保留猜测", "修正前猜测被保留，结算选误差更小的", 55, 1, mgr =>
            {
                mgr.SetKeepPreviousGuess(true);
            }),

            Create("二次修正", "本局修正次数变为2次", 65, 1, mgr =>
            {
                mgr.CorrectionManager.AddCorrectionChances(1);
            }),

            Create("打折修正", "修正消耗-10", 20, 2, 25, mgr =>
            {
                int newCost = mgr.GetCardLevel("打折修正") == 2 ? 0 : 10;
                mgr.CorrectionManager.SetCorrectionCost(newCost);
            }),

            Create("超时修正", "翻过N后仍可修正，消耗-40", 18, 2, 25, mgr =>
            {
                int cost = mgr.GetCardLevel("超时修正") == 2 ? 20 : 40;
                mgr.CorrectionManager.ExtendCorrectionWindow(true);
                mgr.CorrectionManager.SetExtendedCorrectionCost(cost);
            }),

            CreateOncePerGame("免死一次", "致命伤害时筹码强制保留1点", 100, mgr =>
            {
                mgr.SetDeathSave(true);
            }),

            CreateOncePerGame("全押", "修正前激活；扣则归零，赚则×3", 60, mgr =>
            {
                mgr.SetAllInMode(true);
            }),
        };
    }

    private static StrategyCard Create(string name, string desc, int price, int maxLevel,
        int upgradePrice, Action<StrategyCardManager> effect)
    {
        var card = new StrategyCard(name, desc, price, maxLevel, effect);
        card.upgradePrice = upgradePrice;
        return card;
    }

    private static StrategyCard Create(string name, string desc, int price, int maxLevel,
        Action<StrategyCardManager> effect)
    {
        return new StrategyCard(name, desc, price, maxLevel, effect);
    }

    private static StrategyCard CreateOncePerGame(string name, string desc, int price,
        Action<StrategyCardManager> effect)
    {
        var card = new StrategyCard(name, desc, price, 1, effect);
        card.isOncePerGame = true;
        return card;
    }
}