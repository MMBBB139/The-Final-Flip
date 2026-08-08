public struct Card
{
    public enum Suit
    {
        Spades,     // ♠ 黑桃
        Hearts,     // ♥ 红心
        Clubs,      // ♣ 梅花
        Diamonds    // ♦ 方片
    }

    public enum Rank
    {
        Ace = 1, Two, Three, Four, Five, Six, Seven,
        Eight, Nine, Ten, Jack, Queen, King
    }

    public Suit suit;
    public Rank rank;
    public bool isFaceUp;
    public bool isFaded;

    public Card(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
        isFaceUp = false;
        isFaded = false;
    }
}