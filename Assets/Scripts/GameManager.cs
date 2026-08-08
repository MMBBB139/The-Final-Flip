using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int currentStage = 1;          // 当前层 1-5
    public int currentLevel = 1;          // 当前关 1-3
    public int chips = 30;                // 筹码
    public int guessFlips;               // 玩家猜测的翻牌数
    public int actualFlips;               // 实际翻牌数
    public int error;                     // 误差
    public int correctionsRemaining;      // 剩余修正次数
    public bool isCorrectionWindowOpen;     // 修正窗口是否开启

    public List<StrategyCard> ownedCards = new List<StrategyCard>();  // 拥有的牌
    public GoalChecker currentGoal;       // 本关目标

    public enum GamePhase
    {
        Preview,        // 预览阶段
        Guess,          // 猜翻牌数
        FlipCards,      // 翻牌阶段
        Result          // 结算
    }
    public GamePhase currentPhase;

    // 状态
    void Awake() => Instance = this;

    // 核心流程
    public void StartStage() { /* 初始化本关 */ }
    public void StartPreview() { /* 预览N张 */ }
    public void SubmitGuess(int number) { /* 玩家下注 */ }
    public void Flip() { /* 翻4张（或受控速影响） */ }
    public void UseStrategyCard(StrategyCard card) {  /* 用策略卡 */ }
    public void UseCorrection(int newGuess) { /* 使用修正 */ }
    public void EndStage() { /* 结算 */ }
    public void OpenShop() { /* 商店 */ }
}