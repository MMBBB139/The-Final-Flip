using System;

[Serializable]
public class Card
{
    public enum Suit
    {
        Spades,     // 黑桃 ♠
        Hearts,     // 红心 ♥
        Clubs,      // 梅花 ♣
        Diamonds    // 方块 ♦
    }

    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    public enum CardColor
    {
        Red,
        Black
    }

    public Suit suit;
    public Rank rank;
    public CardColor color;

    public Card(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
        this.color = (suit == Suit.Hearts || suit == Suit.Diamonds) ? CardColor.Red : CardColor.Black;
    }

    public override string ToString()
    {
        return $"{rank} of {suit} ({color})";
    }
}