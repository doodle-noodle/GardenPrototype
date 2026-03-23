using UnityEngine;

// Processes the active tool against whatever the player clicks.
// Attached to the same GameObject as the main input manager, or a dedicated ToolUser object.
public class ToolUser : MonoBehaviour
{
    void Update()
    {
        if (DialoguePanel.IsOpen)        return;
        if (EvolutionConfirmPanel.IsOpen) return;
        if (ShopUI.IsOpen)               return;
        if (InventoryPanel.IsOpen)       return;

        var slot = Inventory.Instance.SelectedSlot;
        if (slot == null || slot.IsEmpty) return;
        if (slot.Type != InventoryItemType.Tool) return;

        var tool = slot.Tool;
        if (tool == null) return;

        if (tool.toolType == ToolType.Shovel)
        {
            HandleShovel();
            return;
        }

        // Non-shovel tools activate on mouse-down over a plot
        if (!Input.GetMouseButtonDown(0)) return;

        var plot = RaycastPlot();
        if (plot == null) return;

        switch (tool.toolType)
        {
            case ToolType.WateringCan:  UseWateringCan(plot, tool);  break;
            case ToolType.Fertilizer:   UseFertilizer(plot, tool);   break;
        }
    }

    // ── Shovel ────────────────────────────────────────────────

    void HandleShovel()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var plot = RaycastPlot();
        if (plot == null) return;

        if (plot.State == FarmPlot.PlotState.Evolved)
        {
            string name = plot.EvolvedCharacter?.characterName ?? "this character";
            TutorialConsole.Warn($"You cannot remove {name}'s plot.");
            return;
        }

        if (plot.State != FarmPlot.PlotState.Empty)
        {
            TutorialConsole.Warn("Remove the plant before removing the plot.");
            return;
        }

        plot.RemovePlot();
        Inventory.Instance.UseTool(plot.GetComponent<FarmPlot>() != null
            ? Inventory.Instance.SelectedSlot.Tool : null);
    }

    // ── Watering can ─────────────────────────────────────────

    void UseWateringCan(FarmPlot plot, ToolData tool)
    {
        if (plot.State == FarmPlot.PlotState.Evolved)
        {
            string name = plot.EvolvedCharacter?.characterName ?? "the character";
            TutorialConsole.Log($"You watered {name}'s plot. How thoughtful!");
            return;
        }

        if (plot.State != FarmPlot.PlotState.Growing)
        {
            TutorialConsole.Warn("Watering can only be used on a growing plant.");
            return;
        }

        plot.ApplyWatering();
        if (tool.isConsumable) Inventory.Instance.UseTool(tool);

        AudioManager.Play(SoundEvent.PlotWatered);
    }

    // ── Fertilizer ────────────────────────────────────────────

    void UseFertilizer(FarmPlot plot, ToolData tool)
    {
        if (plot.State != FarmPlot.PlotState.Empty)
        {
            TutorialConsole.Warn("Fertilizer cannot be used on an occupied farm plot.");
            return;
        }

        if (plot.IsFertilized)
        {
            TutorialConsole.Warn("This plot is already fertilized.");
            return;
        }

        if (tool.fertilizerData == null)
        {
            Debug.LogError($"ToolUser: {tool.toolName} has no FertilizerData assigned.");
            return;
        }

        plot.ApplyFertilizer(tool.fertilizerData);
        if (tool.isConsumable) Inventory.Instance.UseTool(tool);

        AudioManager.Play(SoundEvent.FertilizerApplied);
    }

    // ── Raycast ───────────────────────────────────────────────

    static FarmPlot RaycastPlot()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 100f))
            return hit.collider.GetComponent<FarmPlot>();
        return null;
    }
}
