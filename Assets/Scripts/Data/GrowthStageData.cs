using UnityEngine;

// Growth logic — used by FarmPlot state machine
[System.Serializable]
public class GrowthStage
{
    public string stageName;
    public float  duration;
    public float  mutationBonus;  // future: extra mutation chance at this stage
}

// Growth visuals — used by FarmPlotVisual only
[System.Serializable]
public class GrowthStageVisual
{
    public GameObject visualPrefab;
    public Color      stageColor = Color.green;
    [Range(0.05f, 2f)]
    public float      scale      = 0.3f;
}