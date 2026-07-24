// Deck.cs
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
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

        // 验证牌堆数量
        Debug.Assert(deck.Count == 52, $"牌堆应有52张，实际{deck.Count}张");

        return deck;
    }

    void Start()
    {
        // 测试用
        List<Card> deck = CreateStandardDeck();
        foreach (Card card in deck)
        {
            Debug.Log(card);
        }
    }
}