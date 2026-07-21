// BetResult.cs
[System.Serializable]
public class BetResult
{
    public int error;               // 误差：0=完美猜中，正=提前猜中，负=猜晚
    public float riskFactor;        // 风险系数 R = N / E
    public float rewardMultiplier;  // 奖励倍率
    public int creditChange;        // 信用值变化（正为增加，负为扣除）
    public int goldChange;          // 金币变化
}