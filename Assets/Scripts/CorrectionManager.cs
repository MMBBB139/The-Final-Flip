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

    // 从拥有的牌里提取修正相关的数据，只遍历一次
    private struct CorrectionModifiers
    {
        public int extraCount;
        public int rangeBonus;
        public int windowExtension;
        public bool infiniteWindow;
        public bool freeCorrection;
    }

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
        correctionsRemaining = defaultCount + GetModifiers().extraCount;
        isWindowOpen = true;
    }

    public void OnFlip(int flipCount, int guessFlips)
    {
        if (!isWindowOpen) return;

        var mods = GetModifiers();
        if (mods.infiniteWindow) return;

        int threshold = guessFlips - 3 - mods.windowExtension;
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

        var mods = GetModifiers();
        int range = defaultRange + mods.rangeBonus;

        if (Mathf.Abs(newGuess - baseGuess) > range)
        {
            errorMessage = $"修正范围超出±{range}";
            return false;
        }

        if (!mods.freeCorrection && GameManager.Instance.chips < cost)
        {
            errorMessage = "筹码不足";
            return false;
        }

        if (!mods.freeCorrection)
            GameManager.Instance.chips -= cost;

        correctionsRemaining--;
        GameManager.Instance.guessFlips = newGuess;
        GameManager.Instance.usedCorrectionThisRound = true;
        return true;
    }

    public void CloseWindow() => isWindowOpen = false;
    public void ResetForRound() { correctionsRemaining = 0; isWindowOpen = false; }

    // 所有策略牌相关逻辑集中在这里
    private CorrectionModifiers GetModifiers()
    {
        CorrectionModifiers mods = new CorrectionModifiers();

        foreach (var card in GameManager.Instance.ownedCards)
        {
            if (card is SC_OneMoreCorrection c)
                mods.extraCount += c.ExtraCount;
            else if (card is SC_PerfectCorrection p)
            {
                mods.extraCount += p.ExtraCount;
                mods.rangeBonus += p.RangeBonus;
                mods.infiniteWindow = mods.infiniteWindow || p.InfiniteWindow;
                mods.freeCorrection = mods.freeCorrection || p.FreeCorrection;
            }
            else if (card is SC_FlexRange f)
                mods.rangeBonus += f.RangeBonus;
            else if (card is SC_FreeCorrection f2)
            {
                mods.rangeBonus += f2.RangeBonus;
                mods.infiniteWindow = mods.infiniteWindow || f2.InfiniteWindow;
                mods.freeCorrection = mods.freeCorrection || f2.FreeCorrection;
            }
            else if (card is SC_DelayWindow d)
                mods.windowExtension += d.WindowExtension;
        }

        return mods;
    }
}