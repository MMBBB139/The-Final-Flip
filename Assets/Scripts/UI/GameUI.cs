using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CardSpriteManager cardSpriteManager;

    [Header("顶部信息")]
    [SerializeField] private TMP_Text chipsText;
    [SerializeField] private TMP_Text layerText;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text ruleText;

    [Header("牌堆区域")]
    [SerializeField] private Transform deckPileParent;
    [SerializeField] private GameObject deckCardPrefab;
    [SerializeField] private Transform drawnCardsParent;
    [SerializeField] private GameObject drawnCardPrefab;
    [SerializeField] private TMP_Text drawnCountText;

    [Header("猜测区域")]
    [SerializeField] private TMP_InputField guessInput;
    [SerializeField] private Button submitGuessButton;
    [SerializeField] private Button correctButton;
    [SerializeField] private TMP_Text correctionInfoText;
    [SerializeField] private Button drawCardButton;

    [Header("策略牌区域")]
    [SerializeField] private Transform strategyCardParent;
    [SerializeField] private GameObject strategyCardSlotPrefab;

    [Header("商店面板")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform shopSlotsParent;
    [SerializeField] private GameObject shopSlotPrefab;
    [SerializeField] private Button refreshShopButton;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private TMP_Text refreshCostText;
    [SerializeField] private Transform ownedCardsParent;
    [SerializeField] private GameObject ownedCardSlotPrefab;

    [Header("结算面板")]
    [SerializeField] private GameObject settlementPanel;
    [SerializeField] private TMP_Text settlementResultText;
    [SerializeField] private Button settlementContinueButton;

    [Header("游戏结束面板")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [Header("通用选择弹窗")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text selectionPromptText;
    [SerializeField] private Transform selectionOptionsParent;
    [SerializeField] private GameObject selectionOptionPrefab;
    [SerializeField] private Button selectionCancelButton;
    [SerializeField] private Button selectionHideButton;

    private List<GameObject> strategyCardSlots = new List<GameObject>();
    private List<GameObject> shopSlotObjects = new List<GameObject>();
    private List<GameObject> ownedCardSlotObjects = new List<GameObject>();
    private List<GameObject> selectionOptionObjects = new List<GameObject>();
    private bool shopOpen;
    private GameState currentState;
    private bool isCorrecting;
    private bool selectionHidden;
    private Dictionary<string, string> customEffectTexts = new Dictionary<string, string>();

    private System.Action<int> onSelectionConfirmed;
    private System.Action onSelectionCancelCallback;

    void Start()
    {
        submitGuessButton.onClick.AddListener(OnSubmitGuess);
        correctButton.onClick.AddListener(OnCorrect);
        drawCardButton.onClick.AddListener(OnDrawCard);
        refreshShopButton.onClick.AddListener(OnRefreshShop);
        closeShopButton.onClick.AddListener(CloseShop);
        settlementContinueButton.onClick.AddListener(OnSettlementContinue);
        selectionCancelButton.onClick.AddListener(OnSelectionCancel);
        selectionHideButton.onClick.AddListener(OnSelectionHide);

        gameManager.OnCardDrawn.AddListener(OnCardDrawn);
        gameManager.OnTargetAchieved.AddListener(OnTargetAchievedEvent);
        gameManager.OnStageEnd.AddListener(OnStageEndEvent);
        gameManager.OnWaitingForInput.AddListener(OnWaitingForInput);

        var sm = gameManager.StrategyCardManager;
        if (sm != null)
        {
            sm.OnEffectTextUpdate += OnEffectTextUpdate;
            sm.OnRequestSelection += OnRequestSelection;
        }

        shopPanel.SetActive(false);
        settlementPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        selectionPanel.SetActive(false);
        refreshCostText.text = "刷新 -10";

        for (int i = 0; i < 4; i++)
        {
            var slot = Instantiate(strategyCardSlotPrefab, strategyCardParent);
            slot.name = $"StrategySlot_{i}";
            strategyCardSlots.Add(slot);
        }

        RefreshAll();
    }

    void Update()
    {
        currentState = gameManager.GetCurrentState();

        chipsText.text = $"{currentState.chips}";
        drawnCountText.text = $"已翻: {currentState.drawnCount}/52";

        bool canCorrectNow = currentState.canCorrect && currentState.isDrawingPhase && !currentState.isGameOver && !currentState.targetAchieved && !isCorrecting;

        correctButton.interactable = canCorrectNow;
        if (canCorrectNow)
            correctionInfoText.text = $"修正({currentState.remainingCorrections}次) -10筹码";
        else if (isCorrecting)
            correctionInfoText.text = "输入新猜测数字后点确定";
        else
            correctionInfoText.text = "修正不可用";

        drawCardButton.interactable = currentState.isDrawingPhase && !currentState.isGameOver && !currentState.targetAchieved && !isCorrecting;

        if (isCorrecting)
        {
            guessInput.interactable = true;
            submitGuessButton.interactable = true;
        }
        else if (currentState.isWaitingForGuess && !currentState.isGameOver)
        {
            guessInput.interactable = true;
            submitGuessButton.interactable = true;
        }
        else
        {
            guessInput.interactable = false;
            submitGuessButton.interactable = false;
        }

        UpdateStrategyCardSlots();
    }

    void RefreshAll()
    {
        customEffectTexts.Clear();
        currentState = gameManager.GetCurrentState();
        var (layer, stage) = gameManager.GetCurrentLayerStage();
        int tolerance = gameManager.GetCurrentErrorTolerance();
        string toleranceText = layer == 5 ? "误差必须=0" : $"误差≤{tolerance}";
        layerText.text = $"第{layer}层 第{stage}关\n{toleranceText}";

        string ruleDesc = layer switch
        {
            1 => "无特殊规则",
            2 => "修正窗口提前关闭（猜数-3张）",
            3 => "必须在前25张内达成目标",
            4 => "使用主动策略牌后下一张牌变为褪色牌（隐藏花色点数但结算有效），可用筹码恢复显示，价格递增：10/20/30...",
            5 => "无特殊规则",
            _ => ""
        };
        ruleText.text = ruleDesc;

        if (currentState.currentTarget != null)
            targetText.text = $"目标: {currentState.currentTarget.handName}\n{currentState.currentTarget.description}";
        else
            targetText.text = "无目标";

        UpdateDeckPile();
        UpdateDrawnCards();
    }

    void UpdateDeckPile()
    {
        foreach (Transform child in deckPileParent)
            Destroy(child.gameObject);

        int remaining = currentState.remainingCount;
        if (remaining <= 0) return;

        int displayCount = Mathf.Min(5, remaining);
        for (int i = 0; i < displayCount; i++)
        {
            var cardObj = Instantiate(deckCardPrefab, deckPileParent);
            cardObj.GetComponent<Image>().sprite = cardSpriteManager.GetCardBack();
            cardObj.transform.localPosition = new Vector3(i * 3, -i * 2, 0);
        }
    }

    void UpdateDrawnCards()
    {
        foreach (Transform child in drawnCardsParent)
            Destroy(child.gameObject);

        var drawnCards = currentState.drawnCards;
        for (int i = 0; i < drawnCards.Count; i++)
        {
            var cardObj = Instantiate(drawnCardPrefab, drawnCardsParent);
            cardObj.GetComponent<Image>().sprite = cardSpriteManager.GetCardSprite(drawnCards[i]);
            cardObj.transform.localPosition = new Vector3(i * 48, 0, 0);
        }
    }

    void UpdateStrategyCardSlots()
    {
        var owned = currentState.ownedCards;
        for (int i = 0; i < strategyCardSlots.Count; i++)
        {
            var slot = strategyCardSlots[i];
            if (i < owned.Count)
            {
                slot.SetActive(true);
                var card = owned[i];
                var nameText = slot.transform.Find("NameText").GetComponent<TMP_Text>();
                var effectText = slot.transform.Find("EffectText").GetComponent<TMP_Text>();
                var useBtn = slot.transform.Find("UseButton").GetComponent<Button>();

                nameText.text = card.cardName;

                if (customEffectTexts.TryGetValue(card.cardName, out string customText))
                    effectText.text = customText;
                else if (card.currentLevel >= 2 && card.description.Contains("升级："))
                {
                    int idx = card.description.IndexOf("升级：");
                    effectText.text = "已升级：" + card.description.Substring(idx + 3);
                }
                else
                    effectText.text = card.description;

                useBtn.gameObject.SetActive(card.type != StrategyCardData.CardType.被动);
                useBtn.interactable = card.IsAvailableThisRound() && !currentState.isGameOver;
                useBtn.onClick.RemoveAllListeners();
                string cardName = card.cardName;
                useBtn.onClick.AddListener(() => OnUseStrategyCard(cardName));
            }
            else
            {
                slot.SetActive(false);
            }
        }
    }

    void OnEffectTextUpdate(string cardName, string text)
    {
        customEffectTexts[cardName] = text;
    }

    void OnRequestSelection(string prompt, List<string> options, System.Action<int> callback, System.Action onCancel = null)
    {
        selectionPanel.SetActive(true);
        selectionPromptText.text = prompt;
        onSelectionConfirmed = callback;
        onSelectionCancelCallback = onCancel;
        selectionHidden = false;
        selectionHideButton.gameObject.SetActive(true);
        selectionOptionsParent.gameObject.SetActive(true);
        selectionPromptText.gameObject.SetActive(true);
        selectionCancelButton.gameObject.SetActive(true);
        selectionPanel.GetComponent<Image>().enabled = true;

        foreach (var obj in selectionOptionObjects)
            Destroy(obj);
        selectionOptionObjects.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var optObj = Instantiate(selectionOptionPrefab, selectionOptionsParent);
            selectionOptionObjects.Add(optObj);

            var label = optObj.transform.Find("LabelText").GetComponent<TMP_Text>();
            var btn = optObj.GetComponent<Button>();

            label.text = options[i];
            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSelectionOptionClicked(index));
        }
    }

    void OnSelectionHide()
    {
        selectionHidden = !selectionHidden;
        selectionPromptText.gameObject.SetActive(!selectionHidden);
        selectionOptionsParent.gameObject.SetActive(!selectionHidden);
        selectionCancelButton.gameObject.SetActive(!selectionHidden);
        selectionPanel.GetComponent<Image>().enabled = !selectionHidden;
    }

    void OnSelectionOptionClicked(int index)
    {
        selectionHidden = false;
        selectionHideButton.gameObject.SetActive(false);
        selectionPanel.SetActive(false);
        onSelectionConfirmed?.Invoke(index);
        onSelectionConfirmed = null;
        onSelectionCancelCallback = null;
    }

    void OnSelectionCancel()
    {
        selectionHidden = false;
        selectionHideButton.gameObject.SetActive(false);
        selectionPanel.SetActive(false);
        onSelectionConfirmed = null;
        if (onSelectionCancelCallback != null)
        {
            var cb = onSelectionCancelCallback;
            onSelectionCancelCallback = null;
            cb.Invoke();
        }
    }

    void OnSubmitGuess()
    {
        if (!int.TryParse(guessInput.text, out int n)) return;

        if (isCorrecting)
        {
            gameManager.UseCorrection(n);
            EndCorrecting();
        }
        else
        {
            gameManager.SubmitGuess(n);
        }
    }

    void OnCorrect()
    {
        isCorrecting = true;
        guessInput.text = "";
        guessInput.Select();
        guessInput.ActivateInputField();
    }

    void EndCorrecting()
    {
        isCorrecting = false;
    }

    void OnDrawCard()
    {
        gameManager.DrawCard();
    }

    void OnUseStrategyCard(string cardName)
    {
        gameManager.UseStrategyCard(cardName);
    }

    void OnCardDrawn(Card card)
    {
        currentState = gameManager.GetCurrentState();
        UpdateDeckPile();
        UpdateDrawnCards();
    }

    void OnTargetAchievedEvent() { }

    void OnStageEndEvent(string msg)
    {
        EndCorrecting();
        selectionPanel.SetActive(false);
        StartCoroutine(ShowSettlement(msg));
    }

    void OnWaitingForInput()
    {
        EndCorrecting();
        selectionPanel.SetActive(false);
        RefreshAll();
        guessInput.text = "";
    }

    IEnumerator ShowSettlement(string msg)
    {
        yield return new WaitForSeconds(0.3f);
        settlementPanel.SetActive(true);
        settlementResultText.text = msg;
    }

    void OnSettlementContinue()
    {
        settlementPanel.SetActive(false);

        if (currentState.isGameOver || gameManager.GetCurrentState().isGameOver)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = $"游戏结束\n最终筹码: {currentState.chips}";
        }
        else
        {
            OpenShop();
        }
    }

    void OpenShop()
    {
        customEffectTexts.Clear();
        shopOpen = true;
        shopPanel.SetActive(true);
        UpdateShopDisplay();
    }

    void UpdateShopDisplay()
    {
        foreach (var obj in shopSlotObjects)
            Destroy(obj);
        shopSlotObjects.Clear();

        var shopItems = gameManager.GetCurrentState().shopItems;
        for (int i = 0; i < shopItems.Count; i++)
        {
            var item = shopItems[i];
            var slotObj = Instantiate(shopSlotPrefab, shopSlotsParent);
            shopSlotObjects.Add(slotObj);

            var nameText = slotObj.transform.Find("NameText").GetComponent<TMP_Text>();
            var effectText = slotObj.transform.Find("EffectText").GetComponent<TMP_Text>();
            var buyBtn = slotObj.transform.Find("BuyButton").GetComponent<Button>();

            nameText.text = item.cardName;
            effectText.text = item.description;
            buyBtn.GetComponentInChildren<TMP_Text>().text = $"购买 {item.price}";

            int index = i;
            buyBtn.onClick.RemoveAllListeners();
            buyBtn.onClick.AddListener(() => OnBuyShopItem(index));
        }

        foreach (var obj in ownedCardSlotObjects)
            Destroy(obj);
        ownedCardSlotObjects.Clear();

        var owned = gameManager.GetCurrentState().ownedCards;
        for (int i = 0; i < owned.Count; i++)
        {
            var card = owned[i];
            var slotObj = Instantiate(ownedCardSlotPrefab, ownedCardsParent);
            ownedCardSlotObjects.Add(slotObj);

            var nameText = slotObj.transform.Find("NameText").GetComponent<TMP_Text>();
            var infoText = slotObj.transform.Find("InfoText").GetComponent<TMP_Text>();
            var sellBtn = slotObj.transform.Find("SellButton").GetComponent<Button>();
            var upgradeBtn = slotObj.transform.Find("UpgradeButton").GetComponent<Button>();

            nameText.text = card.cardName;

            if (customEffectTexts.TryGetValue(card.cardName, out string customText))
                infoText.text = customText;
            else if (card.currentLevel >= 2 && card.description.Contains("升级："))
            {
                int idx = card.description.IndexOf("升级：");
                infoText.text = "已升级：" + card.description.Substring(idx + 3);
            }
            else
                infoText.text = card.description;

            sellBtn.GetComponentInChildren<TMP_Text>().text = $"卖掉 {card.GetSellPrice()}";
            sellBtn.onClick.RemoveAllListeners();
            string cardName = card.cardName;
            sellBtn.onClick.AddListener(() => {
                gameManager.SellStrategyCard(cardName);
                UpdateShopDisplay();
            });

            if (card.IsUpgradable)
            {
                upgradeBtn.gameObject.SetActive(true);
                upgradeBtn.GetComponentInChildren<TMP_Text>().text = $"升级 {card.GetUpgradeCost()}";
                upgradeBtn.onClick.RemoveAllListeners();
                upgradeBtn.onClick.AddListener(() => {
                    if (gameManager.StrategyCardManager.UpgradeCard(cardName))
                        UpdateShopDisplay();
                });
            }
            else
            {
                upgradeBtn.gameObject.SetActive(false);
            }
        }
    }

    void OnBuyShopItem(int index)
    {
        if (gameManager.BuyStrategyCard(index))
            UpdateShopDisplay();
    }

    void OnRefreshShop()
    {
        gameManager.RefreshShop();
        UpdateShopDisplay();
    }

    public void CloseShop()
    {
        shopOpen = false;
        shopPanel.SetActive(false);
        gameManager.ProceedToNextStage();
        RefreshAll();
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            var sm = gameManager.StrategyCardManager;
            if (sm != null)
            {
                sm.OnEffectTextUpdate -= OnEffectTextUpdate;
                sm.OnRequestSelection -= OnRequestSelection;
            }
        }
    }
}