using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ==================== 结算结果 ====================
public class GoalResult
{
    public bool isAchieved;
    public int flipCount;
    public List<Card> winningCards;

    public static GoalResult Fail => new GoalResult { isAchieved = false, flipCount = -1, winningCards = new List<Card>() };
}

// ==================== 基类 ====================
public abstract class GoalChecker
{
    public abstract string goalName { get; }
    public abstract int stage { get; }
    public abstract GoalResult Check(List<Card> drawnCards);

    protected static bool HasSameSuitCount(List<Card> cards, int needCount)
    {
        Dictionary<Card.Suit, int> suitCount = new Dictionary<Card.Suit, int>();
        foreach (var card in cards)
        {
            if (!suitCount.ContainsKey(card.suit)) suitCount[card.suit] = 0;
            suitCount[card.suit]++;
            if (suitCount[card.suit] >= needCount) return true;
        }
        return false;
    }

    protected static bool HasConsecutiveRanks(List<Card> cards, int needCount)
    {
        HashSet<int> ranks = new HashSet<int>();
        foreach (var card in cards)
            ranks.Add((int)card.rank);

        List<int> sorted = new List<int>(ranks);
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 1)
                    consecutive++;
                else
                    break;
            }
            if (consecutive >= needCount) return true;
        }
        return false;
    }

    protected static bool HasSameRankCount(List<Card> cards, int needCount)
    {
        Dictionary<int, int> rankCount = new Dictionary<int, int>();
        foreach (var card in cards)
        {
            int r = (int)card.rank;
            if (!rankCount.ContainsKey(r)) rankCount[r] = 0;
            rankCount[r]++;
            if (rankCount[r] >= needCount) return true;
        }
        return false;
    }

    protected static int CountCards(List<Card> cards, System.Predicate<Card> predicate)
    {
        int count = 0;
        foreach (var card in cards)
        {
            if (predicate(card)) count++;
        }
        return count;
    }

    protected static int FindNthIndex(List<Card> cards, System.Predicate<Card> predicate, int needCount)
    {
        int found = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (predicate(cards[i]))
            {
                found++;
                if (found == needCount) return i + 1;
            }
        }
        return -1;
    }

    protected static List<Card> CollectMatchingCards(List<Card> cards, System.Predicate<Card> predicate)
    {
        List<Card> result = new List<Card>();
        foreach (var card in cards)
        {
            if (predicate(card)) result.Add(card);
        }
        return result;
    }
}

// ==================== 第1层 ====================

public class Goal_ThreeOdds : GoalChecker
{
    public override string goalName => "三奇数";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        System.Predicate<Card> pred = c => ((int)c.rank % 2 == 1);
        int count = CountCards(drawnCards, pred);
        if (count < 3) return GoalResult.Fail;

        return new GoalResult
        {
            isAchieved = true,
            flipCount = FindNthIndex(drawnCards, pred, 3),
            winningCards = CollectMatchingCards(drawnCards, pred)
        };
    }
}

public class Goal_SmallThreeConsecutive : GoalChecker
{
    public override string goalName => "小三张";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        System.Predicate<Card> pred = c => ((int)c.rank <= 4);
        int count = CountCards(drawnCards, pred);
        if (count < 3) return GoalResult.Fail;

        return new GoalResult
        {
            isAchieved = true,
            flipCount = FindNthIndex(drawnCards, pred, 3),
            winningCards = CollectMatchingCards(drawnCards, pred)
        };
    }
}

public class Goal_ThreeSameSuit : GoalChecker
{
    public override string goalName => "三同花";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, int> suitCount = new Dictionary<Card.Suit, int>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();
        List<Card>[] suitCards = new List<Card>[4];
        for (int i = 0; i < 4; i++) suitCards[i] = new List<Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            int s = (int)card.suit;
            suitCards[s].Add(card);

            if (!suitCount.ContainsKey(card.suit))
            {
                suitCount[card.suit] = 0;
                suitLastIndex[card.suit] = 0;
            }
            suitCount[card.suit]++;
            suitLastIndex[card.suit] = i + 1;

