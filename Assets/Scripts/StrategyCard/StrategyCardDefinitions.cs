using System.Collections.Generic;
using System.Linq;
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
                // ===== 第一层 =====
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
                        if (mgr.GetCardLevel("探底") == 2 && cards.Count > 1)
                            mgr.RequestTopOneFromPeek(cards);
                        else if (mgr.GetCardLevel("探底") == 1 && cards.Count > 0)
                            mgr.RequestTopOneFromPeek(new List<Card> { cards[0] });
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

                case "宽限":
                    list.Add(new StrategyCard(data, mgr => {
                        int extend = mgr.GetCardLevel("宽限") == 2 ? 4 : 2;
                        mgr.CorrectionManager.ExtendCorrectionWindowBy(extend);
                    }, mgr => true));
                    break;

                case "再修一次":
                    list.Add(new StrategyCard(data, mgr => {
                        int extra = mgr.GetCardLevel("再修一次") == 2 ? 2 : 1;
                        mgr.CorrectionManager.AddCorrectionChances(extra);
                    }, mgr => true));
                    break;

                case "早鸟优惠":
                    list.Add(new StrategyCard(data, mgr => {
                        int threshold = mgr.GetCardLevel("早鸟优惠") == 2 ? 10 : 10;
                        int bonus = mgr.GetCardLevel("早鸟优惠") == 2 ? 20 : 12;
                        mgr.SetEarlyBird(threshold, bonus);
                    }, mgr => true));
                    break;

                case "近误差红利":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("近误差红利") == 2 ? 15 : 8;
                        mgr.SetNearErrorBonus(bonus);
                    }, mgr => true));
                    break;

                case "偏差大师":
                    list.Add(new StrategyCard(data, mgr => {
                        // 被动牌，效果在结算时检查
                    }, mgr => true));
                    break;

                // ===== 第二层 =====
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

                case "沉底":
                    list.Add(new StrategyCard(data, mgr => {
                        if (mgr.GetCardLevel("沉底") == 2)
                            mgr.RequestSinkChoice(3);
                        else
                            mgr.Deck.MoveTopToBottom(2);
                    }));
                    break;

                case "交换":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("交换") == 2 ? 3 : 2;
                        var top = mgr.Deck.PeekTop(count);
                        var bottom = mgr.Deck.PeekBottom(count);
                        mgr.Deck.RemoveTop(count);
                        for (int i = bottom.Count - 1; i >= 0; i--)
                            mgr.Deck.GetRemainingDeck().Insert(0, bottom[i]);
                        for (int i = 0; i < top.Count; i++)
                            mgr.Deck.GetRemainingDeck().Add(top[i]);
                        Debug.Log($"[交换] 顶部{count}张和底部{count}张互换");
                    }));
                    break;

                case "洗牌":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.Deck.ReshuffleRemaining();
                        if (mgr.GetCardLevel("洗牌") == 2)
                        {
                            var top = mgr.Deck.PeekTop(2);
                            Debug.Log("[洗牌] 免费查看顶部2张:");
                            foreach (var c in top) Debug.Log($"  {c}");
                        }
                    }));
                    break;

                case "预览":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("预览") == 2 ? 5 : 3;
                        mgr.SetPreview(count);
                    }, mgr => true));
                    break;

                case "快进":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("快进") == 2 ? 5 : 3;
                        var cards = mgr.Deck.PeekTop(count);
                        Debug.Log($"[快进] 连续翻{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                        mgr.OnRequestFastForwardSelect?.Invoke(count, selectedIdx => {
                            var remaining = mgr.Deck.GetRemainingDeck();
                            var selected = remaining[selectedIdx];
                            remaining.RemoveAt(selectedIdx);
                            remaining.Insert(0, selected);
                            for (int i = 1; i < count; i++)
                                mgr.Deck.MoveTopToBottom(1);
                            Debug.Log($"[快进] 保留{selected}，其余沉底");
                        });
                    }));
                    break;

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

                case "换目标":
                    list.Add(new StrategyCard(data, mgr => {
                        int options = mgr.GetCardLevel("换目标") == 2 ? 3 : 2;
                        var alternatives = mgr.TargetHandManager.GetAlternativeTargets(options);
                        mgr.RequestTargetChoice(alternatives);
                    }));
                    break;

                case "零误差红利":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("零误差红利") == 2 ? 35 : 20;
                        mgr.SetZeroErrorBonus(bonus);
                    }, mgr => true));
                    break;

                case "宽容":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("宽容") == 2 ? 2 : 1;
                        mgr.AddErrorToleranceBonus(bonus);
                    }, mgr => true));
                    break;

                case "稳扎稳打":
                    list.Add(new StrategyCard(data, mgr => {
                        // 被动牌，效果在结算时检查
                    }, mgr => true));
                    break;

                case "修正艺术家":
                    list.Add(new StrategyCard(data, mgr => {
                        // 被动牌，效果在结算时检查
                    }, mgr => true));
                    break;

                case "速攻":
                    list.Add(new StrategyCard(data, mgr => {
                        // 被动牌，效果在结算时检查
                    }, mgr => true));
                    break;

                // ===== 第三层 =====
                case "回收":
                    list.Add(new StrategyCard(data, mgr => {
                        int max = mgr.GetCardLevel("回收") == 2 ? 2 : 1;
                        mgr.RequestDeleteDrawnCards(max);
                        Debug.Log($"[回收] 选择最多{max}张已翻牌洗回（翻牌数-{max}）");
                    }));
                    break;

                case "修正促销":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.CorrectionManager.SetCorrectionCost(0);
                        if (mgr.GetCardLevel("修正促销") == 2)
                        {
                            Debug.Log("[修正促销+] 修正免费且使用后额外+3");
                        }
                    }, mgr => true));
                    break;

                case "消除特殊":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.RuleManager.DisableSpecialRule();
                    }));
                    break;

                case "孤注一掷":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.SetAllInMode(true);
                        mgr.AddErrorToleranceBonus(-mgr.GetErrorToleranceBonus());
                        Debug.Log("[孤注一掷] 容忍度=0，误差=0时收入x5");
                    }));
                    break;

                case "完美风暴":
                    list.Add(new StrategyCard(data, mgr => {
                        float mult = mgr.GetCardLevel("完美风暴") == 2 ? 2f : 1.5f;
                        mgr.SetPerfectMultiplier(mult);
                    }, mgr => true));
                    break;

                // ===== 第四层 =====
                case "不死鸟":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.SetDeathDefy(true);
                    }));
                    break;

                case "十次修正":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.CorrectionManager.SetCorrectionCost(0);
                        mgr.CorrectionManager.AddCorrectionChances(10);
                        mgr.CorrectionManager.ExtendCorrectionWindow(true);
                    }, mgr => true));
                    break;

                case "命运之轮":
                    list.Add(new StrategyCard(data, mgr => {
                        // 被动牌，+15容忍度
                    }, mgr => true));
                    break;

                case "天启":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetApocalypsePreviewCount();
                        var cards = mgr.Deck.PeekTop(count);
                        Debug.Log($"[天启] 查看顶部{count}张:");
                        foreach (var c in cards) Debug.Log($"  {c}");
                    }));
                    break;

                default:
                    Debug.LogWarning($"未实现效果的策略牌: {data.cardName}");
                    break;
            }
        }
        return list;
    }
}