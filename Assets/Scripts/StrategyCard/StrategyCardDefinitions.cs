using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                        StringBuilder sb = new StringBuilder("牌堆顶部：");
                        for (int i = 0; i < cards.Count; i++)
                            sb.Append($"第{i + 1}张{CardToChinese(cards[i])} ");
                        mgr.OnEffectTextUpdate?.Invoke("探顶", sb.ToString().Trim());
                        if (mgr.GetCardLevel("探顶") == 2 && cards.Count > 0)
                        {
                            List<string> opts = new List<string>();
                            for (int i = 0; i < cards.Count; i++)
                                opts.Add($"第{i + 1}张 {CardToChinese(cards[i])}");
                            mgr.RequestSelection("选择一张沉底", opts, idx => {
                                mgr.Deck.SinkCard(idx);
                            });
                        }
                    }));
                    break;

                case "探底":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("探底") == 2 ? 5 : 3;
                        var cards = mgr.Deck.PeekBottom(count);
                        StringBuilder sb = new StringBuilder("牌堆底部：");
                        for (int i = 0; i < cards.Count; i++)
                            sb.Append($"倒数第{i + 1}张{CardToChinese(cards[i])} ");
                        mgr.OnEffectTextUpdate?.Invoke("探底", sb.ToString().Trim());

                        int toSelect = mgr.GetCardLevel("探底") == 2 ? 2 : 1;
                        List<string> opts = new List<string>();
                        for (int i = 0; i < cards.Count; i++)
                            opts.Add($"倒数第{i + 1}张 {CardToChinese(cards[i])}");

                        SelectMultiple(mgr, "选择要置顶的牌", opts, toSelect, selectedIndices => {
                            List<Card> toTop = new List<Card>();
                            for (int i = 0; i < selectedIndices.Count; i++)
                            {
                                toTop.Add(cards[selectedIndices[i]]);
                            }
                            for (int i = toTop.Count - 1; i >= 0; i--)
                            {
                                int idx = mgr.Deck.FindCardIndex(toTop[i]);
                                if (idx >= 0) mgr.Deck.TopCard(idx);
                            }
                        });
                    }));
                    break;

                case "探牌":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("探牌") == 2 ? 8 : 5;
                        var remaining = mgr.Deck.GetRemainingDeck();
                        var rng = new System.Random();
                        var indices = new HashSet<int>();
                        StringBuilder sb = new StringBuilder("未翻牌中：");
                        int found = 0;
                        while (found < count && indices.Count < remaining.Count)
                        {
                            int idx = rng.Next(remaining.Count);
                            if (indices.Add(idx))
                            {
                                sb.Append($"第{idx + 1}张{CardToChinese(remaining[idx])} ");
                                found++;
                            }
                        }
                        mgr.OnEffectTextUpdate?.Invoke("探牌", sb.ToString().Trim());
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
                        int bonus = mgr.GetCardLevel("早鸟优惠") == 2 ? 20 : 12;
                        mgr.SetEarlyBird(10, bonus);
                    }, mgr => true));
                    break;

                case "近误差红利":
                    list.Add(new StrategyCard(data, mgr => {
                        int bonus = mgr.GetCardLevel("近误差红利") == 2 ? 15 : 8;
                        mgr.SetNearErrorBonus(bonus);
                    }, mgr => true));
                    break;

                case "偏差大师":
                    list.Add(new StrategyCard(data, mgr => { }, mgr => true));
                    break;

                // ===== 第二层 =====
                case "点数搜索":
                    list.Add(new StrategyCard(data, mgr => {
                        bool showAll = mgr.GetCardLevel("点数搜索") == 2;
                        List<string> ranks = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
                        mgr.RequestSelection("选择要搜索的点数", ranks, idx => {
                            Card.Rank targetRank = (Card.Rank)(idx + 1);
                            var remaining = mgr.Deck.GetRemainingDeck();
                            StringBuilder sb = new StringBuilder($"点数{ranks[idx]}：");
                            int found = 0;
                            for (int i = 0; i < remaining.Count; i++)
                            {
                                if (remaining[i].rank == targetRank)
                                {
                                    sb.Append($"第{i + 1}张 ");
                                    found++;
                                    if (!showAll && found >= 2) break;
                                }
                            }
                            if (found == 0) sb.Append("未找到");
                            mgr.OnEffectTextUpdate?.Invoke("点数搜索", sb.ToString().Trim());
                        });
                    }));
                    break;

                case "花色搜索":
                    list.Add(new StrategyCard(data, mgr => {
                        bool showAll = mgr.GetCardLevel("花色搜索") == 2;
                        List<string> suits = new List<string> { "黑桃", "红心", "梅花", "方块" };
                        mgr.RequestSelection("选择要搜索的花色", suits, idx => {
                            Card.Suit targetSuit = (Card.Suit)idx;
                            var remaining = mgr.Deck.GetRemainingDeck();
                            StringBuilder sb = new StringBuilder($"{suits[idx]}：");
                            int found = 0;
                            for (int i = 0; i < remaining.Count; i++)
                            {
                                if (remaining[i].suit == targetSuit)
                                {
                                    sb.Append($"第{i + 1}张{CardToChinese(remaining[i])} ");
                                    found++;
                                    if (!showAll && found >= 2) break;
                                }
                            }
                            if (found == 0) sb.Append("未找到");
                            mgr.OnEffectTextUpdate?.Invoke("花色搜索", sb.ToString().Trim());
                        });
                    }));
                    break;

                case "沉底":
                    list.Add(new StrategyCard(data, mgr => {
                        if (mgr.GetCardLevel("沉底") == 2)
                        {
                            var top3 = mgr.Deck.PeekTop(3);
                            List<string> opts = new List<string>();
                            for (int i = 0; i < top3.Count; i++)
                                opts.Add($"第{i + 1}张 {CardToChinese(top3[i])}");
                            mgr.RequestSelection("选择要沉底的牌", opts, idx => {
                                mgr.Deck.SinkCard(idx);
                            });
                        }
                        else
                        {
                            mgr.Deck.MoveTopToBottom(2);
                        }
                    }));
                    break;

                case "交换":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetCardLevel("交换") == 2 ? 3 : 2;
                        mgr.Deck.SwapTopAndBottom(count);
                    }));
                    break;

                case "洗牌":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.Deck.ReshuffleRemaining();
                        if (mgr.GetCardLevel("洗牌") == 2)
                        {
                            var top = mgr.Deck.PeekTop(2);
                            StringBuilder sb = new StringBuilder("洗牌后顶部：");
                            for (int i = 0; i < top.Count; i++)
                                sb.Append($"第{i + 1}张{CardToChinese(top[i])} ");
                            mgr.OnEffectTextUpdate?.Invoke("洗牌", sb.ToString().Trim());
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
                        List<string> opts = new List<string>();
                        for (int i = 0; i < cards.Count; i++)
                            opts.Add($"第{i + 1}张 {CardToChinese(cards[i])}");
                        mgr.RequestSelection("选择要保留的牌（其余沉底）", opts, idx => {
                            mgr.Deck.FastForward(count, idx);
                        });
                    }));
                    break;

                case "删除":
                    list.Add(new StrategyCard(data, mgr => {
                        var drawn = mgr.Deck.GetDrawnCards();
                        if (drawn.Count == 0) return;
                        int max = mgr.GetCardLevel("删除") == 2 ? 2 : 1;
                        List<string> opts = new List<string>();
                        for (int i = 0; i < drawn.Count; i++)
                            opts.Add($"第{i + 1}张 {CardToChinese(drawn[i])}");
                        SelectMultiple(mgr, "选择要删除的已翻牌", opts, max, selectedIndices => {
                            if (selectedIndices.Count > 0)
                                mgr.Deck.RemoveFromDrawn(selectedIndices.Count);
                        });
                    }));
                    break;

                case "复制":
                    list.Add(new StrategyCard(data, mgr => {
                        var drawn = mgr.Deck.GetDrawnCards();
                        if (drawn.Count == 0) return;
                        bool toTop = mgr.GetCardLevel("复制") == 2;
                        List<string> opts = new List<string>();
                        for (int i = 0; i < drawn.Count; i++)
                            opts.Add($"第{i + 1}张 {CardToChinese(drawn[i])}");
                        mgr.RequestSelection("选择要复制的已翻牌", opts, idx => {
                            mgr.Deck.CopyDrawnCard(idx, toTop);
                        });
                    }));
                    break;

                case "换目标":
                    list.Add(new StrategyCard(data, mgr => {
                        var alternatives = mgr.TargetHandManager.GetAlternativeTargets(mgr.GetCardLevel("换目标") == 2 ? 3 : 2);
                        if (alternatives.Count == 0) return;
                        List<string> opts = new List<string>();
                        foreach (var t in alternatives)
                            opts.Add($"{t.handName}：{t.description}");
                        mgr.RequestSelection("选择新目标", opts, idx => {
                            mgr.TargetHandManager.SetCurrentTarget(alternatives[idx]);
                            mgr.OnEffectTextUpdate?.Invoke("换目标", $"已更换目标为：{alternatives[idx].handName}");
                        });
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
                    list.Add(new StrategyCard(data, mgr => { }, mgr => true));
                    break;

                case "修正艺术家":
                    list.Add(new StrategyCard(data, mgr => { }, mgr => true));
                    break;

                case "速攻":
                    list.Add(new StrategyCard(data, mgr => { }, mgr => true));
                    break;

                // ===== 第三层 =====
                case "回收":
                    list.Add(new StrategyCard(data, mgr => {
                        var drawn = mgr.Deck.GetDrawnCards();
                        if (drawn.Count == 0) return;
                        int max = mgr.GetCardLevel("回收") == 2 ? 2 : 1;
                        List<string> opts = new List<string>();
                        for (int i = 0; i < drawn.Count; i++)
                            opts.Add($"第{i + 1}张 {CardToChinese(drawn[i])}");
                        SelectMultiple(mgr, "选择要洗回的已翻牌", opts, max, selectedIndices => {
                            if (selectedIndices.Count > 0)
                                mgr.Deck.ReturnDrawnToDeck(selectedIndices.Count);
                        });
                    }));
                    break;

                case "修正促销":
                    list.Add(new StrategyCard(data, mgr => {
                        mgr.CorrectionManager.SetCorrectionCost(0);
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
                    list.Add(new StrategyCard(data, mgr => { }, mgr => true));
                    break;

                case "天启":
                    list.Add(new StrategyCard(data, mgr => {
                        int count = mgr.GetApocalypsePreviewCount();
                        var cards = mgr.Deck.PeekTop(count);
                        StringBuilder sb = new StringBuilder($"顶部{count}张：");
                        int show = Mathf.Min(cards.Count, 10);
                        for (int i = 0; i < show; i++)
                            sb.Append($"第{i + 1}张{CardToChinese(cards[i])} ");
                        if (cards.Count > 10) sb.Append($"...共{cards.Count}张");
                        mgr.OnEffectTextUpdate?.Invoke("天启", sb.ToString().Trim());
                    }));
                    break;

                default:
                    break;
            }
        }
        return list;
    }

    private static void SelectMultiple(StrategyCardManager mgr, string prompt, List<string> options, int maxCount, System.Action<List<int>> callback)
    {
        List<int> selected = new List<int>();
        List<string> remainingOptions = new List<string>(options);
        List<int> remainingIndices = new List<int>();
        for (int i = 0; i < options.Count; i++) remainingIndices.Add(i);

        ShowOptions();

        void ShowOptions()
        {
            if (selected.Count >= maxCount || remainingOptions.Count == 0)
            {
                callback(selected);
                return;
            }
            mgr.RequestSelection($"{prompt}（已选{selected.Count}/{maxCount}，取消完成）", remainingOptions, choiceIndex => {
                int actualIndex = remainingIndices[choiceIndex];
                selected.Add(actualIndex);
                remainingOptions.RemoveAt(choiceIndex);
                remainingIndices.RemoveAt(choiceIndex);
                ShowOptions();
            }, () => {
                callback(selected);
            });
        }
    }

    private static string CardToChinese(Card card)
    {
        string suit = card.suit switch
        {
            Card.Suit.Spades => "黑桃",
            Card.Suit.Hearts => "红心",
            Card.Suit.Clubs => "梅花",
            Card.Suit.Diamonds => "方块",
            _ => ""
        };
        string rank = card.rank switch
        {
            Card.Rank.Ace => "A",
            Card.Rank.Jack => "J",
            Card.Rank.Queen => "Q",
            Card.Rank.King => "K",
            _ => ((int)card.rank).ToString()
        };
        return suit + rank;
    }
}