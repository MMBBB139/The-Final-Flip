// GameManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>管理贯穿整个游戏的全局状态：信用值、金币、章节进度、道具库存等</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("全局资源")]
    [SerializeField] private int startCredit = 100;
    [SerializeField] private int startGold = 0;
    [SerializeField] private int startChapter = 1;

    private int credit;
    private int gold;
    private int currentChapter;
    private int maxChapter = 8;     // 总章节数
    private bool isGameOver;

    // 道具库存（ItemData由后续ScriptableObject定义，此处暂时用string占位）
    public List<string> ownedItemNames = new List<string>();

    // 各关卡可能需要的累积数据（如负债清算人用的总负误差）
    public float totalNegativeError;

    // 简易存档标记
    public bool hasSavedGame;

    // 属性访问器
    public int Credit
    {
        get => credit;
        set
        {
            int old = credit;
            credit = Mathf.Max(0, value);
            if (credit != old)
                GameEvents.RaiseCreditChanged(credit);
            if (credit <= 0)
                TriggerGameOver(false);
        }
    }

    public int Gold
    {
        get => gold;
        set
        {
            gold = Mathf.Max(0, value);
            GameEvents.RaiseGoldChanged(gold);
        }
    }

    public int CurrentChapter
    {
        get => currentChapter;
        set => currentChapter = Mathf.Clamp(value, 1, maxChapter);
    }

    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化数据（若没有存档）
        if (!hasSavedGame)
            NewGame();
    }

    /// <summary>开始新游戏</summary>
    public void NewGame()
    {
        credit = startCredit;
        gold = startGold;
        currentChapter = startChapter;
        ownedItemNames.Clear();
        totalNegativeError = 0f;
        isGameOver = false;
        hasSavedGame = true;

        Debug.Log($"新游戏开始。信用:{credit} 金币:{gold} 当前章节:{currentChapter}");
        GameEvents.RaiseCreditChanged(credit);
        GameEvents.RaiseGoldChanged(gold);
        GameEvents.RaiseChapterChanged(currentChapter);
    }

    /// <summary>进入下一章节</summary>
    public void AdvanceChapter()
    {
        if (currentChapter < maxChapter)
        {
            currentChapter++;
            GameEvents.RaiseChapterChanged(currentChapter);
            Debug.Log($"进入第{currentChapter}章");
        }
        else
        {
            // 击败最终Boss，游戏胜利
            TriggerGameOver(true);
        }
    }

    /// <summary>增加/扣除信用值（负数为扣除），触发相应事件</summary>
    public void ModifyCredit(int delta)
    {
        Credit += delta;   // setter会处理事件
    }

    /// <summary>增加/扣除金币</summary>
    public void ModifyGold(int delta)
    {
        Gold += delta;
    }

    /// <summary>结束游戏（胜利或失败）</summary>
    /// <param name="isWin">true=通关胜利，false=信用耗尽或其他失败</param>
    public void TriggerGameOver(bool isWin)
    {
        if (isGameOver) return;
        isGameOver = true;
        GameEvents.RaiseGameOver(isWin);
        Debug.Log(isWin ? "游戏胜利！" : "游戏失败...");
    }

    /// <summary>重置游戏状态（读档或新游戏时调用）</summary>
    public void ResetState()
    {
        NewGame();
    }
}