            if (suitCount[card.suit] >= 3)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = suitLastIndex[card.suit],
                    winningCards = new List<Card>(suitCards[s])
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_RedBlack33 : GoalChecker
{
    public override string goalName => "红黑各三张";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        int redCount = 0, blackCount = 0;
        int redLast = -1, blackLast = -1;
        List<Card> redCards = new List<Card>(), blackCards = new List<Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            bool isRed = card.suit == Card.Suit.Hearts || card.suit == Card.Suit.Diamonds;
            if (isRed)
            {
                redCount++;
                redLast = i + 1;
                redCards.Add(card);
            }
            else
            {
                blackCount++;
                blackLast = i + 1;
                blackCards.Add(card);
            }

            if (redCount >= 3 && blackCount >= 3)
            {
                var winning = new List<Card>();
                winning.AddRange(redCards);
                winning.AddRange(blackCards);
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = System.Math.Max(redLast, blackLast),
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_AllFourSuits : GoalChecker
{
    public override string goalName => "四花";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        HashSet<Card.Suit> suits = new HashSet<Card.Suit>();
        List<Card> firstOfEachSuit = new List<Card>();
        int lastIndex = -1;

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            if (!suits.Contains(card.suit))
            {
                suits.Add(card.suit);
                firstOfEachSuit.Add(card);
            }
            lastIndex = i + 1;

            if (suits.Count >= 4)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastIndex,
                    winningCards = new List<Card>(firstOfEachSuit)
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_ThreeConsecutive : GoalChecker
{
    public override string goalName => "三连顺";
    public override int stage => 1;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            rankLastIndex[r] = i + 1;
            if (!rankSample.ContainsKey(r)) rankSample[r] = drawnCards[i];
        }

        var sorted = rankLastIndex.Keys.ToList();
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            int maxJ = i;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 1)
                {
                    consecutive++;
                    maxJ = j;
                }
                else break;
            }

            if (consecutive >= 3)
            {
                int lastFlip = 0;
                List<Card> winning = new List<Card>();
                for (int k = i; k <= maxJ; k++)
                {
                    int rank = sorted[k];
                    winning.Add(rankSample[rank]);
                    if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                }
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastFlip,
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

// ==================== 第2层 ====================

public class Goal_FourOdds : GoalChecker
{
    public override string goalName => "四奇数";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        System.Predicate<Card> pred = c => ((int)c.rank % 2 == 1);
        int count = CountCards(drawnCards, pred);
        if (count < 4) return GoalResult.Fail;

        return new GoalResult
        {
            isAchieved = true,
            flipCount = FindNthIndex(drawnCards, pred, 4),
            winningCards = CollectMatchingCards(drawnCards, pred)
        };
    }
}

public class Goal_SmallFourConsecutive : GoalChecker
{
    public override string goalName => "小四张";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        System.Predicate<Card> pred = c => ((int)c.rank <= 6);
        int count = CountCards(drawnCards, pred);
        if (count < 4) return GoalResult.Fail;

        return new GoalResult
        {
            isAchieved = true,
            flipCount = FindNthIndex(drawnCards, pred, 4),
            winningCards = CollectMatchingCards(drawnCards, pred)
        };
    }
}

public class Goal_ThreeFaceCards : GoalChecker
{
    public override string goalName => "三人头";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        System.Predicate<Card> pred = c => ((int)c.rank >= 11 && (int)c.rank <= 13);
        int count = CountCards(drawnCards, pred);
        if (count < 3) return GoalResult.Fail;

        return new GoalResult
        {
            isAchieved = true,
            flipCount = FindNthIndex(drawnCards, pred, 3),
            winningCards = CollectMatchingCards(drawnCards, pred)
        };
    }
}

