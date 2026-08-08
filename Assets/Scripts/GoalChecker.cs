using System.Collections.Generic;
using System.Linq;

// ==================== 基类 ====================
public abstract class GoalChecker
{
    public abstract string goalName { get; }
    public abstract bool IsAchieved(List<Card> drawnCards);

    // 工具方法：检测是否有 needCount 张相同花色
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

    // 工具方法：检测是否有 needCount 张点数连续的牌
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

    // 工具方法：检测某个点数在已翻牌中出现了至少 needCount 次
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

    // 工具方法：统计满足条件的牌数量
    protected static int CountCards(List<Card> cards, System.Predicate<Card> predicate)
    {
        int count = 0;
        foreach (var card in cards)
        {
            if (predicate(card)) count++;
        }
        return count;
    }
}

// ==================== 第1层 ====================

public class Goal_ThreeOdds : GoalChecker
{
    public override string goalName => "三奇数";
    public override bool IsAchieved(List<Card> drawnCards)
        => CountCards(drawnCards, c => ((int)c.rank % 2 == 1)) >= 3;
}

public class Goal_SmallThreeConsecutive : GoalChecker
{
    public override string goalName => "小三连";
    public override bool IsAchieved(List<Card> drawnCards)
        => CountCards(drawnCards, c => ((int)c.rank <= 4)) >= 3;
}

public class Goal_ThreeSameSuit : GoalChecker
{
    public override string goalName => "同花三张";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasSameSuitCount(drawnCards, 3);
}

public class Goal_RedBlack33 : GoalChecker
{
    public override string goalName => "红黑3-3";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        int red = CountCards(drawnCards, c => c.suit == Card.Suit.Hearts || c.suit == Card.Suit.Diamonds);
        int black = drawnCards.Count - red;
        return red >= 3 && black >= 3;
    }
}

public class Goal_AllFourSuits : GoalChecker
{
    public override string goalName => "四色齐";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        HashSet<Card.Suit> suits = new HashSet<Card.Suit>();
        foreach (var card in drawnCards)
            suits.Add(card.suit);
        return suits.Count >= 4;
    }
}

public class Goal_ThreeConsecutive : GoalChecker
{
    public override string goalName => "三连数";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasConsecutiveRanks(drawnCards, 3);
}

// ==================== 第2层 ====================

public class Goal_FourOdds : GoalChecker
{
    public override string goalName => "四奇数";
    public override bool IsAchieved(List<Card> drawnCards)
        => CountCards(drawnCards, c => ((int)c.rank % 2 == 1)) >= 4;
}

public class Goal_SmallFourConsecutive : GoalChecker
{
    public override string goalName => "小四连";
    public override bool IsAchieved(List<Card> drawnCards)
        => CountCards(drawnCards, c => ((int)c.rank <= 6)) >= 4;
}

public class Goal_ThreeFaceCards : GoalChecker
{
    public override string goalName => "人头三张";
    public override bool IsAchieved(List<Card> drawnCards)
        => CountCards(drawnCards, c => ((int)c.rank >= 11 && (int)c.rank <= 13)) >= 3;
}

public class Goal_TwoPair : GoalChecker
{
    public override string goalName => "两对";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        Dictionary<int, int> rankCount = new Dictionary<int, int>();
        foreach (var card in drawnCards)
        {
            int r = (int)card.rank;
            if (!rankCount.ContainsKey(r)) rankCount[r] = 0;
            rankCount[r]++;
        }
        int pairs = 0;
        foreach (var kv in rankCount)
        {
            if (kv.Value >= 2) pairs++;
        }
        return pairs >= 2;
    }
}

public class Goal_ThreeOfAKind : GoalChecker
{
    public override string goalName => "三条";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasSameRankCount(drawnCards, 3);
}

public class Goal_FiveSameSuit : GoalChecker
{
    public override string goalName => "五同花";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasSameSuitCount(drawnCards, 5);
}

// ==================== 第3层 ====================

public class Goal_FourConsecutive : GoalChecker
{
    public override string goalName => "四连数";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasConsecutiveRanks(drawnCards, 4);
}

public class Goal_SixSameSuit : GoalChecker
{
    public override string goalName => "六同花";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasSameSuitCount(drawnCards, 6);
}

public class Goal_EvenFourConsecutive : GoalChecker
{
    public override string goalName => "偶数四连";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        HashSet<int> evenRanks = new HashSet<int>();
        foreach (var card in drawnCards)
        {
            int val = (int)card.rank;
            if (val % 2 == 0) evenRanks.Add(val);
        }

