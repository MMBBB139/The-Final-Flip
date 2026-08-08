using System.Collections.Generic;

// ==================== 基类 ====================
public abstract class StrategyCard
{
    public abstract string cardName { get; }
    public abstract string description { get; }
    public abstract int stage { get; }
    public abstract int buyPrice { get; }
    public abstract int upgradePrice { get; }
    public abstract bool canUpgrade { get; }
    public abstract bool isPassive { get; }  // true = 被动（开局自动生效），false = 主动（手动使用）
    public bool isUpgraded { get; set; }
    public bool usedThisRound { get; set; }

    public virtual void ResetForRound()
    {
        if (!isPassive)
            usedThisRound = false;
    }

    // 被动牌：开局自动调用；主动牌：玩家手动调用
    public abstract bool Execute();
}

// ==================== 第1层 ====================

public class SC_ExploreTop : StrategyCard
{
    public override string cardName => "探顶";
    public override string description => isUpgraded ? "看顶部5张，可选2张沉底" : "看顶部3张";
    public override int stage => 1;
    public override int buyPrice => 8;
    public override int upgradePrice => 10;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 显示牌堆顶部 (isUpgraded?5:3) 张
        // TODO(upgraded): 可选2张沉底
        usedThisRound = true;
        return true;
    }
}

public class SC_ExploreBottom : StrategyCard
{
    public override string cardName => "探底";
    public override string description => isUpgraded ? "看底部5张，可选2张置顶" : "看底部5张";
    public override int stage => 1;
    public override int buyPrice => 5;
    public override int upgradePrice => 12;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 显示牌堆底部5张
        // TODO(upgraded): 可选2张置顶
        usedThisRound = true;
        return true;
    }
}

public class SC_Delete : StrategyCard
{
    public override string cardName => "删除";
    public override string description => isUpgraded ? "删除最多2张已翻牌" : "删除1张已翻牌";
    public override int stage => 1;
    public override int buyPrice => 10;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public int MaxDeleteCount => isUpgraded ? 2 : 1;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 玩家选择1~MaxDeleteCount张已翻牌删除
        usedThisRound = true;
        return true;
    }
}

public class SC_Duplicate : StrategyCard
{
    public override string cardName => "复制";
    public override string description => isUpgraded ? "复制1张已翻牌并置顶" : "复制1张已翻牌，洗入牌堆";
    public override int stage => 1;
    public override int buyPrice => 12;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public bool PlaceOnTop => isUpgraded;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 选1张已翻牌生成复制
        // TODO: PlaceOnTop ? 置顶 : 随机插入
        usedThisRound = true;
        return true;
    }
}

public class SC_DelayWindow : StrategyCard
{
    public override string cardName => "修正延后";
    public override string description => isUpgraded ? "修正窗口延长6张" : "修正窗口延长3张";
    public override int stage => 1;
    public override int buyPrice => 6;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int WindowExtension => isUpgraded ? 6 : 3;

    public override bool Execute() => true; // 被动牌由属性生效，Execute只是标记
}

public class SC_FlexRange : StrategyCard
{
    public override string cardName => "弹性修正";
    public override string description => isUpgraded ? "修正范围+2（±5）" : "修正范围+1（±4）";
    public override int stage => 1;
    public override int buyPrice => 8;
    public override int upgradePrice => 6;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int RangeBonus => isUpgraded ? 2 : 1;

    public override bool Execute() => true;
}

public class SC_ControlSpeed : StrategyCard
{
    public override string cardName => "控速";
    public override string description => isUpgraded ? "每次翻2张" : "每次翻3张";
    public override int stage => 1;
    public override int buyPrice => 10;
    public override int upgradePrice => 6;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int DrawCount => isUpgraded ? 2 : 3;

    public override bool Execute() => true;
}

public class SC_EarlyBird : StrategyCard
{
    public override string cardName => "早鸟优惠";
    public override string description => isUpgraded ? "翻牌≤15张达成，额外+20" : "翻牌≤10张达成，额外+12";
    public override int stage => 1;
    public override int buyPrice => 10;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int Threshold => isUpgraded ? 15 : 10;
    public int Bonus => isUpgraded ? 20 : 12;

    public override bool Execute() => true;
}

public class SC_CloseBonus : StrategyCard
{
    public override string cardName => "近误差红利";
    public override string description => isUpgraded ? "误差≤1时额外+12" : "误差=1时额外+8";
    public override int stage => 1;
    public override int buyPrice => 8;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int Bonus => isUpgraded ? 12 : 8;
    public bool RequireExactOne => !isUpgraded;

