using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedHotbar : MonoBehaviour
{
    public static SeedHotbar Instance;

    private GameObject hotbarPanel;
    private List<CropData> seedOrder = new();

    void Awake() => Instance = this;

    void Start() => BuildHotbar();

    void BuildHotbar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        hotbarPanel = new GameObject("Hotbar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        hotbarPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = hotbarPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 20);
        rt.sizeDelta = new Vector2(400, 60);

        hotbarPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        HorizontalLayoutGroup hlg = hotbarPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
    }

    public void Refresh()
    {
        foreach (Transform child in hotbarPanel.transform) Destroy(child.gameObject);
        seedOrder.Clear();

        var allSeeds = Inventory.Instance.GetAllSeeds();
        int index = 1;

        foreach (var kvp in allSeeds)
        {
            if (kvp.Value <= 0) continue;

            CropData captured = kvp.Key;
            seedOrder.Add(captured);

            bool isSelected = Inventory.Instance.SelectedSeed == captured;

            // Slot background
            GameObject slot = new GameObject("Slot_" + captured.cropName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            slot.transform.SetParent(hotbarPanel.transform, false);

            RectTransform srt = slot.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(80, 44);

            slot.GetComponent<Image>().color = isSelected
                ? new Color(0.3f, 0.7f, 0.3f, 1f)
                : new Color(0.25f, 0.25f, 0.25f, 1f);

            slot.GetComponent<Button>().onClick.AddListener(() =>
            {
                Inventory.Instance.SelectSeed(captured);
                Refresh();
            });

            // Label
            GameObject lbl = new GameObject("Label", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lbl.transform.SetParent(slot.transform, false);

            RectTransform lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            lrt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = lbl.GetComponent<TextMeshProUGUI>();
            tmp.text = $"[{index}] {captured.cropName}\nx{kvp.Value}";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            index++;
        }
    }

    void Update()
    {
        if (ShopUI.IsOpen) return;

        // Press 1, 2, 3... to select seed slots
        var allSeeds = Inventory.Instance.GetAllSeeds()
            .Where(k => k.Value > 0).Select(k => k.Key).ToList();

        for (int i = 0; i < allSeeds.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Inventory.Instance.SelectSeed(allSeeds[i]);
                Refresh();
            }
        }
    }
}