public class Goal_TwoPair : GoalChecker
{
    public override string goalName => "两对";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, List<Card>> rankCards = new Dictionary<int, List<Card>>();
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (!rankCards.ContainsKey(r))
            {
                rankCards[r] = new List<Card>();
                rankLastIndex[r] = 0;
            }
            rankCards[r].Add(drawnCards[i]);
            rankLastIndex[r] = i + 1;
        }

        List<int> pairedRanks = new List<int>();
        foreach (var kv in rankCards)
        {
            if (kv.Value.Count >= 2)
                pairedRanks.Add(kv.Key);
        }

        if (pairedRanks.Count < 2) return GoalResult.Fail;

        pairedRanks.Sort((a, b) => rankLastIndex[a].CompareTo(rankLastIndex[b]));
        int firstRank = pairedRanks[pairedRanks.Count - 2];
        int secondRank = pairedRanks[pairedRanks.Count - 1];

        var winning = new List<Card>();
        winning.AddRange(rankCards[firstRank].GetRange(0, 2));
        winning.AddRange(rankCards[secondRank].GetRange(0, 2));

        return new GoalResult
        {
            isAchieved = true,
            flipCount = System.Math.Max(rankLastIndex[firstRank], rankLastIndex[secondRank]),
            winningCards = winning
        };
    }
}

public class Goal_ThreeOfAKind : GoalChecker
{
    public override string goalName => "三条";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, List<Card>> rankCards = new Dictionary<int, List<Card>>();
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (!rankCards.ContainsKey(r))
            {
                rankCards[r] = new List<Card>();
                rankLastIndex[r] = 0;
            }
            rankCards[r].Add(drawnCards[i]);
            rankLastIndex[r] = i + 1;

            if (rankCards[r].Count >= 3)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = rankLastIndex[r],
                    winningCards = new List<Card>(rankCards[r])
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_FiveSameSuit : GoalChecker
{
    public override string goalName => "五同花";
    public override int stage => 2;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, List<Card>> suitCards = new Dictionary<Card.Suit, List<Card>>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            if (!suitCards.ContainsKey(card.suit))
            {
                suitCards[card.suit] = new List<Card>();
                suitLastIndex[card.suit] = 0;
            }
            suitCards[card.suit].Add(card);
            suitLastIndex[card.suit] = i + 1;

            if (suitCards[card.suit].Count >= 5)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = suitLastIndex[card.suit],
                    winningCards = new List<Card>(suitCards[card.suit])
                };
            }
        }
        return GoalResult.Fail;
    }
}

// ==================== 第3层 ====================

