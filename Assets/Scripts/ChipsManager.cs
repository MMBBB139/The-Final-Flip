using UnityEngine;
using UnityEngine.Events;

public class ChipsManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    private int currentChips;

    public UnityEvent OnBankrupt;
    public UnityEvent<int> OnChipsChanged;

    void Awake()
    {
        OnBankrupt ??= new UnityEvent();
        OnChipsChanged ??= new UnityEvent<int>();
    }

    void Start()
    {
        ResetChips();
    }

    public int GetChips() => currentChips;

    public void AddChips(int amount)
    {
        if (amount == 0) return;
        currentChips += amount;
        if (currentChips < 0) currentChips = 0;

        Debug.Log($"{(amount > 0 ? "+" : "")}{amount} 筹码，当前: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);

        if (currentChips <= 0)
            OnBankrupt?.Invoke();
    }

    public void SetChips(int amount)
    {
        currentChips = Mathf.Max(0, amount);
        Debug.Log($"筹码直接设为: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);
        if (currentChips <= 0) OnBankrupt?.Invoke();
    }

    public void ResetChips()
    {
        currentChips = config != null ? config.initialChips : 30;
        Debug.Log($"筹码重置为: {currentChips}");
        OnChipsChanged?.Invoke(currentChips);
    }

    public void AwardSurviveBonus()
    {
        if (currentChips > 0)
        {
            int bonus = config != null ? config.surviveReward : 10;
            AddChips(bonus);
            Debug.Log($"存活奖励 +{bonus}");
        }
    }
}