using UnityEngine;

public class ToolUser : MonoBehaviour
{
    public static ToolUser Instance;

    private Camera   _mainCamera;
    private Collider _lastHitCollider;

    void Awake()
    {
        Instance    = this;
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (ShopUI.IsOpen)        return;
        if (DialoguePanel.IsOpen) return;
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing) return;

        if (Input.GetMouseButtonUp(0)) { _lastHitCollider = null; return; }
        if (!Input.GetMouseButton(0))  return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        InventorySlot selected = Inventory.Instance.SelectedSlot;
        if (selected == null || selected.Type != InventoryItemType.Tool) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;
        if (hit.collider == _lastHitCollider) return;

        FarmPlot plot = hit.collider.GetComponent<FarmPlot>();
        if (plot == null) return;

        _lastHitCollider = hit.collider;
        bool success = UseTool(selected.Tool, plot);
        if (success && selected.Tool.isConsumable)
            Inventory.Instance.UseTool(selected.Tool);
    }

    bool UseTool(ToolData tool, FarmPlot plot)
    {
        switch (tool.toolType)
        {
            case ToolType.Shovel:      return UseShovel(plot);
            case ToolType.WateringCan: return UseWateringCan(plot);
            case ToolType.Fertilizer:  return UseFertilizer(plot, tool);
            default:                   return false;
        }
    }

    bool UseShovel(FarmPlot plot)
    {
        // Evolved characters cannot be removed — the player forms a bond with them
        if (plot.State == FarmPlot.PlotState.Evolved)
        {
            string name = plot.EvolvedCharacter?.characterName ?? "this character";
            TutorialConsole.Warn($"You cannot remove {name}.");
            return false;
        }
        plot.RemovePlot();
        AudioManager.Play(SoundEvent.PlotRemoved);
        TutorialConsole.Log("Removed farm plot.");
        return true;
    }

    bool UseWateringCan(FarmPlot plot)
    {
        if (plot.State != FarmPlot.PlotState.Growing)
        {
            TutorialConsole.Warn("Plant a seed first before watering.");
            return false;
        }
        if (plot.IsWatered)
        {
            TutorialConsole.Warn("Already watered.");
            return false;
        }
        plot.ApplyWatering();
        AudioManager.Play(SoundEvent.WateringCan);
        TutorialConsole.Log($"Watered {plot.ActiveCrop.cropName}! Growth speed doubled.");
        return true;
    }

    bool UseFertilizer(FarmPlot plot, ToolData tool)
    {
        if (plot.State != FarmPlot.PlotState.Growing)
        {
            TutorialConsole.Warn("Plant a seed first before fertilizing.");
            return false;
        }
        if (tool.fertilizerData == null)
        {
            Debug.LogWarning($"ToolUser: {tool.toolName} has no FertilizerData assigned " +
                "in the Inspector.");
            return false;
        }
        plot.ApplyFertilizer(tool.fertilizerData);
        AudioManager.Play(SoundEvent.WateringCan); // reuse until a dedicated Fertilizer sound is added
        return true;
    }
}
