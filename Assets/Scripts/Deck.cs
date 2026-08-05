using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    private List<Card> currentDeck;
    private List<Card> drawnCards;
    private bool nextDrawFaded;

    void Awake()
    {
        currentDeck = new List<Card>();
        drawnCards = new List<Card>();
        nextDrawFaded = false;
    }

    public List<Card> CreateStandardDeck()
    {
        List<Card> deck = new List<Card>();

        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            foreach (Card.Rank rank in System.Enum.GetValues(typeof(Card.Rank)))
            {
                deck.Add(new Card(suit, rank));
            }
        }

        Debug.Assert(deck.Count == 52, $"牌堆应有52张，实际{deck.Count}张");
        return deck;
    }

    public void ShuffleAndInit()
    {
        currentDeck = CreateStandardDeck();
        drawnCards.Clear();
        nextDrawFaded = false;

        System.Random rng = new System.Random();
        int n = currentDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = currentDeck[k];
            currentDeck[k] = currentDeck[n];
            currentDeck[n] = temp;
        }

        Debug.Log($"洗牌完成，牌堆共{currentDeck.Count}张");
    }

    public void MarkNextDrawFaded()
    {
        nextDrawFaded = true;
    }

    public Card DrawTopCard(bool isFaded = false)
    {
        if (currentDeck.Count == 0)
        {
            Debug.LogWarning("牌堆已空，无法翻牌");
            return null;
        }

        Card drawn = currentDeck[0];
        currentDeck.RemoveAt(0);

        bool shouldFade = isFaded || nextDrawFaded;
        nextDrawFaded = false;

        if (shouldFade)
        {
            drawn.isFaded = true;
        }

        drawnCards.Add(drawn);

        Debug.Log($"翻开: {drawn} {(shouldFade ? "[褪色]" : "")}，剩余{currentDeck.Count}张");
        return drawn;
    }

    public void RevealFadedCard(int index)
    {
        if (index >= 0 && index < currentDeck.Count)
        {
            currentDeck[index].isFaded = false;
            Debug.Log($"恢复显示: {currentDeck[index]}");
        }
    }

    public List<Card> PeekTop(int count)
    {
        int peekCount = Mathf.Min(count, currentDeck.Count);
        return currentDeck.GetRange(0, peekCount);
    }

    public List<Card> PeekMiddle(int count)
    {
        if (currentDeck.Count == 0) return new List<Card>();

        int startIndex = currentDeck.Count / 2 - count / 2;
        startIndex = Mathf.Clamp(startIndex, 0, currentDeck.Count - 1);
        int peekCount = Mathf.Min(count, currentDeck.Count - startIndex);

        return currentDeck.GetRange(startIndex, peekCount);
    }

    public List<Card> PeekBottom(int count)
    {
        int peekCount = Mathf.Min(count, currentDeck.Count);
        int startIndex = currentDeck.Count - peekCount;
        return currentDeck.GetRange(startIndex, peekCount);
    }

    public List<Card> PeekRandomUnrevealed(int count)
    {
        if (currentDeck.Count == 0) return new List<Card>();

        var rng = new System.Random();
        int actual = Mathf.Min(count, currentDeck.Count);
        var indices = new HashSet<int>();
        var result = new List<Card>();

        while (result.Count < actual)
        {
            int idx = rng.Next(currentDeck.Count);
            if (indices.Add(idx))
            {
                result.Add(currentDeck[idx]);
            }
        }

        return result;
    }

    public void RemoveTop(int count)
    {
        int removeCount = Mathf.Min(count, currentDeck.Count);
        currentDeck.RemoveRange(0, removeCount);
        Debug.Log($"删除顶部{removeCount}张，剩余{currentDeck.Count}张");
    }

    public void MoveTopToBottom(int count)
    {
        int moveCount = Mathf.Min(count, currentDeck.Count);
        List<Card> topCards = currentDeck.GetRange(0, moveCount);
        currentDeck.RemoveRange(0, moveCount);
        currentDeck.AddRange(topCards);
        Debug.Log($"顶部{moveCount}张沉底");
    }

    public void MoveBottomToTop(int count)
    {
        int moveCount = Mathf.Min(count, currentDeck.Count);
        List<Card> bottomCards = currentDeck.GetRange(currentDeck.Count - moveCount, moveCount);
        currentDeck.RemoveRange(currentDeck.Count - moveCount, moveCount);
        currentDeck.InsertRange(0, bottomCards);
        Debug.Log($"底部{moveCount}张移至顶部");
    }

    public void SplitAndSwap()
    {
        int mid = currentDeck.Count / 2;
        List<Card> topHalf = currentDeck.GetRange(0, mid);
        List<Card> bottomHalf = currentDeck.GetRange(mid, currentDeck.Count - mid);

        currentDeck.Clear();
        currentDeck.AddRange(bottomHalf);
        currentDeck.AddRange(topHalf);
        Debug.Log("切牌完成");
    }

    public void ReverseDeck()
    {
        currentDeck.Reverse();
        Debug.Log("牌堆顺序已翻转");
    }

    public void ReshuffleRemaining()
    {
        System.Random rng = new System.Random();
        int n = currentDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = currentDeck[k];
            currentDeck[k] = currentDeck[n];
            currentDeck[n] = temp;
        }
        Debug.Log($"剩余牌堆已重洗，共{currentDeck.Count}张");
    }

    /// <summary>
    /// 从已翻牌中删除指定数量的牌（从末尾开始），翻牌计数自动减少
    /// </summary>
    public void RemoveFromDrawn(int count)
    {
        int removeCount = Mathf.Min(count, drawnCards.Count);
        for (int i = 0; i < removeCount; i++)
        {
            int lastIndex = drawnCards.Count - 1;
            Debug.Log($"删除已翻牌: {drawnCards[lastIndex]}");
            drawnCards.RemoveAt(lastIndex);
        }
        Debug.Log($"已翻牌删除{removeCount}张，当前计数: {drawnCards.Count}");
    }

    /// <summary>
    /// 将指定数量的已翻牌洗回剩余牌堆（从末尾开始），翻牌计数自动减少
    /// </summary>
    public void ReturnDrawnToDeck(int count)
    {
        int returnCount = Mathf.Min(count, drawnCards.Count);
        List<Card> returnCards = drawnCards.GetRange(drawnCards.Count - returnCount, returnCount);
        drawnCards.RemoveRange(drawnCards.Count - returnCount, returnCount);
        currentDeck.AddRange(returnCards);
        ReshuffleRemaining();
        Debug.Log($"{returnCount}张已翻牌洗回，当前计数: {drawnCards.Count}");
    }

    /// <summary>
    /// 复制一张已翻牌并加入剩余牌堆
    /// </summary>
    public bool CopyDrawnCard(int index, bool toTop = false)
    {
        if (index < 0 || index >= drawnCards.Count) return false;
        Card original = drawnCards[index];
        Card copy = new Card(original.suit, original.rank);
        if (toTop)
            currentDeck.Insert(0, copy);
        else
            currentDeck.Add(copy);
        Debug.Log($"复制已翻牌: {copy}，置顶={toTop}");
        return true;
    }

    public List<Card> GetDrawnCards()
    {
        return new List<Card>(drawnCards);
    }

    public List<Card> GetRemainingDeck()
    {
        return new List<Card>(currentDeck);
    }

    public int GetRemainingCount()
    {
        return currentDeck.Count;
    }

    public int GetDrawnCount()
    {
        return drawnCards.Count;
    }

    public void ReturnLastDrawnToDeck(int count)
    {
        int returnCount = Mathf.Min(count, drawnCards.Count);
        List<Card> returnCards = drawnCards.GetRange(drawnCards.Count - returnCount, returnCount);
        drawnCards.RemoveRange(drawnCards.Count - returnCount, returnCount);

        currentDeck.AddRange(returnCards);
        ReshuffleRemaining();
        Debug.Log($"最后{returnCount}张已翻牌洗回牌堆");
    }

    public void RandomRemoveConsecutive(int count)
    {
        if (currentDeck.Count <= count)
        {
            Debug.LogWarning("牌堆不足，无法移除");
            return;
        }

        System.Random rng = new System.Random();
        int startIndex = rng.Next(0, currentDeck.Count - count);
        currentDeck.RemoveRange(startIndex, count);
        Debug.Log($"随机移除从索引{startIndex}开始的连续{count}张，剩余{currentDeck.Count}张");
    }

    public bool MoveCardToTop(int positionFromTop)
    {
        if (positionFromTop < 0 || positionFromTop >= currentDeck.Count)
        {
            Debug.LogWarning($"[牌堆] 无效位置: {positionFromTop}");
            return false;
        }

        Card targetCard = currentDeck[positionFromTop];
        currentDeck.RemoveAt(positionFromTop);
        currentDeck.Insert(0, targetCard);
        Debug.Log($"[牌堆] 将第{positionFromTop + 1}张 [{targetCard}] 移至顶部");
        return true;
    }
}