public class Goal_FourConsecutive : GoalChecker
{
    public override string goalName => "四连顺";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            rankLastIndex[r] = i + 1;
            if (!rankSample.ContainsKey(r)) rankSample[r] = drawnCards[i];
        }

        var sorted = rankLastIndex.Keys.ToList();
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            int maxJ = i;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 1)
                {
                    consecutive++;
                    maxJ = j;
                }
                else break;
            }

            if (consecutive >= 4)
            {
                int lastFlip = 0;
                List<Card> winning = new List<Card>();
                for (int k = i; k <= maxJ; k++)
                {
                    int rank = sorted[k];
                    winning.Add(rankSample[rank]);
                    if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                }
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastFlip,
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_SixSameSuit : GoalChecker
{
    public override string goalName => "六同花";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, List<Card>> suitCards = new Dictionary<Card.Suit, List<Card>>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            if (!suitCards.ContainsKey(card.suit))
            {
                suitCards[card.suit] = new List<Card>();
                suitLastIndex[card.suit] = 0;
            }
            suitCards[card.suit].Add(card);
            suitLastIndex[card.suit] = i + 1;

            if (suitCards[card.suit].Count >= 6)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = suitLastIndex[card.suit],
                    winningCards = new List<Card>(suitCards[card.suit])
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_EvenFourConsecutive : GoalChecker
{
    public override string goalName => "偶数四连顺";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (r % 2 == 0)
            {
                rankLastIndex[r] = i + 1;
                if (!rankSample.ContainsKey(r)) rankSample[r] = drawnCards[i];
            }
        }

        var sorted = rankLastIndex.Keys.ToList();
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            int maxJ = i;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 2)
                {
                    consecutive++;
                    maxJ = j;
                }
                else break;
            }

            if (consecutive >= 4)
            {
                int lastFlip = 0;
                List<Card> winning = new List<Card>();
                for (int k = i; k <= maxJ; k++)
                {
                    int rank = sorted[k];
                    winning.Add(rankSample[rank]);
                    if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                }
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastFlip,
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_FiveConsecutive : GoalChecker
{
    public override string goalName => "五连顺";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            rankLastIndex[r] = i + 1;
            if (!rankSample.ContainsKey(r)) rankSample[r] = drawnCards[i];
        }

        var sorted = rankLastIndex.Keys.ToList();
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            int maxJ = i;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 1)
                {
                    consecutive++;
                    maxJ = j;
                }
                else break;
            }

            if (consecutive >= 5)
            {
                int lastFlip = 0;
                List<Card> winning = new List<Card>();
                for (int k = i; k <= maxJ; k++)
                {
                    int rank = sorted[k];
                    winning.Add(rankSample[rank]);
                    if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                }
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastFlip,
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_FaceCardThreeSameSuit : GoalChecker
{
    public override string goalName => "三人头同花";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, List<Card>> suitFaceCards = new Dictionary<Card.Suit, List<Card>>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            int r = (int)card.rank;
            if (r >= 11 && r <= 13)
            {
                if (!suitFaceCards.ContainsKey(card.suit))
                {
                    suitFaceCards[card.suit] = new List<Card>();
                    suitLastIndex[card.suit] = 0;
                }
                suitFaceCards[card.suit].Add(card);
                suitLastIndex[card.suit] = i + 1;

                if (suitFaceCards[card.suit].Count >= 3)
                {
                    return new GoalResult
                    {
                        isAchieved = true,
                        flipCount = suitLastIndex[card.suit],
                        winningCards = new List<Card>(suitFaceCards[card.suit])
                    };
                }
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_FourOfAKind : GoalChecker
{
    public override string goalName => "四条";
    public override int stage => 3;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, List<Card>> rankCards = new Dictionary<int, List<Card>>();
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (!rankCards.ContainsKey(r))
            {
                rankCards[r] = new List<Card>();
                rankLastIndex[r] = 0;
            }
            rankCards[r].Add(drawnCards[i]);
            rankLastIndex[r] = i + 1;

            if (rankCards[r].Count >= 4)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = rankLastIndex[r],
                    winningCards = new List<Card>(rankCards[r])
                };
            }
        }
        return GoalResult.Fail;
    }
}

// ==================== 第4层 & 第5层 ====================

public class Goal_FullHouse : GoalChecker
{
    public override string goalName => "葫芦";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, List<Card>> rankCards = new Dictionary<int, List<Card>>();
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (!rankCards.ContainsKey(r))
            {
                rankCards[r] = new List<Card>();
                rankLastIndex[r] = 0;
            }
            rankCards[r].Add(drawnCards[i]);
            rankLastIndex[r] = i + 1;
        }

        int threeRank = -1;
        foreach (var kv in rankCards)
        {
            if (kv.Value.Count >= 3)
            {
                threeRank = kv.Key;
                break;
            }
        }
        if (threeRank == -1) return GoalResult.Fail;

        int pairRank = -1;
        foreach (var kv in rankCards)
        {
            if (kv.Key != threeRank && kv.Value.Count >= 2)
            {
                pairRank = kv.Key;
                break;
            }
        }
        if (pairRank == -1) return GoalResult.Fail;

        var winning = new List<Card>();
        winning.AddRange(rankCards[threeRank].GetRange(0, 3));
        winning.AddRange(rankCards[pairRank].GetRange(0, 2));

        return new GoalResult
        {
            isAchieved = true,
            flipCount = System.Math.Max(rankLastIndex[threeRank], rankLastIndex[pairRank]),
            winningCards = winning
        };
    }
}

