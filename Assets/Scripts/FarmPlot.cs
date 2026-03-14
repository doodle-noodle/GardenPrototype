using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    private CropData activeCrop;
    private int currentStage = -1;
    private float stageTimer = 0f;
    private bool isGrown = false;
    private GameObject cropSphere;

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

    void OnMouseDown()
    {
        if (ShopUI.IsOpen) return;
        if (activeCrop == null)
            TryPlant();
        else if (isGrown)
            Harvest();
        else
        {
            float remaining = activeCrop.growthStages[currentStage].duration - stageTimer;
            Debug.Log($"{activeCrop.cropName} — stage {currentStage + 1}/{activeCrop.growthStages.Length}, {remaining:F1}s left");
        }
    }

    void TryPlant()
    {
        CropData selected = Inventory.Instance.SelectedSeed;

        if (selected == null)
        {
            Debug.Log("No seed selected. Buy seeds from the shop first.");
            return;
        }

        if (!Inventory.Instance.UseSeed(selected))
        {
            Debug.Log($"No {selected.cropName} seeds left. Buy more from the shop.");
            return;
        }

        activeCrop = selected;
        currentStage = 0;
        stageTimer = 0f;
        isGrown = false;
        UpdateVisual();
        Debug.Log($"Planted {activeCrop.cropName}!");
    }

    void Harvest()
    {
        Inventory.Instance.AddHarvest(activeCrop);
        Debug.Log($"Harvested {activeCrop.cropName}! Go sell it at the shop.");

        activeCrop = null;
        currentStage = -1;
        isGrown = false;

        if (cropSphere != null) { Destroy(cropSphere); cropSphere = null; }
    }

    void UpdateVisual()
    {
        GrowthStage stage = activeCrop.growthStages[currentStage];

        if (cropSphere == null)
        {
            cropSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            // Parent to scene root instead of the plot so it doesn't inherit squashed Y scale
            cropSphere.transform.SetParent(null);
            Destroy(cropSphere.GetComponent<Collider>());
        }

        float s = stage.scale;
        cropSphere.transform.localScale = new Vector3(s, s, s);

        // Position in world space — sit on top of the plot's world surface
        Vector3 plotTop = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        cropSphere.transform.position = plotTop + Vector3.up * (s * 0.5f);

        cropSphere.GetComponent<Renderer>().material.color = stage.stageColor;
    }
}