using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSpriteManager : MonoBehaviour
{
    public static CardSpriteManager Instance;

    [Header("牌背和褪色")]
    [SerializeField] private Sprite cardBack;
    [SerializeField] private Sprite fadedSprite;

    [Header("扑克牌面 - 按花色分组")]
    [SerializeField] private SuitSprites spades;
    [SerializeField] private SuitSprites hearts;
    [SerializeField] private SuitSprites clubs;
    [SerializeField] private SuitSprites diamonds;

    private Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

    void Awake()
    {
        Instance = this;
        BuildDictionary();
    }

    void BuildDictionary()
    {
        spriteDict.Clear();
        AddSuitSprites(Card.Suit.Spades, spades);
        AddSuitSprites(Card.Suit.Hearts, hearts);
        AddSuitSprites(Card.Suit.Clubs, clubs);
        AddSuitSprites(Card.Suit.Diamonds, diamonds);
    }

    void AddSuitSprites(Card.Suit suit, SuitSprites suitSprites)
    {
        if (suitSprites == null) return;
        if (suitSprites.ace != null) spriteDict[$"{suit}_Ace"] = suitSprites.ace;
        if (suitSprites.two != null) spriteDict[$"{suit}_Two"] = suitSprites.two;
        if (suitSprites.three != null) spriteDict[$"{suit}_Three"] = suitSprites.three;
        if (suitSprites.four != null) spriteDict[$"{suit}_Four"] = suitSprites.four;
        if (suitSprites.five != null) spriteDict[$"{suit}_Five"] = suitSprites.five;
        if (suitSprites.six != null) spriteDict[$"{suit}_Six"] = suitSprites.six;
        if (suitSprites.seven != null) spriteDict[$"{suit}_Seven"] = suitSprites.seven;
        if (suitSprites.eight != null) spriteDict[$"{suit}_Eight"] = suitSprites.eight;
        if (suitSprites.nine != null) spriteDict[$"{suit}_Nine"] = suitSprites.nine;
        if (suitSprites.ten != null) spriteDict[$"{suit}_Ten"] = suitSprites.ten;
        if (suitSprites.jack != null) spriteDict[$"{suit}_Jack"] = suitSprites.jack;
        if (suitSprites.queen != null) spriteDict[$"{suit}_Queen"] = suitSprites.queen;
        if (suitSprites.king != null) spriteDict[$"{suit}_King"] = suitSprites.king;
    }

    public Sprite GetCardSprite(Card card)
    {
        if (card.isFaded && fadedSprite != null)
            return fadedSprite;

        string key = $"{card.suit}_{card.rank}";
        if (spriteDict.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        Debug.LogWarning($"找不到牌面: {key}");
        return cardBack;
    }

    public Sprite GetCardBack() => cardBack;
}

[Serializable]
public class SuitSprites
{
    public Sprite ace;
    public Sprite two;
    public Sprite three;
    public Sprite four;
    public Sprite five;
    public Sprite six;
    public Sprite seven;
    public Sprite eight;
    public Sprite nine;
    public Sprite ten;
    public Sprite jack;
    public Sprite queen;
    public Sprite king;
}