public class Goal_AllSuitsThree : GoalChecker
{
    public override string goalName => "四花各三张";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, List<Card>> suitCards = new Dictionary<Card.Suit, List<Card>>();
        int lastIndex = 0;

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            if (!suitCards.ContainsKey(card.suit))
                suitCards[card.suit] = new List<Card>();
            suitCards[card.suit].Add(card);
            lastIndex = i + 1;
        }

        if (suitCards.Count < 4) return GoalResult.Fail;

        foreach (var kv in suitCards)
        {
            if (kv.Value.Count < 3) return GoalResult.Fail;
        }

        var winning = new List<Card>();
        foreach (var kv in suitCards)
            winning.AddRange(kv.Value.GetRange(0, 3));

        return new GoalResult
        {
            isAchieved = true,
            flipCount = lastIndex,
            winningCards = winning
        };
    }
}

public class Goal_SixConsecutive : GoalChecker
{
    public override string goalName => "六连顺";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            rankLastIndex[r] = i + 1;
            if (!rankSample.ContainsKey(r)) rankSample[r] = drawnCards[i];
        }

        var sorted = rankLastIndex.Keys.ToList();
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            int maxJ = i;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j] == sorted[j - 1] + 1)
                {
                    consecutive++;
                    maxJ = j;
                }
                else break;
            }

            if (consecutive >= 6)
            {
                int lastFlip = 0;
                List<Card> winning = new List<Card>();
                for (int k = i; k <= maxJ; k++)
                {
                    int rank = sorted[k];
                    winning.Add(rankSample[rank]);
                    if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                }
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = lastFlip,
                    winningCards = winning
                };
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_AllRanks : GoalChecker
{
    public override string goalName => "全套点数";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        HashSet<int> ranks = new HashSet<int>();
        Dictionary<int, Card> rankSample = new Dictionary<int, Card>();
        int lastIndex = -1;

        for (int i = 0; i < drawnCards.Count; i++)
        {
            int r = (int)drawnCards[i].rank;
            if (!ranks.Contains(r))
            {
                ranks.Add(r);
                rankSample[r] = drawnCards[i];
            }
            lastIndex = i + 1;
        }

        if (ranks.Count < 13) return GoalResult.Fail;

        var winning = new List<Card>();
        foreach (var kv in rankSample)
            winning.Add(kv.Value);

        return new GoalResult
        {
            isAchieved = true,
            flipCount = lastIndex,
            winningCards = winning
        };
    }
}

public class Goal_StraightFlush : GoalChecker
{
    public override string goalName => "同花顺";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, List<Card>> suitCards = new Dictionary<Card.Suit, List<Card>>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            if (!suitCards.ContainsKey(card.suit))
            {
                suitCards[card.suit] = new List<Card>();
                suitLastIndex[card.suit] = 0;
            }
            suitCards[card.suit].Add(card);
            suitLastIndex[card.suit] = i + 1;
        }

        foreach (var kv in suitCards)
        {
            if (kv.Value.Count < 5) continue;

            HashSet<int> ranks = new HashSet<int>();
            Dictionary<int, Card> rankSample = new Dictionary<int, Card>();
            Dictionary<int, int> rankLastIndex = new Dictionary<int, int>();

            foreach (var card in kv.Value)
            {
                int r = (int)card.rank;
                ranks.Add(r);
                if (!rankSample.ContainsKey(r)) rankSample[r] = card;
                rankLastIndex[r] = suitLastIndex[kv.Key];
            }

            var sorted = ranks.ToList();
            sorted.Sort();

            for (int i = 0; i < sorted.Count; i++)
            {
                int consecutive = 1;
                int maxJ = i;
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    if (sorted[j] == sorted[j - 1] + 1)
                    {
                        consecutive++;
                        maxJ = j;
                    }
                    else break;
                }

                if (consecutive >= 5)
                {
                    int lastFlip = 0;
                    List<Card> winning = new List<Card>();
                    for (int k = i; k <= maxJ; k++)
                    {
                        int rank = sorted[k];
                        winning.Add(rankSample[rank]);
                        if (rankLastIndex[rank] > lastFlip) lastFlip = rankLastIndex[rank];
                    }
                    return new GoalResult
                    {
                        isAchieved = true,
                        flipCount = lastFlip,
                        winningCards = winning
                    };
                }
            }
        }
        return GoalResult.Fail;
    }
}

