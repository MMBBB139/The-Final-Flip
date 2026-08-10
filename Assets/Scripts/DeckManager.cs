using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    public List<Card> deck = new List<Card>(52);
    public List<Card> drawnCards = new List<Card>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildDeck();
    }

    public void BuildDeck()
    {
        deck.Clear();
        drawnCards.Clear();
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            for (Card.Rank rank = Card.Rank.Ace; rank <= Card.Rank.King; rank++)
            {
                deck.Add(new Card(suit, rank));
            }
        }
    }

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

    public List<Card> PeekTop(int count)
    {
        List<Card> result = new List<Card>();
        for (int i = 0; i < count && i < deck.Count; i++)
            result.Add(deck[i]);
        return result;
    }

    public List<Card> PeekBottom(int count)
    {
        List<Card> result = new List<Card>();
        for (int i = deck.Count - 1; i >= 0 && result.Count < count; i--)
            result.Add(deck[i]);
        return result;
    }

    public void MoveToTop(Card card)
    {
        deck.Remove(card);
        deck.Insert(0, card);
    }

    public void MoveToBottom(Card card)
    {
        deck.Remove(card);
        deck.Add(card);
    }

    public void DuplicateAndShuffleIn(Card card)
    {
        Card copy = new Card(card.suit, card.rank);
        int index = Random.Range(0, deck.Count + 1);
        deck.Insert(index, copy);
    }

    public void DuplicateToTop(Card card)
    {
        Card copy = new Card(card.suit, card.rank);
        deck.Insert(0, copy);
    }

    public void RemoveDrawnCard(Card card)
    {
        drawnCards.Remove(card);
    }

    public void ChangeSuit(Card card, Card.Suit newSuit)
    {
        card.suit = newSuit;
    }

    public void ChangeRank(Card card, Card.Rank newRank)
    {
        card.rank = newRank;
    }
}