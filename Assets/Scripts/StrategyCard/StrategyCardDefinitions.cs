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
                // ===== 信息-看牌 =====
                case "探顶":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("探顶") == 2 ? 5 : 3;
                        var cards = mgr.Deck.PeekTop(count);
                        Debug.Log($"[探顶] 顶部{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                        if (mgr.GetCardLevel("探顶") == 2 && cards.Count > 0)
                            mgr.RequestSinkOneFromPeek(cards);
                    }));
                    break;

                case "探底":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("探底") == 2 ? 5 : 3;
                        var cards = mgr.Deck.PeekBottom(count);
                        Debug.Log($"[探底] 底部{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                        if (mgr.GetCardLevel("探底") == 2 && cards.Count > 0)
                            mgr.RequestTopOneFromPeek(cards);
                    }));
                    break;

                case "探牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("探牌") == 2 ? 5 : 3;
                        var cards = mgr.Deck.PeekRandomUnrevealed(count);
                        Debug.Log($"[探牌] 随机{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                    }));
                    break;

                case "点数搜索":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.RequestRankSearch(mgr.GetCardLevel("点数搜索") == 2);
                    }));
                    break;

                case "花色搜索":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.RequestSuitSearch(mgr.GetCardLevel("花色搜索") == 2);
                    }));
                    break;

                // ===== 信息-目标检测 =====
                case "先知":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("先知") == 2 ? 8 : 5;
                        var top = mgr.Deck.PeekTop(count);
                        var all = new List<Card>(mgr.Deck.GetDrawnCards());
                        all.AddRange(top);
                        var target = mgr.TargetHandManager.GetCurrentTarget();
                        if (target != null)
                        {
                            bool canAchieve = target.checkCondition(all);
                            if (mgr.GetCardLevel("先知") == 2 && canAchieve)
                            {
                                int pos = FindFirstAchievePosition(mgr.Deck.GetDrawnCards(), top, target);
                                Debug.Log($"[先知+] 接下来{count}张可达成，第{pos}张首次达成");
                            }
                            else
                            {
                                Debug.Log($"[先知] 接下来{count}张{(canAchieve ? "可以" : "无法")}达成");
                            }
                        }
                    }));
                    break;

                // ===== 改牌-移动 =====
                case "沉底":
                    list.Add(new StrategyCard(data, mgr => {
                        if (mgr.GetCardLevel("沉底") == 2)
                            mgr.RequestSinkChoice(3);
                        else
                            mgr.Deck.MoveTopToBottom(2);
                    }));
                    break;

                case "置顶":
                    list.Add(new StrategyCard(data, mgr => {
                        if (mgr.GetCardLevel("置顶") == 2)
                            mgr.RequestTopChoice(3);
                        else
                            mgr.Deck.MoveBottomToTop(2);
                    }));
                    break;

                // ===== 改牌-删复 =====
                case "删除":
                    list.Add(new StrategyCard(data, mgr => {
                        int max = mgr.GetCardLevel("删除") == 2 ? 2 : 1;
                        mgr.RequestDeleteDrawnCards(max);
                    }));
                    break;

                case "复制":
                    list.Add(new StrategyCard(data, mgr => {
                        bool toTop = mgr.GetCardLevel("复制") == 2;
                        mgr.RequestCopyDrawnCard(toTop);
                    }));
                    break;

                // ===== 改修正 =====
                case "宽限":
                    list.Add(new StrategyCard(data, mgr => {
                        int extend = mgr.GetCardLevel("宽限") == 2 ? 4 : 2;
                        mgr.CorrectionManager.ExtendCorrectionWindowBy(extend);
                    }));
                    break;

                case "再修一次":
                    list.Add(new StrategyCard(data, mgr => {
                        int extra = mgr.GetCardLevel("再修一次") == 2 ? 2 : 1;
                        mgr.CorrectionManager.AddCorrectionChances(extra);
                    }));
                    break;

                case "修正促销":
                    list.Add(new StrategyCard(data, mgr => {
                        int cost = mgr.GetCardLevel("修正促销") == 2 ? 0 : 5;
                        mgr.CorrectionManager.SetCorrectionCost(cost);
                    }));
                    break;

                // ===== 改规则 =====
                case "近误差红利":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("近误差红利") == 2 ? 25 : 15;
                        mgr.SetNearErrorBonus(bonus);
                    }));
                    break;

                case "早鸟优惠":
                    list.Add(new StrategyCard(data, mgr => {
                        int threshold = mgr.GetCardLevel("早鸟优惠") == 2 ? 14 : 10;
                        int bonus = mgr.GetCardLevel("早鸟优惠") == 2 ? 24 : 18;
                        mgr.SetEarlyBird(threshold, bonus);
                    }));
                    break;

                case "换目标":
                    list.Add(new StrategyCard(data, mgr => {
                        int options = mgr.GetCardLevel("换目标") == 2 ? 3 : 2;
                        var alternatives = mgr.TargetHandManager.GetAlternativeTargets(options);
                        mgr.RequestTargetChoice(alternatives);
                    }));
                    break;

                case "零误差红利":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("零误差红利") == 2 ? 50 : 30;
                        mgr.SetZeroErrorBonus(bonus);
                    }));
                    break;

                case "绝处逢生":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.SetDeathDefy(true);
                    }));
                    break;

                case "消除特殊":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.RuleManager.DisableSpecialRule();
                    }));
                    break;

                case "宽容+":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("宽容+") == 2 ? 2 : 1;
                        mgr.AddErrorToleranceBonus(bonus);
                    }));
                    break;

                default:
                    Debug.LogWarning($"未实现效果的策略牌: {data.cardName}");
                    break;
            }
        }
        return list;
    }

    private static int FindFirstAchievePosition(List<Card> drawn, List<Card> upcoming, TargetHand target)
    {
        var sim = new List<Card>(drawn);
        for (int i = 0; i < upcoming.Count; i++)
        {
            sim.Add(upcoming[i]);
            if (target.checkCondition(sim))
                return drawn.Count + i + 1;
        }
        return -1;
    }
}