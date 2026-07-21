// CardDeck.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>管理一整副52张牌的抽牌堆、弃牌堆以及当前关卡已翻开的牌</summary>
public class CardDeck : MonoBehaviour
{
    [Header("牌堆状态")]
    public List<Card> drawPile = new List<Card>();          // 抽牌堆
    public List<Card> discardPile = new List<Card>();       // 弃牌堆
    public List<Card> revealedCardsThisLevel = new List<Card>(); // 本关已按顺序翻开的牌

    [Header("设置")]
    [SerializeField] private bool autoShuffleOnCreate = true;

    private System.Random rng = new System.Random();

    private void Awake()
    {
        if (autoShuffleOnCreate)
            InitializeStandardDeck();
    }

    /// <summary>创建一副完整的52张标准扑克牌（无大小王）并洗牌</summary>
    public void InitializeStandardDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        revealedCardsThisLevel.Clear();

        // 生成所有花色和点数的组合
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                drawPile.Add(new Card(suit, rank));
            }
        }
        Shuffle(drawPile);
    }

    /// <summary>Fisher-Yates洗牌</summary>
    public void Shuffle(List<Card> pile)
    {
        int n = pile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = pile[k];
            pile[k] = pile[n];
            pile[n] = temp;
        }
    }

    /// <summary>将弃牌堆的所有牌洗回抽牌堆，并清空弃牌堆</summary>
    public void ShuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    /// <summary>从抽牌堆顶部抽取一张牌，自动加入已翻牌列表</summary>
    /// <returns>抽到的牌，若抽牌堆为空则返回null</returns>
    public Card DrawTop()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("抽牌堆已空！");
            return null;
        }

        Card drawn = drawPile[0];
        drawPile.RemoveAt(0);
        revealedCardsThisLevel.Add(drawn);
        GameEvents.RaiseCardDrawn(drawn);
        return drawn;
    }

    /// <summary>查看抽牌堆顶部第n张牌（不抽取），n从0开始</summary>
    public Card Peek(int indexFromTop = 0)
    {
        if (drawPile.Count <= indexFromTop) return null;
        return drawPile[indexFromTop];
    }

    /// <summary>将一张牌放入弃牌堆</summary>
    public void Discard(Card card)
    {
        if (card == null) return;
        discardPile.Add(card);
    }

    /// <summary>将一张牌插入抽牌堆的指定位置（0为顶部）</summary>
    public void InsertToDrawPile(Card card, int position)
    {
        if (card == null) return;
        position = Mathf.Clamp(position, 0, drawPile.Count);
        drawPile.Insert(position, card);
    }

    /// <summary>从抽牌堆中移除一张指定牌（用于道具销毁）</summary>
    public bool RemoveFromDrawPile(Card card)
    {
        return drawPile.Remove(card);
    }

    /// <summary>重置当前关卡状态（清空已翻牌记录，弃牌堆回抽牌堆，重新洗牌）</summary>
    public void ResetForNewLevel()
    {
        revealedCardsThisLevel.Clear();
        if (discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
        }
        Shuffle(drawPile);
    }

    /// <summary>获取抽牌堆中剩余牌的数量</summary>
    public int DrawPileCount => drawPile.Count;
    /// <summary>获取本关已翻开牌的数量</summary>
    public int RevealedCount => revealedCardsThisLevel.Count;
}