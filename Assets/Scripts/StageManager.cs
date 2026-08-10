using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public int currentStage { get; private set; }
    public int currentLevel { get; private set; }
    public bool isGoalAchieved { get; set; }

    private Dictionary<int, int> previewCounts = new Dictionary<int, int>
    {
        {1, 3}, {2, 5}, {3, 8}, {4, 12}, {5, 12}
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartStage(int stage, int level)
    {
        currentStage = stage;
        currentLevel = level;
        isGoalAchieved = false;

        GameManager.Instance.StartStage();
    }

    public void RunPreview(GameManager gm)
    {
        int count = previewCounts[currentStage];

        foreach (var card in gm.ownedCards)
        {
            if (card is SC_PreviewPlus p)
                count += p.ExtraPreview;
            else if (card is SC_OmniVision o)
                count += o.ExtraPeek;
        }

        foreach (var card in gm.ownedCards)
        {
            if (card is SC_AllSeeingEye)
            {
                List<Card> simulated = DeckManager.Instance.PeekTop(count);
                simulated.AddRange(DeckManager.Instance.PeekTop(44 - count));
                if (gm.currentGoal.Check(simulated).isAchieved)
                {
                    gm.ForceZeroErrorWin();
                    gm.OnPreviewComplete(true);
                    return;
                }
            }
        }

        DeckManager.Instance.Draw(count);

        if (gm.currentGoal.Check(DeckManager.Instance.drawnCards).isAchieved)
        {
            gm.ForceZeroErrorWin();
            gm.OnPreviewComplete(true);
        }
        else
        {
            gm.OnPreviewComplete(false);
        }
    }

    public void CheckGoal()
    {
        var result = GameManager.Instance.currentGoal.Check(DeckManager.Instance.drawnCards);
        if (result.isAchieved)
            isGoalAchieved = true;
    }

    public void NextLevel()
    {
        if (currentLevel < 3 && currentStage < 5)
        {
            currentLevel++;
            StartStage(currentStage, currentLevel);
        }
        else if (currentStage < 5)
        {
            currentStage++;
            currentLevel = 1;
            StartStage(currentStage, currentLevel);
        }
    }
}