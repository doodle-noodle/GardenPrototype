using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedHotbar : MonoBehaviour
{
    public static SeedHotbar Instance;

    private GameObject hotbarPanel;

    void Awake()
    {
        Instance = this;

        // Subscribe to EventBus instead of being called directly
        EventBus.OnSeedAdded    += _ => Refresh();
        EventBus.OnSeedUsed     += _ => Refresh();
        EventBus.OnShopClosed   += Refresh;
    }

    void OnDestroy()
    {
        EventBus.OnSeedAdded    -= _ => Refresh();
        EventBus.OnSeedUsed     -= _ => Refresh();
        EventBus.OnShopClosed   -= Refresh;
    }

    void Start()
    {
        BuildHotbar();
        Refresh();
    }

    void BuildHotbar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        hotbarPanel = new GameObject("Hotbar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        hotbarPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = hotbarPanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 20);
        rt.sizeDelta        = new Vector2(500, 64);

        hotbarPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        HorizontalLayoutGroup hlg = hotbarPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6;
        hlg.padding              = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
    }

    public void Refresh()
    {
        if (hotbarPanel == null) return;

        foreach (Transform child in hotbarPanel.transform)
            Destroy(child.gameObject);

        var allSeeds = Inventory.Instance.GetAllSeeds()
            .Where(k => k.Value > 0)
            .ToList();

        if (allSeeds.Count == 0) return;

        int index = 1;
        foreach (var kvp in allSeeds)
        {
            CropData captured  = kvp.Key;
            bool     isSelected = Inventory.Instance.SelectedSeed == captured;

            // Slot
            GameObject slot = new GameObject("Slot_" + captured.cropName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            slot.transform.SetParent(hotbarPanel.transform, false);

            RectTransform srt = slot.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(88, 48);

            slot.GetComponent<Image>().color = isSelected
                ? new Color(0.25f, 0.65f, 0.25f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);

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
            lrt.anchorMin        = Vector2.zero;
            lrt.anchorMax        = Vector2.one;
            lrt.sizeDelta        = Vector2.zero;
            lrt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = lbl.GetComponent<TextMeshProUGUI>();
            tmp.text      = $"[{index}] {captured.cropName}\nx{kvp.Value}";
            tmp.fontSize  = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            index++;
        }
    }

    void Update()
    {
        if (ShopUI.IsOpen) return;

        var allSeeds = Inventory.Instance.GetAllSeeds()
            .Where(k => k.Value > 0)
            .Select(k => k.Key)
            .ToList();

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