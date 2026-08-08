using UnityEngine;

public class CorrectionManager : MonoBehaviour
{
    public static CorrectionManager Instance { get; private set; }

    public int correctionsRemaining { get; private set; }
    public bool isWindowOpen { get; private set; }
    public int baseGuess { get; private set; }

    private int defaultRange = 3;
    private int defaultCount = 1;
    private int cost = 10;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartRound(int guess)
    {
        baseGuess = guess;
        correctionsRemaining = GetCount();
        isWindowOpen = true;
    }

    public void OnFlip(int flipCount, int guessFlips)
    {
        if (!isWindowOpen) return;

        if (HasInfiniteWindow()) return;

        int threshold = guessFlips - 3 - GetWindowExtension();
        if (flipCount >= threshold)
            isWindowOpen = false;
    }

    public bool UseCorrection(int newGuess, out string errorMessage)
    {
        errorMessage = "";

        if (!isWindowOpen)
        {
            errorMessage = "修正窗口已关闭";
            return false;
        }

        if (correctionsRemaining <= 0)
        {
            errorMessage = "修正次数已用完";
            return false;
        }

        int range = GetRange();
        if (Mathf.Abs(newGuess - baseGuess) > range)
        {
            errorMessage = $"修正范围超出±{range}";
            return false;
        }

        if (!IsFree() && GameManager.Instance.chips < cost)
        {
            errorMessage = "筹码不足";
            return false;
        }

        if (!IsFree())
            GameManager.Instance.chips -= cost;

        correctionsRemaining--;
        GameManager.Instance.guessFlips = newGuess;
        GameManager.Instance.usedCorrectionThisRound = true;
        return true;
    }

    public void CloseWindow()
    {
        isWindowOpen = false;
    }

    public void ResetForRound()
    {
        correctionsRemaining = 0;
        isWindowOpen = false;
    }

    private int GetCount()
    {
        int count = defaultCount;
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_OneMoreCorrection c)
                count += c.ExtraCount;
            else if (card is SC_PerfectCorrection p)
                count += p.ExtraCount;
        }
        return count;
    }

    private int GetRange()
    {
        int range = defaultRange;
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_FlexRange f)
                range += f.RangeBonus;
            else if (card is SC_FreeCorrection f2)
                range += f2.RangeBonus;
            else if (card is SC_PerfectCorrection p)
                range += p.RangeBonus;
        }
        return range;
    }

    private int GetWindowExtension()
    {
        int extension = 0;
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_DelayWindow d)
                extension += d.WindowExtension;
        }
        return extension;
    }

    private bool HasInfiniteWindow()
    {
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_FreeCorrection f && f.InfiniteWindow) return true;
            if (card is SC_PerfectCorrection p && p.InfiniteWindow) return true;
        }
        return false;
    }

    private bool IsFree()
    {
        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_FreeCorrection f && f.FreeCorrection) return true;
            if (card is SC_PerfectCorrection p && p.FreeCorrection) return true;
        }
        return false;
    }
}