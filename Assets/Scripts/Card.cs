// Card.cs

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
    public bool isFaded;  // 新增：褪色牌标记（隐藏花色点数，但结算有效）

    public Card(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
        this.color = (suit == Suit.Hearts || suit == Suit.Diamonds) ? CardColor.Red : CardColor.Black;
        this.isFaded = false;
    }

    public override string ToString()
    {
        string fadedMark = isFaded ? "[褪色]" : "";
        return $"{fadedMark}{rank} of {suit} ({color})";
    }
}