public class Goal_RoyalFlush : GoalChecker
{
    public override string goalName => "皇家同花顺";
    public override int stage => 4;

    public override GoalResult Check(List<Card> drawnCards)
    {
        int[] royalRanks = { 1, 10, 11, 12, 13 };

        Dictionary<Card.Suit, List<Card>> suitRoyalCards = new Dictionary<Card.Suit, List<Card>>();
        Dictionary<Card.Suit, int> suitLastIndex = new Dictionary<Card.Suit, int>();

        for (int i = 0; i < drawnCards.Count; i++)
        {
            var card = drawnCards[i];
            int r = (int)card.rank;
            bool isRoyal = false;
            foreach (int rr in royalRanks)
            {
                if (r == rr) { isRoyal = true; break; }
            }
            if (!isRoyal) continue;

            if (!suitRoyalCards.ContainsKey(card.suit))
            {
                suitRoyalCards[card.suit] = new List<Card>();
                suitLastIndex[card.suit] = 0;
            }
            suitRoyalCards[card.suit].Add(card);
            suitLastIndex[card.suit] = i + 1;
        }

        foreach (var kv in suitRoyalCards)
        {
            if (kv.Value.Count < 5) continue;

            HashSet<int> ranks = new HashSet<int>();
            foreach (var card in kv.Value)
                ranks.Add((int)card.rank);

            bool hasAll = true;
            foreach (int rr in royalRanks)
            {
                if (!ranks.Contains(rr)) { hasAll = false; break; }
            }

            if (hasAll)
            {
                return new GoalResult
                {
                    isAchieved = true,
                    flipCount = suitLastIndex[kv.Key],
                    winningCards = new List<Card>(kv.Value)
                };
            }
        }
        return GoalResult.Fail;
    }
}

// ==================== 目标池 ====================
public static class GoalPool
{
    private static Dictionary<int, List<GoalChecker>> pool;

    static GoalPool()
    {
        pool = new Dictionary<int, List<GoalChecker>>();

        pool[1] = new List<GoalChecker>
        {
            new Goal_ThreeOdds(), new Goal_SmallThreeConsecutive(),
            new Goal_ThreeSameSuit(), new Goal_RedBlack33(),
            new Goal_AllFourSuits(), new Goal_ThreeConsecutive()
        };

        pool[2] = new List<GoalChecker>
        {
            new Goal_FourOdds(), new Goal_SmallFourConsecutive(),
            new Goal_ThreeFaceCards(), new Goal_TwoPair(),
            new Goal_ThreeOfAKind(), new Goal_FiveSameSuit()
        };

        pool[3] = new List<GoalChecker>
        {
            new Goal_FourConsecutive(), new Goal_SixSameSuit(),
            new Goal_EvenFourConsecutive(), new Goal_FiveConsecutive(),
            new Goal_FaceCardThreeSameSuit(), new Goal_FourOfAKind()
        };

        pool[4] = new List<GoalChecker>
        {
            new Goal_FullHouse(), new Goal_AllSuitsThree(),
            new Goal_SixConsecutive(), new Goal_AllRanks(),
            new Goal_StraightFlush(), new Goal_RoyalFlush()
        };
    }

    public static GoalChecker GetRandomGoal(int stage)
    {
        int poolStage = stage == 5 ? 4 : stage;
        if (!pool.ContainsKey(poolStage)) return null;
        var list = pool[poolStage];
        return list[Random.Range(0, list.Count)];
    }
}