    public override bool Execute() => true;
}

public class SC_DeviationMaster : StrategyCard
{
    public override string cardName => "偏差大师";
    public override string description => isUpgraded ? "误差≥3存活，+15，永久累计+8/次" : "误差≥3存活，+15，永久累计+5/次";
    public override int stage => 1;
    public override int buyPrice => 15;
    public override int upgradePrice => 10;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int accumulatedBonus;
    public int BaseBonus => 15;
    public int StackPerTrigger => isUpgraded ? 8 : 5;

    public override bool Execute() => true;
}

public class SC_Steady : StrategyCard
{
    public override string cardName => "稳扎稳打";
    public override string description => isUpgraded ? "误差≤2，+8，连续+6/局" : "误差≤2，+8，连续+4/局";
    public override int stage => 1;
    public override int buyPrice => 12;
    public override int upgradePrice => 10;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int streak;
    public int BaseBonus => 8;
    public int StackPerStreak => isUpgraded ? 6 : 4;

    public override bool Execute() => true;
}

public class SC_ToleranceUp : StrategyCard
{
    public override string cardName => "宽容";
    public override string description => isUpgraded ? "本层误差容忍度+2" : "本层误差容忍度+1";
    public override int stage => 1;
    public override int buyPrice => 10;
    public override int upgradePrice => 8;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int ToleranceBonus => isUpgraded ? 2 : 1;

    public override bool Execute() => true;
}

// ==================== 第2层 ====================

public class SC_ChangeColor : StrategyCard
{
    public override string cardName => "变色";
    public override string description => isUpgraded ? "最多2张改花色" : "1张改花色";
    public override int stage => 2;
    public override int buyPrice => 18;
    public override int upgradePrice => 14;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public int MaxCount => isUpgraded ? 2 : 1;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 玩家选择1~MaxCount张已翻牌，每张指定新花色
        usedThisRound = true;
        return true;
    }
}

public class SC_ChangeRank : StrategyCard
{
    public override string cardName => "变点";
    public override string description => isUpgraded ? "改为±2范围内点数" : "改为相邻点数";
    public override int stage => 2;
    public override int buyPrice => 18;
    public override int upgradePrice => 14;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public int Range => isUpgraded ? 2 : 1;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 选1张已翻牌，选偏移量（-Range ~ +Range，不含0）
        usedThisRound = true;
        return true;
    }
}

public class SC_Scout : StrategyCard
{
    public override string cardName => "探牌";
    public override string description => isUpgraded ? "随机显示12张，可选2张沉底" : "随机显示8张";
    public override int stage => 2;
    public override int buyPrice => 24;
    public override int upgradePrice => 16;
    public override bool canUpgrade => true;
    public override bool isPassive => false;

    public int RevealCount => isUpgraded ? 12 : 8;
    public bool CanSendToBottom => isUpgraded;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 随机选RevealCount张未翻牌显示
        // TODO: CanSendToBottom时可选2张沉底
        usedThisRound = true;
        return true;
    }
}

public class SC_PreviewPlus : StrategyCard
{
    public override string cardName => "预览";
    public override string description => isUpgraded ? "开局预览+7张" : "开局预览+4张";
    public override int stage => 2;
    public override int buyPrice => 20;
    public override int upgradePrice => 14;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int ExtraPreview => isUpgraded ? 7 : 4;

    public override bool Execute() => true;
}

public class SC_OneMoreCorrection : StrategyCard
{
    public override string cardName => "再修一次";
    public override string description => isUpgraded ? "修正次数+2" : "修正次数+1";
    public override int stage => 2;
    public override int buyPrice => 18;
    public override int upgradePrice => 14;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int ExtraCount => isUpgraded ? 2 : 1;

    public override bool Execute() => true;
}

public class SC_ZeroBonus : StrategyCard
{
    public override string cardName => "零误差红利";
    public override string description => isUpgraded ? "误差≤1，+35" : "误差=0，+30";
    public override int stage => 2;
    public override int buyPrice => 28;
    public override int upgradePrice => 16;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int Bonus => isUpgraded ? 35 : 30;
    public bool RequireExactZero => !isUpgraded;

    public override bool Execute() => true;
}

