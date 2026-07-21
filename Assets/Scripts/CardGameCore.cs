using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardGameCore : MonoBehaviour
{
    [Header("Info Display")]
    public TextMeshProUGUI TxtCredit;
    public TextMeshProUGUI TxtTarget;
    public TextMeshProUGUI TxtGuess;
    public TextMeshProUGUI TxtLog;

    [Header("Player Actions")]
    public TMP_InputField InputGuess;
    public Button BtnSubmitGuess;
    public Button BtnFlip;
    public Button BtnChangeGuess;

    [Header("Items")]
    public Button BtnItem_BottomMirror;
    public Button BtnItem_FlipPreview;

    private List<Card> deck = new List<Card>();
    private int currentFlipIndex = 0;
    private int credit = 100;
    private int playerGuess = -1;
    private bool hasUsedFix = false;
    private string currentTargetName;
    private System.Func<List<Card>, bool> currentTargetChecker;
    private HashSet<string> usedItems = new HashSet<string>();

    public enum Suit { Hearts, Spades, Diamonds, Clubs }
    public struct Card
    {
        public int point;
        public Suit suit;
        public override string ToString() => $"{suit} {point}";
    }

    void Start()
    {
        BtnSubmitGuess.onClick.AddListener(SubmitGuess);
        BtnFlip.onClick.AddListener(FlipNextCard);
        BtnChangeGuess.onClick.AddListener(UseFix);

        BtnItem_BottomMirror.onClick.AddListener(() => UseItem("BottomMirror"));
        BtnItem_FlipPreview.onClick.AddListener(() => UseItem("FlipPreview"));

        StartNewRound();
    }

    void StartNewRound()
    {
        deck = CreateShuffledDeck();
        currentFlipIndex = 0;
        playerGuess = -1;
        hasUsedFix = false;
        usedItems.Clear();
        InputGuess.text = "";

        SetRandomTarget();
        UpdateUI();
        Log("New round! Enter your guess N based on the target pattern.");
    }

    List<Card> CreateShuffledDeck()
    {
        var cards = new List<Card>();
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            for (int p = 1; p <= 13; p++)
                cards.Add(new Card { point = p, suit = s });

        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        return cards;
    }

    void SetRandomTarget()
    {
        int rand = Random.Range(0, 6);
        switch (rand)
        {
            case 0: currentTargetName = "Pair (2 same rank)"; currentTargetChecker = CheckPair; break;
            case 1: currentTargetName = "2 Flush (2 same suit)"; currentTargetChecker = Check2Flush; break;
            case 2: currentTargetName = "3 Flush (3 same suit)"; currentTargetChecker = Check3Flush; break;
            case 3: currentTargetName = "3 of a Kind (3 same rank)"; currentTargetChecker = CheckThreeKind; break;
            case 4: currentTargetName = "Two Pairs"; currentTargetChecker = CheckTwoPairs; break;
            case 5: currentTargetName = "Full House (3+2)"; currentTargetChecker = CheckFullHouse; break;
        }
    }

    bool CheckPair(List<Card> cards) => cards.GroupBy(c => c.point).Any(g => g.Count() >= 2);
    bool Check2Flush(List<Card> cards) => cards.GroupBy(c => c.suit).Any(g => g.Count() >= 2);
    bool Check3Flush(List<Card> cards) => cards.GroupBy(c => c.suit).Any(g => g.Count() >= 3);
    bool CheckThreeKind(List<Card> cards) => cards.GroupBy(c => c.point).Any(g => g.Count() >= 3);
    bool CheckTwoPairs(List<Card> cards) => cards.GroupBy(c => c.point).Count(g => g.Count() >= 2) >= 2;
    bool CheckFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.point).Where(g => g.Count() >= 2).ToList();
        return groups.Any(g => g.Count() >= 3) && groups.Count >= 2;
    }

    void SubmitGuess()
    {
        if (currentFlipIndex > 0)
        {
            Log("Can only guess before flipping. Use Fix to change.");
            return;
        }
        if (!int.TryParse(InputGuess.text, out int guess) || guess < 1)
        {
            Log("Enter a valid positive integer N.");
            return;
        }
        playerGuess = guess;
        Log($"Guess recorded: target on card #{playerGuess}.");
        UpdateUI();
    }

    void FlipNextCard()
    {
        if (playerGuess == -1)
        {
            Log("Enter guess N first!");
            return;
        }
        if (currentFlipIndex >= deck.Count)
        {
            Log("Deck empty.");
            return;
        }

        currentFlipIndex++;
        Card flipped = deck[currentFlipIndex - 1];
        Log($"Flipped #{currentFlipIndex}: {flipped}");

        var revealedCards = deck.Take(currentFlipIndex).ToList();
        if (currentTargetChecker(revealedCards))
        {
            Log($"Target [{currentTargetName}] reached at card #{currentFlipIndex}!");
            SettleGame();
            return;
        }

        UpdateUI();
    }

    void UseFix()
    {
        if (hasUsedFix)
        {
            Log("Fix already used this round.");
            return;
        }
        if (currentFlipIndex >= playerGuess)
        {
            Log("Fix lost: must use before flipping card N.");
            hasUsedFix = true;
            UpdateUI();
            return;
        }
        if (!int.TryParse(InputGuess.text, out int newGuess) || newGuess < 1)
        {
            Log("Enter new guess in input field first.");
            return;
        }
        if (credit < 10)
        {
            Log("Not enough credit (need 10).");
            return;
        }

        credit -= 2;
        playerGuess = newGuess;
        hasUsedFix = true;
        Log($"Fix used. New guess: #{playerGuess}. -2 credit.");
        UpdateUI();
    }

    void UseItem(string itemName)
    {
        if (usedItems.Contains(itemName))
        {
            Log($"Item [{itemName}] already used.");
            return;
        }
        usedItems.Add(itemName);

        switch (itemName)
        {
            case "BottomMirror":
                var bottom5 = deck.Skip(Mathf.Max(0, deck.Count - 5)).Select(c => c.ToString());
                Log($"Bottom 5: {string.Join(", ", bottom5)}");
                break;
            case "FlipPreview":
                bool possible = false;
                for (int i = currentFlipIndex; i < Mathf.Min(currentFlipIndex + 3, deck.Count); i++)
                {
                    if (currentTargetChecker(deck.Take(i + 1).ToList())) { possible = true; break; }
                }
                Log($"Flip Preview: possible in 3 cards? {(possible ? "YES" : "NO")}.");
                break;
        }
        UpdateUI();
    }

    void SettleGame()
    {
        if (playerGuess == -1) return;
        int error = Mathf.Abs(currentFlipIndex - playerGuess);
        int deltaCredit;
        if (error == 0) deltaCredit = 5;
        else if (currentFlipIndex < playerGuess) deltaCredit = -(error * 2);
        else deltaCredit = -(error * 4);

        credit += deltaCredit;
        Log($"Settled: error {error}, credit {deltaCredit:+0;-#}. Total: {credit}.");

        if (credit <= 0)
        {
            Log("GAME OVER - No credit left!");
            BtnFlip.interactable = false;
            BtnSubmitGuess.interactable = false;
        }
        else
        {
            StartNewRound();
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        TxtCredit.text = $"Credit: {credit}";
        TxtTarget.text = $"Target: {currentTargetName}";
        TxtGuess.text = playerGuess == -1 ? "No guess" : $"Guess: card #{playerGuess}";
        BtnChangeGuess.interactable = !hasUsedFix && currentFlipIndex < playerGuess;
    }

    void Log(string msg)
    {
        if (TxtLog != null)
            TxtLog.text = msg + "\n" + TxtLog.text;
        Debug.Log(msg);
    }
}