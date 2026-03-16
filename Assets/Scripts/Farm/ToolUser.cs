using UnityEngine;

public class ToolUser : MonoBehaviour
{
    public static ToolUser Instance;

    private Camera _mainCamera;

    void Awake()
    {
        Instance    = this;
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (ShopUI.IsOpen) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        InventorySlot selected = Inventory.Instance.SelectedSlot;
        if (selected == null || selected.Type != InventoryItemType.Tool) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

        FarmPlot plot = hit.collider.GetComponent<FarmPlot>();
        if (plot == null) return;

        UseTool(selected.Tool, plot);
    }

    void UseTool(ToolData tool, FarmPlot plot)
    {
        switch (tool.toolType)
        {
            case ToolType.Shovel:
                UseShovel(tool, plot);
                break;
        }

        // Only consume if marked consumable in the Inspector
        if (tool.isConsumable)
            Inventory.Instance.UseTool(tool);
    }

    void UseShovel(ToolData tool, FarmPlot plot)
    {
        plot.RemovePlot();
        TutorialConsole.Log("Removed farm plot with shovel.");
    }
}