public class SC_SpeedRun : StrategyCard
{
    public override string cardName => "速攻";
    public override string description => isUpgraded ? "20张内误差≤1，+25，累计+8/次" : "15张内误差≤1，+20，累计+5/次";
    public override int stage => 2;
    public override int buyPrice => 28;
    public override int upgradePrice => 16;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int accumulatedBonus;
    public int Threshold => isUpgraded ? 20 : 15;
    public int BaseBonus => isUpgraded ? 25 : 20;
    public int StackPerTrigger => isUpgraded ? 8 : 5;

    public override bool Execute() => true;
}

public class SC_CorrectionArtist : StrategyCard
{
    public override string cardName => "修正艺术家";
    public override string description => isUpgraded ? "修正后误差≤1，+25，累计+8/次" : "修正后误差=0，+20，累计+5/次";
    public override int stage => 2;
    public override int buyPrice => 30;
    public override int upgradePrice => 16;
    public override bool canUpgrade => true;
    public override bool isPassive => true;

    public int accumulatedBonus;
    public int BaseBonus => isUpgraded ? 25 : 20;
    public int StackPerTrigger => isUpgraded ? 8 : 5;
    public bool RequireExactZero => !isUpgraded;

    public override bool Execute() => true;
}

// ==================== 第3层 ====================

public class SC_OmniVision : StrategyCard
{
    public override string cardName => "全视";
    public override string description => "探顶探底各多探3张";
    public override int stage => 3;
    public override int buyPrice => 40;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public int ExtraPeek => 3;

    public override bool Execute() => true;
}

public class SC_Rebirth : StrategyCard
{
    public override string cardName => "新生";
    public override string description => "删1张已翻牌，复制2张指定已翻牌";
    public override int stage => 3;
    public override int buyPrice => 45;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => false;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 删1 + 复制2
        usedThisRound = true;
        return true;
    }
}

public class SC_FreeCorrection : StrategyCard
{
    public override string cardName => "自由修正";
    public override string description => "修正免费，窗口无限，范围+6";
    public override int stage => 3;
    public override int buyPrice => 42;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public bool FreeCorrection => true;
    public bool InfiniteWindow => true;
    public int RangeBonus => 6;

    public override bool Execute() => true;
}

public class SC_Reshape : StrategyCard
{
    public override string cardName => "重塑";
    public override string description => "1张已翻牌，花色点数全改";
    public override int stage => 3;
    public override int buyPrice => 48;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => false;

    public override bool Execute()
    {
        if (usedThisRound) return false;
        // TODO: 选1张，指定花色和点数
        usedThisRound = true;
        return true;
    }
}

public class SC_HighStakes : StrategyCard
{
    public override string cardName => "豪赌";
    public override string description => "容忍度+2，偏差大师筹码翻倍";
    public override int stage => 3;
    public override int buyPrice => 45;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public int ToleranceBonus => 2;
    public bool DoubleDeviationMaster => true;

    public override bool Execute() => true;
}

// ==================== 第4层 ====================

public class SC_Phoenix : StrategyCard
{
    public override string cardName => "不死鸟";
    public override string description => "失败改存活，无奖励，永久消失";
    public override int stage => 4;
    public override int buyPrice => 80;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => false;

    public bool isGoneForever;

    public override bool Execute()
    {
        if (usedThisRound || isGoneForever) return false;
        usedThisRound = true;
        isGoneForever = true;
        // TODO: 标记本局失败改为存活
        return true;
    }
}

public class SC_PerfectCorrection : StrategyCard
{
    public override string cardName => "完美修正";
    public override string description => "修正免费，窗口无限，次数+6，范围+6";
    public override int stage => 4;
    public override int buyPrice => 100;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public bool FreeCorrection => true;
    public bool InfiniteWindow => true;
    public int ExtraCount => 6;
    public int RangeBonus => 6;

    public override bool Execute() => true;
}

public class SC_Absolution : StrategyCard
{
    public override string cardName => "赦免";
    public override string description => "误差容忍度+6";
    public override int stage => 4;
    public override int buyPrice => 150;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public int ToleranceBonus => 6;

    public override bool Execute() => true;
}

public class SC_AllSeeingEye : StrategyCard
{
    public override string cardName => "全视之眼";
    public override string description => "下注前看顶44张，直接达成则0误差获胜";
    public override int stage => 4;
    public override int buyPrice => 200;
    public override int upgradePrice => 0;
    public override bool canUpgrade => false;
    public override bool isPassive => true;

    public override bool Execute()
    {
        // TODO: 下注前模拟翻顶部44张，检查是否能直接达成
        return true;
    }
}