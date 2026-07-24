// ChipsManager.cs
using UnityEngine;
using UnityEngine.Events;

public class ChipsManager : MonoBehaviour
{
    [SerializeField] private int initialChips = 200;
    private int currentChips;

    // 事件：筹码归零时触发
    public UnityEvent OnBankrupt;

    // 事件：筹码变动时触发（供UI更新）
    public UnityEvent<int> OnChipsChanged;

    void Awake()
    {
        if (OnBankrupt == null) OnBankrupt = new UnityEvent();
        if (OnChipsChanged == null) OnChipsChanged = new UnityEvent<int>();
    }

    void Start()
    {
        currentChips = initialChips;
        OnChipsChanged?.Invoke(currentChips);
    }

    /// <summary>
    /// 获取当前筹码数
    /// </summary>
    public int GetChips()
    {
        return currentChips;
    }

    /// <summary>
    /// 增加筹码
    /// </summary>
    public void AddChips(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("AddChips: 增加数量必须为正数");
            return;
        }

        currentChips += amount;
        Debug.Log($"+{amount} 筹码，当前: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);
    }

    /// <summary>
    /// 减少筹码，返回是否成功（筹码不足时返回false）
    /// </summary>
    public bool SpendChips(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("SpendChips: 消耗数量必须为正数");
            return false;
        }

        if (currentChips < amount)
        {
            Debug.Log($"筹码不足！需要{amount}，当前只有{currentChips}");
            return false;
        }

        currentChips -= amount;
        Debug.Log($"-{amount} 筹码，当前: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);

        // 检查是否归零，触发失败结算
        if (currentChips <= 0)
        {
            currentChips = 0; // 防止负数
            Debug.Log("筹码归零！触发失败结算。");
            OnBankrupt?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 直接设置筹码数（用于特殊效果，如全押）
    /// </summary>
    public void SetChips(int amount)
    {
        if (amount < 0) amount = 0;

        currentChips = amount;
        Debug.Log($"筹码直接设为: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);

        if (currentChips <= 0)
        {
            currentChips = 0;
            OnBankrupt?.Invoke();
        }
    }

    /// <summary>
    /// 重置筹码为初始值（用于新游戏）
    /// </summary>
    public void ResetChips()
    {
        currentChips = initialChips;
        Debug.Log($"筹码重置为: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);
    }
}