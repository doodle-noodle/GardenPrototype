using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;
    public static bool IsOpen { get; private set; }

    [Header("Direct placeable references")]
    public PlaceableData farmPlotPlaceable;

    private GameObject      shopPanel;
    private Transform       stockContainer;
    private Transform       sellContainer;
    private TextMeshProUGUI noHarvestText;
    private TextMeshProUGUI refreshTimerText;
    private bool            isOpen = false;

    void Awake()
    {
        Instance = this;
        EventBus.OnShopStockRefreshed += OnStockRefreshed;
    }

    void OnDestroy()
    {
        EventBus.OnShopStockRefreshed -= OnStockRefreshed;
    }

    void Start()
    {
        BuildShopUI();
        shopPanel.SetActive(false);
    }

    void Update()
    {
        if (isOpen && refreshTimerText != null && ShopStock.Instance != null)
        {
            float t = ShopStock.Instance.TimeUntilRefresh;
            refreshTimerText.text = $"Refreshes in: {(int)(t / 60)}:{(t % 60):00}";
        }
    }

    #region Build UI

    void BuildShopUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        shopPanel = MakePanel(canvas.transform, new Vector2(440, 620), "ShopPanel");

        MakeText(shopPanel.transform, "Shop", 22,
            new Vector2(0, 285), new Vector2(420, 40));

        refreshTimerText = MakeText(shopPanel.transform, "", 12,
            new Vector2(0, 258), new Vector2(420, 24)).GetComponent<TextMeshProUGUI>();

        MakeText(shopPanel.transform, "— For Sale —", 14,
            new Vector2(0, 228), new Vector2(420, 28));

        GameObject stockScroll = MakeScrollArea(shopPanel.transform,
            new Vector2(0, 100), new Vector2(420, 240));
        stockContainer = stockScroll.transform.Find("Content");

        MakeText(shopPanel.transform, "— Sell Harvest —", 14,
            new Vector2(0, -65), new Vector2(420, 28));

        MakeButton(shopPanel.transform, "Sell All", new Vector2(0, -95),
            new Vector2(120, 30), new Color(0.5f, 0.35f, 0.05f), () => SellAll());

        GameObject sellScroll = MakeScrollArea(shopPanel.transform,
            new Vector2(0, -190), new Vector2(420, 150));
        sellContainer = sellScroll.transform.Find("Content");

        var noHarvestObj = MakeText(shopPanel.transform, "Nothing to sell yet.", 13,
            new Vector2(0, -190), new Vector2(400, 28));
        noHarvestText = noHarvestObj.GetComponent<TextMeshProUGUI>();

        MakeButton(shopPanel.transform, "Close", new Vector2(0, -290),
            new Vector2(120, 36), new Color(0.7f, 0.25f, 0.25f), () => CloseShop());
    }

    #endregion

    #region Show / Hide

    public void ToggleShop()
    {
        isOpen = !isOpen;
        IsOpen = isOpen;
        shopPanel.SetActive(isOpen);

        if (isOpen)
        {
            BuildStockButtons();
            RefreshSellButtons();
            EventBus.Raise_ShopOpened();
        }
        else
        {
            EventBus.Raise_ShopClosed();
        }
    }

    public void CloseShop()
    {
        isOpen = false;
        IsOpen = false;
        shopPanel.SetActive(false);
        EventBus.Raise_ShopClosed();
    }

    void OnStockRefreshed()
    {
        if (isOpen) BuildStockButtons();
    }

    #endregion

    #region Buy Plot Button

    public void OnBuyPlotClicked()
    {
        if (farmPlotPlaceable == null)
        {
            TutorialConsole.Error("Farm plot placeable not assigned in ShopUI.");
            return;
        }

        if (!GameManager.Instance.SpendCoins(farmPlotPlaceable.unlockCost)) return;

        PlacementController.Instance.BeginPlacement(farmPlotPlaceable);
        TutorialConsole.Log("Click the grid to place your Farm Plot. Press Escape to cancel.");
    }

    #endregion

    #region Buy

    void BuildStockButtons()
    {
        foreach (Transform child in stockContainer) Destroy(child.gameObject);
        if (ShopStock.Instance == null) return;

        foreach (var item in ShopStock.Instance.CurrentStock)
        {
            ShopItem captured = item;

            Color btnColor = RarityUtility.RarityColor(item.Rarity) * 0.55f;
            btnColor.a = 1f;

            MakeButton(stockContainer,
                $"{item.DisplayName}  —  {item.Price} coins",
                Vector2.zero, new Vector2(400, 44),
                btnColor, () => BuyItem(captured));
        }
    }

    void BuyItem(ShopItem item)
    {
        if (!GameManager.Instance.SpendCoins(item.Price)) return;

        switch (item.Type)
        {
            case ShopItem.ItemType.Seed:
                Inventory.Instance.AddSeed(item.Crop);
                TutorialConsole.Log($"Bought {RarityUtility.RarityLabel(item.Rarity)} " +
                    $"{item.Crop.cropName} seed. Click a farm plot to plant it.");
                break;
            case ShopItem.ItemType.Placeable:
                PlacementController.Instance.BeginPlacement(item.Placeable);
                TutorialConsole.Log($"Click the grid to place your " +
                    $"{item.Placeable.placeableName}. Press Escape to cancel.");
                CloseShop();
                break;
        }
    }

    #endregion

    #region Sell

    void RefreshSellButtons()
    {
        foreach (Transform child in sellContainer) Destroy(child.gameObject);

        var  groups   = Inventory.Instance.GetGroupedHarvest();
        bool hasItems = false;

        foreach (var kvp in groups)
        {
            if (kvp.Value.Count == 0) continue;
            hasItems = true;

            var captured = kvp.Value;
            var sample   = kvp.Value[0];
            int total    = 0;
            foreach (var h in captured) total += h.SellValue;

            string label = $"Sell {sample.DisplayName} " +
                           $"{RankUtility.RankLabel(sample.Rank)} " +
                           $"x{captured.Count}  —  +{total} coins";

            MakeButton(sellContainer, label, Vector2.zero,
                new Vector2(400, 44),
                ColorForRank(sample.Rank),
                () => SellGroup(captured));
        }

        if (noHarvestText != null)
            noHarvestText.gameObject.SetActive(!hasItems);
    }

    void SellGroup(List<HarvestedCrop> group)
    {
        foreach (var crop in new List<HarvestedCrop>(group))
        {
            GameManager.Instance.AddCoins(crop.SellValue);
            Inventory.Instance.RemoveHarvest(crop);
            EventBus.Raise_CropSold(crop);
        }
        RefreshSellButtons();
    }

    void SellAll()
    {
        var allHarvested = new List<HarvestedCrop>(Inventory.Instance.GetAllHarvested());
        if (allHarvested.Count == 0)
        {
            TutorialConsole.Warn("Nothing to sell.");
            return;
        }

        int total = 0;
        foreach (var crop in allHarvested)
        {
            total += crop.SellValue;
            Inventory.Instance.RemoveHarvest(crop);
            EventBus.Raise_CropSold(crop);
        }

        GameManager.Instance.AddCoins(total);
        TutorialConsole.Log($"Sold everything for {total} coins.");
        RefreshSellButtons();
    }

    Color ColorForRank(Rank rank) => rank switch
    {
        Rank.D => new Color(0.35f, 0.35f, 0.35f),
        Rank.C => new Color(0.35f, 0.35f, 0.55f),
        Rank.B => new Color(0.15f, 0.45f, 0.15f),
        Rank.A => new Color(0.15f, 0.25f, 0.60f),
        Rank.S => new Color(0.60f, 0.50f, 0.05f),
        _      => new Color(0.35f, 0.35f, 0.35f)
    };

    #endregion

    #region UI Helpers

    GameObject MakePanel(Transform parent, Vector2 size, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        return go;
    }

    GameObject MakeScrollArea(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject scroll = new GameObject("Scroll", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scroll.transform.SetParent(parent, false);
        var srt = scroll.GetComponent<RectTransform>();
        srt.sizeDelta        = size;
        srt.anchoredPosition = pos;
        scroll.GetComponent<Image>().color          = new Color(0.12f, 0.12f, 0.12f, 1f);
        scroll.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin        = new Vector2(0, 1);
        crt.anchorMax        = new Vector2(1, 1);
        crt.pivot            = new Vector2(0.5f, 1f);
        crt.sizeDelta        = Vector2.zero;
        crt.anchoredPosition = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 6;
        vlg.padding              = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scroll.GetComponent<ScrollRect>();
        sr.content           = crt;
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.scrollSensitivity = 20f;

        return scroll;
    }

    GameObject MakeButton(Transform parent, string label, Vector2 pos,
        Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject("Btn", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);

        GameObject txt = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txt.transform.SetParent(go.transform, false);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin        = Vector2.zero;
        trt.anchorMax        = Vector2.one;
        trt.sizeDelta        = Vector2.zero;
        trt.anchoredPosition = Vector2.zero;

        var tmp = txt.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.richText  = true;

        return go;
    }

    GameObject MakeText(Transform parent, string content, int fontSize,
        Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("Txt", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = content;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.richText  = true;

        return go;
    }

    #endregion
}