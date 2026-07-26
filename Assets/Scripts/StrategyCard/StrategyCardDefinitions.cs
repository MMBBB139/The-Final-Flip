using System.Collections.Generic;
using UnityEngine;

public static class StrategyCardDefinitions
{
    public static List<StrategyCard> CreateAll(StrategyCardData[] dataArray)
    {
        var list = new List<StrategyCard>();
        foreach (var data in dataArray)
        {
            switch (data.cardName)
            {
                case "偷看顶牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("偷看顶牌") == 2 ? 5 : 2;
                        var cards = mgr.Deck.PeekTop(count);
                        Debug.Log($"[偷看顶牌] 顶部{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                    }));
                    break;
                case "偷看中间":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("偷看中间") == 2 ? 10 : 5;
                        var cards = mgr.Deck.PeekMiddle(count);
                        Debug.Log($"[偷看中间] 中间{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                    }));
                    break;
                case "偷看底牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("偷看底牌") == 2 ? 15 : 8;
                        var cards = mgr.Deck.PeekBottom(count);
                        Debug.Log($"[偷看底牌] 底部{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                    }));
                    break;
                case "定点找牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int targetCount = mgr.GetCardLevel("定点找牌") == 2 ? 3 : 2;
                        Card.Rank[] targets = { Card.Rank.Ace, Card.Rank.King, Card.Rank.Queen };
                        var remaining = mgr.Deck.GetRemainingDeck();
                        Debug.Log($"[定点找牌] 查找前{targetCount}个目标:");
                        for (int i = 0; i < Mathf.Min(targetCount, targets.Length); i++)
                        {
                            for (int j = 0; j < remaining.Count; j++)
                                if (remaining[j].rank == targets[i])
                                {
                                    Debug.Log($"  {targets[i]} 在第{j + 1}张");
                                    break;
                                }
                        }
                    }));
                    break;
                case "提前验货":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("提前验货") == 2 ? 8 : 5;
                        var top = mgr.Deck.PeekTop(count);
                        var all = new List<Card>(mgr.Deck.GetDrawnCards());
                        all.AddRange(top);
                        var target = mgr.TargetHandManager.GetCurrentTarget();
                        if (target != null)
                            Debug.Log($"[提前验货] 接下来{count}张{(target.checkCondition(all) ? "可以" : "无法")}达成[{target.handName}]");
                    }));
                    break;
                case "删顶牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("删顶牌") == 2 ? 10 : 5;
                        mgr.Deck.RemoveTop(count);
                    }));
                    break;
                case "底牌搬家":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("底牌搬家") == 2 ? 8 : 5;
                        mgr.Deck.MoveBottomToTop(count);
                    }));
                    break;
                case "切牌":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.Deck.SplitAndSwap();
                        if (mgr.GetCardLevel("切牌") == 2)
                        {
                            var top3 = mgr.Deck.PeekTop(3);
                            Debug.Log("[切牌] 新顶部3张:");
                            foreach (var c in top3) Debug.Log($"  {c}");
                        }
                    }));
                    break;
                case "翻转发牌":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.Deck.ReverseDeck();
                    }));
                    break;
                case "换花色":
                    list.Add(new StrategyCard(data, mgr => {
                        Debug.Log("[换花色] 效果已触发（需扩展花色替换逻辑）");
                    }));
                    break;
                case "重洗牌堆":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.Deck.ReshuffleRemaining();
                        if (mgr.GetCardLevel("重洗牌堆") == 2)
                        {
                            var top = mgr.Deck.PeekTop(2);
                            Debug.Log("[重洗] 免费查看顶部2张:");
                            foreach (var c in top) Debug.Log($"  {c}");
                        }
                    }));
                    break;
                case "双重目标":
                    list.Add(new StrategyCard(data, mgr => {
                        var (t1, t2) = mgr.TargetHandManager.GetDoubleTargets();
                        if (t1 != null && t2 != null) mgr.TargetHandManager.AnnounceTarget(t2);
                    }));
                    break;
                case "换目标":
                    list.Add(new StrategyCard(data, mgr => mgr.TargetHandManager.ChangeTarget()));
                    break;
                case "半赔半赚":
                    list.Add(new StrategyCard(data, mgr => mgr.SetSettlementMultiplier(0.5f)));
                    break;
                case "双倍输赢":
                    list.Add(new StrategyCard(data, mgr => mgr.SetSettlementMultiplier(2.0f)));
                    break;
                case "容错两次":
                    list.Add(new StrategyCard(data, mgr => mgr.SetErrorTolerance(2)));
                    break;
                case "亏损封顶":
                    list.Add(new StrategyCard(data, mgr => mgr.SetLossCap(40)));
                    break;
                case "保留猜测":
                    list.Add(new StrategyCard(data, mgr => mgr.SetKeepPreviousGuess(true)));
                    break;
                case "二次修正":
                    list.Add(new StrategyCard(data, mgr => mgr.CorrectionManager.AddCorrectionChances(1)));
                    break;
                case "打折修正":
                    list.Add(new StrategyCard(data, mgr => {
                        int newCost = mgr.GetCardLevel("打折修正") == 2 ? 0 : 10;
                        mgr.CorrectionManager.SetCorrectionCost(newCost);
                    }));
                    break;
                case "超时修正":
                    list.Add(new StrategyCard(data, mgr => {
                        int cost = mgr.GetCardLevel("超时修正") == 2 ? 20 : 40;
                        mgr.CorrectionManager.ExtendCorrectionWindow(true);
                        mgr.CorrectionManager.SetExtendedCorrectionCost(cost);
                    }));
                    break;
                case "免死一次":
                    list.Add(new StrategyCard(data, mgr => mgr.SetDeathSave(true)));
                    break;
                case "全押":
                    list.Add(new StrategyCard(data, mgr => mgr.SetAllInMode(true)));
                    break;
                default:
                    Debug.LogWarning($"未实现效果的策略牌: {data.cardName}");
                    break;
            }
        }
        return list;
    }
}