using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    private CropData activeCrop;
    private int currentStage = -1;
    private float stageTimer = 0f;
    private bool isGrown = false;

    private GameObject cropSphere;
    private GameObject ghostSphere;  // preview shown on hover

    // ── Hover ghost ───────────────────────────────────────────

    void OnMouseEnter()
    {
        if (ShopUI.IsOpen) return;
        if (activeCrop != null) return;  // plot is already in use
        if (Inventory.Instance.SelectedSeed == null) return;
        if (Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) <= 0) return;

        ShowGhost();
    }

    void OnMouseOver()
    {
        // If the selected seed changed while hovering, refresh the ghost
        if (ghostSphere == null && activeCrop == null
            && Inventory.Instance.SelectedSeed != null
            && Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) > 0
            && !ShopUI.IsOpen)
        {
            ShowGhost();
        }
    }

    void OnMouseExit()
    {
        HideGhost();
    }

    void ShowGhost()
    {
        HideGhost();

        CropData seed = Inventory.Instance.SelectedSeed;
        if (seed == null || seed.growthStages.Length == 0) return;

        ghostSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ghostSphere.transform.SetParent(null);
        Destroy(ghostSphere.GetComponent<Collider>());

        GrowthStage firstStage = seed.growthStages[0];
        float s = firstStage.scale;
        ghostSphere.transform.localScale = new Vector3(s, s, s);

        Vector3 plotTop = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        ghostSphere.transform.position = plotTop + Vector3.up * (s * 0.5f);

        // Use the same GhostValid material as the farm plot placement preview
        if (PlacementController.GhostValid != null)
            ghostSphere.GetComponent<Renderer>().material = PlacementController.GhostValid;
    }

    void HideGhost()
    {
        if (ghostSphere != null)
        {
            Destroy(ghostSphere);
            ghostSphere = null;
        }
    }

    // ── Click ─────────────────────────────────────────────────

    void OnMouseDown()
    {
        if (ShopUI.IsOpen) return;

        if (activeCrop == null)
        {
            if (TryPlant())
                HideGhost();  // ghost becomes the real plant
        }
        else if (isGrown)
        {
            Harvest();
        }
        else
        {
            float remaining = activeCrop.growthStages[currentStage].duration - stageTimer;
            Debug.Log($"{activeCrop.cropName} — stage {currentStage + 1}/{activeCrop.growthStages.Length}, {remaining:F1}s left");
        }
    }

    // ── Planting ──────────────────────────────────────────────

    bool TryPlant()
    {
        CropData selected = Inventory.Instance.SelectedSeed;

        if (selected == null)
        {
            Debug.Log("No seed selected. Buy seeds from the shop first.");
            return false;
        }

        if (!Inventory.Instance.UseSeed(selected))
        {
            Debug.Log($"No {selected.cropName} seeds left. Buy more from the shop.");
            return false;
        }

        activeCrop = selected;
        currentStage = 0;
        stageTimer = 0f;
        isGrown = false;
        UpdateVisual();
        Debug.Log($"Planted {activeCrop.cropName}!");
        return true;
    }

    // ── Growing ───────────────────────────────────────────────

    void Update()
    {
        if (activeCrop == null || isGrown) return;

        stageTimer += Time.deltaTime;
        GrowthStage stage = activeCrop.growthStages[currentStage];
        bool isFinalStage = currentStage == activeCrop.growthStages.Length - 1;

        if (!isFinalStage && stageTimer >= stage.duration)
        {
            currentStage++;
            stageTimer = 0f;
            UpdateVisual();
        }
        else if (isFinalStage && !isGrown)
        {
            isGrown = true;
            UpdateVisual();
            Debug.Log($"{activeCrop.cropName} is ready to harvest!");
        }
    }

    // ── Harvest ───────────────────────────────────────────────

    void Harvest()
    {
        Inventory.Instance.AddHarvest(activeCrop);
        Debug.Log($"Harvested {activeCrop.cropName}! Go sell it at the shop.");

        activeCrop = null;
        currentStage = -1;
        isGrown = false;

        if (cropSphere != null) { Destroy(cropSphere); cropSphere = null; }
    }

    // ── Visuals ───────────────────────────────────────────────

    void UpdateVisual()
    {
        if (cropSphere == null)
        {
            cropSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cropSphere.transform.SetParent(null);
            Destroy(cropSphere.GetComponent<Collider>());
        }

        GrowthStage stage = activeCrop.growthStages[currentStage];
        float s = stage.scale;
        cropSphere.transform.localScale = new Vector3(s, s, s);

        Vector3 plotTop = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        cropSphere.transform.position = plotTop + Vector3.up * (s * 0.5f);

        cropSphere.GetComponent<Renderer>().material.color = stage.stageColor;
    }

    void OnDestroy()
    {
        // Clean up floating spheres if the plot is ever destroyed
        if (cropSphere != null) Destroy(cropSphere);
        if (ghostSphere != null) Destroy(ghostSphere);
    }
}