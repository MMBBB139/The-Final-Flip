using System.Collections.Generic;
using UnityEngine;


public class DeckManager : MonoBehaviour
{
    public List<Card> deck = new List<Card>(52);   // 牌堆
    public List<Card> drawnCards = new List<Card>(); // 已翻开的牌

    void Awake()
    {
        BuildDeck();
    }

    // 构建全新的52张牌
    public void BuildDeck()
    {
        deck.Clear();
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            for (Card.Rank rank = Card.Rank.Ace; rank <= Card.Rank.King; rank++)
            {
                deck.Add(new Card(suit, rank));
            }
        }
    }

    // 洗牌
    public void Shuffle()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    // 翻指定数量的牌
    public List<Card> Draw(int count)
    {
        List<Card> drawn = new List<Card>();
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            Card card = deck[0];
            card.isFaceUp = true;
            deck.RemoveAt(0);
            drawnCards.Add(card);
            drawn.Add(card);
        }
        return drawn;
    }
}