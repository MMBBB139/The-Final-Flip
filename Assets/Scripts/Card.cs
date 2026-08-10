public class Card
{
    public enum Suit
    {
        Spades,
        Hearts,
        Clubs,
        Diamonds
    }

    public enum Rank
    {
        Ace = 1, Two, Three, Four, Five, Six, Seven,
        Eight, Nine, Ten, Jack, Queen, King
    }

    public Suit suit;
    public Rank rank;
    public bool isFaceUp;

    public Card(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
        isFaceUp = false;
    }
}