using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;
    public static bool IsOpen { get; private set; }

    [Header("All crops in the game")]
    public CropData[] allCrops;

    private GameObject shopPanel;
    private Transform seedContainer;
    private Transform sellContainer;
    private TextMeshProUGUI noHarvestText;
    private bool isOpen = false;

    void Awake() => Instance = this;

    void Start()
    {
        BuildShopUI();
        shopPanel.SetActive(false);
    }

    // ── Build the entire shop UI in code ─────────────────────

    void BuildShopUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // Main panel
        shopPanel = MakePanel(canvas.transform, new Vector2(400, 500), "ShopPanel");

        // Title
        MakeText(shopPanel.transform, "Shop", 22, new Vector2(0, 210), new Vector2(380, 40));

        // Divider label
        MakeText(shopPanel.transform, "— Buy Seeds —", 14, new Vector2(0, 170), new Vector2(380, 30));

        // Seed scroll area
        GameObject seedScroll = MakeScrollArea(shopPanel.transform, new Vector2(0, 60), new Vector2(380, 200));
        seedContainer = seedScroll.transform.Find("Content");

        // Divider label
        MakeText(shopPanel.transform, "— Sell Harvest —", 14, new Vector2(0, -90), new Vector2(380, 30));

        // Sell scroll area
        GameObject sellScroll = MakeScrollArea(shopPanel.transform, new Vector2(0, -160), new Vector2(380, 120));
        sellContainer = sellScroll.transform.Find("Content");

        // No harvest text
        GameObject noHarvestObj = MakeText(shopPanel.transform, "Nothing to sell yet.", 13,
            new Vector2(0, -160), new Vector2(360, 30));
        noHarvestText = noHarvestObj.GetComponent<TextMeshProUGUI>();

        // Close button
        MakeButton(shopPanel.transform, "Close", new Vector2(0, -225), new Vector2(120, 36),
            new Color(0.8f, 0.3f, 0.3f), () => CloseShop());

        BuildSeedButtons();
    }

    // ── Shop logic ────────────────────────────────────────────

    public void ToggleShop()
    {
        isOpen = !isOpen;
        IsOpen = isOpen;
        shopPanel.SetActive(isOpen);
        if (isOpen) RefreshSellButtons();
    }

    public void CloseShop()
    {
        isOpen = false;
        IsOpen = false;
        shopPanel.SetActive(false);
    }

    void BuildSeedButtons()
    {
        foreach (Transform child in seedContainer) Destroy(child.gameObject);

        foreach (var crop in allCrops)
        {
            CropData captured = crop;
            MakeButton(seedContainer, $"Buy {crop.cropName} seed  —  {crop.seedCost} coins",
                Vector2.zero, new Vector2(340, 44), new Color(0.25f, 0.6f, 0.3f),
                () => BuySeed(captured));
        }
    }

    void BuySeed(CropData crop)
    {
        if (!GameManager.Instance.SpendCoins(crop.seedCost)) return;
        Inventory.Instance.AddSeed(crop);
        CloseShop();
    }

    void RefreshSellButtons()
    {
        foreach (Transform child in sellContainer) Destroy(child.gameObject);

        var harvested = Inventory.Instance.GetAllHarvested();
        bool hasAnything = false;

        foreach (var kvp in harvested)
        {
            if (kvp.Value <= 0) continue;
            hasAnything = true;

            CropData captured = kvp.Key;
            int count = kvp.Value;
            int total = captured.sellValue * count;

            MakeButton(sellContainer,
                $"Sell {captured.cropName}  x{count}  —  +{total} coins",
                Vector2.zero, new Vector2(340, 44), new Color(0.7f, 0.55f, 0.1f),
                () => SellAll(captured));
        }

        if (noHarvestText != null)
            noHarvestText.gameObject.SetActive(!hasAnything);
    }

    void SellAll(CropData crop)
    {
        int count = Inventory.Instance.GetHarvestCount(crop);
        for (int i = 0; i < count; i++)
        {
            Inventory.Instance.UseHarvest(crop);
            GameManager.Instance.AddCoins(crop.sellValue);
        }
        RefreshSellButtons();
    }

    // ── UI helpers ────────────────────────────────────────────

    GameObject MakePanel(Transform parent, Vector2 size, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        return go;
    }

    GameObject MakeScrollArea(Transform parent, Vector2 pos, Vector2 size)
    {
        // Viewport
        GameObject scroll = new GameObject("Scroll", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(ScrollRect), typeof(Mask));
        scroll.transform.SetParent(parent, false);
        RectTransform srt = scroll.GetComponent<RectTransform>();
        srt.sizeDelta = size;
        srt.anchoredPosition = pos;
        scroll.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        scroll.GetComponent<Mask>().showMaskGraphic = true;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.sizeDelta = new Vector2(0, 0);
        crt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scroll.GetComponent<ScrollRect>();
        sr.content   = crt;
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 20f;

        return scroll;
    }

    GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);

        GameObject txt = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txt.transform.SetParent(go.transform, false);
        RectTransform trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        trt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = txt.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    GameObject MakeText(Transform parent, string content, int fontSize, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("Text_" + content, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }
}