        List<int> sorted = new List<int>(evenRanks);
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++)
        {
            int consecutive = 1;
            for (int j = i + 1; j < sorted.Count; j++)
            {
                // 偶数连续：2,4,6,8... 差值都是2
                if (sorted[j] == sorted[j - 1] + 2)
                    consecutive++;
                else
                    break;
            }
            if (consecutive >= 4) return true;
        }
        return false;
    }
}

public class Goal_FiveConsecutive : GoalChecker
{
    public override string goalName => "五连数";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasConsecutiveRanks(drawnCards, 5);
}

public class Goal_FaceCardThreeSameSuit : GoalChecker
{
    public override string goalName => "人头同花三张";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, int> faceCount = new Dictionary<Card.Suit, int>();
        foreach (var card in drawnCards)
        {
            int val = (int)card.rank;
            if (val >= 11 && val <= 13)
            {
                if (!faceCount.ContainsKey(card.suit)) faceCount[card.suit] = 0;
                faceCount[card.suit]++;
                if (faceCount[card.suit] >= 3) return true;
            }
        }
        return false;
    }
}

public class Goal_FourOfAKind : GoalChecker
{
    public override string goalName => "四条";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasSameRankCount(drawnCards, 4);
}

// ==================== 第4层 & 第5层 ====================

public class Goal_FullHouse : GoalChecker
{
    public override string goalName => "葫芦";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        Dictionary<int, int> rankCount = new Dictionary<int, int>();
        foreach (var card in drawnCards)
        {
            int r = (int)card.rank;
            if (!rankCount.ContainsKey(r)) rankCount[r] = 0;
            rankCount[r]++;
        }

        // 先找有没有出现至少3次的点数
        int threeRank = -1;
        foreach (var kv in rankCount)
        {
            if (kv.Value >= 3)
            {
                threeRank = kv.Key;
                break;
            }
        }
        if (threeRank == -1) return false;

        // 再找有没有不同的点数出现至少2次
        foreach (var kv in rankCount)
        {
            if (kv.Key != threeRank && kv.Value >= 2)
                return true;
        }
        return false;
    }
}

public class Goal_AllSuitsThree : GoalChecker
{
    public override string goalName => "四色各三";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        Dictionary<Card.Suit, int> suitCount = new Dictionary<Card.Suit, int>();
        foreach (var card in drawnCards)
        {
            if (!suitCount.ContainsKey(card.suit)) suitCount[card.suit] = 0;
            suitCount[card.suit]++;
        }
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            if (!suitCount.ContainsKey(suit) || suitCount[suit] < 3)
                return false;
        }
        return true;
    }
}

public class Goal_SixConsecutive : GoalChecker
{
    public override string goalName => "六连数";
    public override bool IsAchieved(List<Card> drawnCards)
        => HasConsecutiveRanks(drawnCards, 6);
}

public class Goal_AllRanks : GoalChecker
{
    public override string goalName => "全套点数";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        HashSet<int> ranks = new HashSet<int>();
        foreach (var card in drawnCards)
            ranks.Add((int)card.rank);
        return ranks.Count >= 13;
    }
}

public class Goal_StraightFlush : GoalChecker
{
    public override string goalName => "同花顺";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        // 按花色分组
        Dictionary<Card.Suit, List<Card>> suitGroups = new Dictionary<Card.Suit, List<Card>>();
        foreach (var card in drawnCards)
        {
            if (!suitGroups.ContainsKey(card.suit))
                suitGroups[card.suit] = new List<Card>();
            suitGroups[card.suit].Add(card);
        }

        // 每个花色单独检查是否有5连数
        foreach (var kv in suitGroups)
        {
            if (HasConsecutiveRanks(kv.Value, 5)) return true;
        }
        return false;
    }
}

public class Goal_RoyalFlush : GoalChecker
{
    public override string goalName => "皇家同花顺";
    public override bool IsAchieved(List<Card> drawnCards)
    {
        // 皇家同花顺需要的点数：10, J, Q, K, A (即10, 11, 12, 13, 1)
        int[] royalRanks = { 10, 11, 12, 13, 1 };

        Dictionary<Card.Suit, HashSet<int>> suitRanks = new Dictionary<Card.Suit, HashSet<int>>();
        foreach (var card in drawnCards)
        {
            if (!suitRanks.ContainsKey(card.suit))
                suitRanks[card.suit] = new HashSet<int>();
            suitRanks[card.suit].Add((int)card.rank);
        }

        foreach (var kv in suitRanks)
        {
            bool hasAll = true;
            foreach (int r in royalRanks)
            {
                if (!kv.Value.Contains(r))
                {
                    hasAll = false;
                    break;
                }
            }
            if (hasAll) return true;
        }
        return false;
    }
}