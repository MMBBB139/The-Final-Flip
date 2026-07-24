// Deck.cs
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    private List<Card> currentDeck; // 当前牌堆顺序（未翻开的）
    private List<Card> drawnCards;  // 已翻开的牌

    void Awake()
    {
        currentDeck = new List<Card>();
        drawnCards = new List<Card>();
    }

    /// <summary>
    /// 创建一副标准52张扑克牌
    /// </summary>
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

    /// <summary>
    /// 洗牌并初始化牌堆（Fisher-Yates洗牌算法）
    /// </summary>
    public void ShuffleAndInit()
    {
        // 生成新牌堆
        currentDeck = CreateStandardDeck();
        drawnCards.Clear();

        // Fisher-Yates洗牌
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

    /// <summary>
    /// 从牌堆顶部翻一张牌，加入已翻牌区
    /// </summary>
    public Card DrawTopCard()
    {
        if (currentDeck.Count == 0)
        {
            Debug.LogWarning("牌堆已空，无法翻牌");
            return null;
        }

        Card drawn = currentDeck[0];
        currentDeck.RemoveAt(0);
        drawnCards.Add(drawn);

        Debug.Log($"翻开: {drawn}，剩余{currentDeck.Count}张");
        return drawn;
    }

    /// <summary>
    /// 查看牌堆顶部指定数量（不翻牌）
    /// </summary>
    public List<Card> PeekTop(int count)
    {
        int peekCount = Mathf.Min(count, currentDeck.Count);
        return currentDeck.GetRange(0, peekCount);
    }

    /// <summary>
    /// 查看牌堆中间指定数量（从中间位置开始）
    /// </summary>
    public List<Card> PeekMiddle(int count)
    {
        if (currentDeck.Count == 0) return new List<Card>();

        int startIndex = currentDeck.Count / 2 - count / 2;
        startIndex = Mathf.Clamp(startIndex, 0, currentDeck.Count - 1);
        int peekCount = Mathf.Min(count, currentDeck.Count - startIndex);

        return currentDeck.GetRange(startIndex, peekCount);
    }

    /// <summary>
    /// 查看牌堆底部指定数量（不翻牌）
    /// </summary>
    public List<Card> PeekBottom(int count)
    {
        int peekCount = Mathf.Min(count, currentDeck.Count);
        int startIndex = currentDeck.Count - peekCount;
        return currentDeck.GetRange(startIndex, peekCount);
    }

    /// <summary>
    /// 删除牌堆顶部指定数量
    /// </summary>
    public void RemoveTop(int count)
    {
        int removeCount = Mathf.Min(count, currentDeck.Count);
        currentDeck.RemoveRange(0, removeCount);
        Debug.Log($"删除顶部{removeCount}张，剩余{currentDeck.Count}张");
    }

    /// <summary>
    /// 从底部取指定数量插入顶部
    /// </summary>
    public void MoveBottomToTop(int count)
    {
        int moveCount = Mathf.Min(count, currentDeck.Count);
        List<Card> bottomCards = currentDeck.GetRange(currentDeck.Count - moveCount, moveCount);
        currentDeck.RemoveRange(currentDeck.Count - moveCount, moveCount);
        currentDeck.InsertRange(0, bottomCards);
        Debug.Log($"底部{moveCount}张移至顶部");
    }

    /// <summary>
    /// 对半分并交换位置
    /// </summary>
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

    /// <summary>
    /// 翻转剩余牌堆顺序
    /// </summary>
    public void ReverseDeck()
    {
        currentDeck.Reverse();
        Debug.Log("牌堆顺序已翻转");
    }

    /// <summary>
    /// 彻底重新洗牌剩余牌堆
    /// </summary>
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
    /// 获取已翻牌列表（只读）
    /// </summary>
    public List<Card> GetDrawnCards()
    {
        return new List<Card>(drawnCards);
    }

    /// <summary>
    /// 获取剩余牌堆（只读）
    /// </summary>
    public List<Card> GetRemainingDeck()
    {
        return new List<Card>(currentDeck);
    }

    /// <summary>
    /// 获取剩余牌堆数量
    /// </summary>
    public int GetRemainingCount()
    {
        return currentDeck.Count;
    }

    /// <summary>
    /// 获取已翻牌数量
    /// </summary>
    public int GetDrawnCount()
    {
        return drawnCards.Count;
    }

    /// <summary>
    /// 将已翻牌的最后几张洗回剩余牌堆（特殊规则用）
    /// </summary>
    public void ReturnLastDrawnToDeck(int count)
    {
        int returnCount = Mathf.Min(count, drawnCards.Count);
        List<Card> returnCards = drawnCards.GetRange(drawnCards.Count - returnCount, returnCount);
        drawnCards.RemoveRange(drawnCards.Count - returnCount, returnCount);

        // 洗回剩余牌堆并打乱
        currentDeck.AddRange(returnCards);
        ReshuffleRemaining();
        Debug.Log($"最后{returnCount}张已翻牌洗回牌堆");
    }

    /// <summary>
    /// 随机连续移除指定数量（第2层全局规则用）
    /// </summary>
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
}