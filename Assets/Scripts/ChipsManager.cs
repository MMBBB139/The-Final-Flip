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
    /// 修改筹码（正数为增加，负数为减少，最少锁到0）
    /// </summary>
    public void AddChips(int amount)
    {
        if (amount == 0) return;

        currentChips += amount;
        if (currentChips < 0) currentChips = 0;

        Debug.Log($"{(amount > 0 ? "+" : "")}{amount} 筹码，当前: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);

        if (currentChips <= 0)
        {
            OnBankrupt?.Invoke();
        }
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