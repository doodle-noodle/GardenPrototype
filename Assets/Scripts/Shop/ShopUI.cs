using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;
    public static bool IsOpen { get; private set; }

    private const int   FontTitle  = 44;
    private const int   FontHeader = 28;
    private const int   FontBody   = 24;
    private const int   FontSmall  = 22;
    private const float PanelW     = 560f;
    private const float PanelH     = 880f;
    private const float ButtonW    = 520f;
    private const float ButtonH    = 60f;
    private const float ScrollBuyH = 280f;
    private const float ScrollSellH = 180f;
    private const float Gap        = 16f;

    private const float HoldInitialDelay  = 0.4f;
    private const float HoldRepeatMin     = 0.15f;
    private const float HoldRepeatMax     = 0.02f;
    private const float HoldAccelDuration = 3f;

    private GameObject      shopPanel;
    private Transform       stockContainer;
    private Transform       sellContainer;
    private TextMeshProUGUI noHarvestText;
    private TextMeshProUGUI refreshTimerText;
    private bool            isOpen = false;

    private ShopItem  _heldItem;
    private bool      _holdActive;
    private Coroutine _holdCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────

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
        if (!isOpen || refreshTimerText == null || ShopStock.Instance == null) return;
        float t = ShopStock.Instance.TimeUntilRefresh;
        refreshTimerText.text = $"Refreshes in: {(int)(t / 60)}:{(t % 60):00}";
    }

    // ── Build ─────────────────────────────────────────────────

    void BuildShopUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        shopPanel     = MakePanel(canvas.transform, new Vector2(PanelW, PanelH), "ShopPanel");

        var rt = shopPanel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        float top = PanelH * 0.5f;

        MakeText(shopPanel.transform, "Shop", FontTitle,
            new Vector2(0f, top - 26f - Gap), new Vector2(PanelW - 20f, 52f));

        float timerY = top - 52f - Gap * 2f - 15f;
        refreshTimerText = MakeText(shopPanel.transform, "", FontSmall,
            new Vector2(0f, timerY), new Vector2(PanelW - 20f, 30f))
            .GetComponent<TextMeshProUGUI>();

        float buyHeaderY = timerY - 30f - Gap;
        MakeText(shopPanel.transform, "— Buy —", FontHeader,
            new Vector2(0f, buyHeaderY), new Vector2(PanelW - 20f, 36f));

        float buyScrollY = buyHeaderY - 18f - ScrollBuyH * 0.5f - Gap;
        var stockScroll  = MakeScrollArea(shopPanel.transform,
            new Vector2(0f, buyScrollY), new Vector2(PanelW - 20f, ScrollBuyH));
        stockContainer   = stockScroll.transform.Find("Content");

        float sellHeaderY = buyScrollY - ScrollBuyH * 0.5f - Gap - 18f;
        MakeText(shopPanel.transform, "— Sell —", FontHeader,
            new Vector2(0f, sellHeaderY), new Vector2(PanelW - 20f, 36f));

        float sellBtnY = sellHeaderY - 18f - ButtonH * 0.5f - Gap * 0.5f;
        MakeButton(shopPanel.transform, "Sell All",
            new Vector2(0f, sellBtnY), new Vector2(160f, ButtonH),
            UIColors.ShopBtnSellAll, () => SellAll());

        float sellScrollY = sellBtnY - ButtonH * 0.5f - ScrollSellH * 0.5f - Gap * 0.5f;
        var sellScroll    = MakeScrollArea(shopPanel.transform,
            new Vector2(0f, sellScrollY), new Vector2(PanelW - 20f, ScrollSellH));
        sellContainer     = sellScroll.transform.Find("Content");

        noHarvestText = MakeText(shopPanel.transform, "Nothing to sell yet.", FontBody,
            new Vector2(0f, sellScrollY), new Vector2(PanelW - 20f, 36f))
            .GetComponent<TextMeshProUGUI>();

        float closeBtnY = sellScrollY - ScrollSellH * 0.5f - ButtonH * 0.5f - Gap;
        MakeButton(shopPanel.transform, "Close",
            new Vector2(0f, closeBtnY), new Vector2(160f, ButtonH),
            UIColors.ShopBtnClose, () => CloseShop());
    }

    // ── Show / Hide ───────────────────────────────────────────

    public void ToggleShop()
    {
        InventoryHotbar.CancelPlacementIfActive();  
        InventoryPanel.Instance?.CloseIfOpen(); // close inventory when shop opens

        isOpen = !isOpen;
        IsOpen = isOpen;
        shopPanel.SetActive(isOpen);

        if (isOpen)
        {
            BuildStockButtons();
            RefreshSellButtons();
            EventBus.Raise_ShopOpened();
            AudioManager.Play(SoundEvent.ShopOpened);
        }
        else
        {
            StopHold();
            EventBus.Raise_ShopClosed();
            AudioManager.Play(SoundEvent.ShopClosed);
        }
    }

    public void CloseShop()
    {
        isOpen = false;
        IsOpen = false;
        shopPanel.SetActive(false);
        StopHold();
        EventBus.Raise_ShopClosed();
        AudioManager.Play(SoundEvent.ShopClosed);
    }

    void OnStockRefreshed() { if (isOpen) BuildStockButtons(); }

    // ── Buy ───────────────────────────────────────────────────

    void BuildStockButtons()
    {
        foreach (Transform child in stockContainer) Destroy(child.gameObject);
        if (ShopStock.Instance == null) return;

        foreach (var item in ShopStock.Instance.CurrentStock)
        {
            ShopItem captured = item;
            Color    btnColor = RarityUtility.RarityColor(item.Rarity) * 0.55f;
            btnColor.a = 1f;

            var btn = MakeButton(stockContainer,
                $"{item.DisplayName}  —  {item.Price} coins",
                Vector2.zero, new Vector2(ButtonW, ButtonH),
                btnColor, () => BuyItem(captured));

            if (item.Type == ShopItem.ItemType.Seed)
                AddHoldToBuy(btn, captured);
        }
    }

    void AddHoldToBuy(GameObject btn, ShopItem item)
    {
        var trigger = btn.AddComponent<EventTrigger>();

        AddTriggerEntry(trigger, EventTriggerType.PointerDown, _ => StartHold(item));
        AddTriggerEntry(trigger, EventTriggerType.PointerUp,   _ => StopHold());
        AddTriggerEntry(trigger, EventTriggerType.PointerExit, _ => StopHold());
    }

    void AddTriggerEntry(EventTrigger t, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        t.triggers.Add(entry);
    }

    void StartHold(ShopItem item)
    {
        StopHold();
        _heldItem      = item;
        _holdActive    = true;
        _holdCoroutine = StartCoroutine(HoldBuyRoutine());
    }

    void StopHold()
    {
        _holdActive = false;
        _heldItem   = null;
        if (_holdCoroutine != null) { StopCoroutine(_holdCoroutine); _holdCoroutine = null; }
    }

    IEnumerator HoldBuyRoutine()
    {
        yield return new WaitForSeconds(HoldInitialDelay);
        float holdTime = 0f;
        while (_holdActive && _heldItem != null)
        {
            BuyItem(_heldItem);
            holdTime += HoldRepeatMin;
            float interval = Mathf.Lerp(HoldRepeatMin, HoldRepeatMax,
                Mathf.Clamp01(holdTime / HoldAccelDuration));
            yield return new WaitForSeconds(interval);
        }
    }

    void BuyItem(ShopItem item)
    {
        InventoryHotbar.CancelPlacementIfActive();
        if (!GameManager.Instance.SpendCoins(item.Price)) return;

        switch (item.Type)
        {
            case ShopItem.ItemType.Seed:
                Inventory.Instance.AddSeed(item.Crop);
                TutorialConsole.Log($"Bought {RarityUtility.RarityLabel(item.Rarity)} " +
                    $"{item.Crop.cropName} seed.");
                AudioManager.Play(SoundEvent.ItemBought);
                break;
            case ShopItem.ItemType.Placeable:
                PlacementController.Instance.BeginPlacement(item.Placeable, paid: true);
                TutorialConsole.Log($"Click the grid to place your " +
                    $"{item.Placeable.placeableName}. Press Escape to cancel.");
                AudioManager.Play(SoundEvent.ItemBought);
                CloseShop();
                break;
            case ShopItem.ItemType.Tool:
                Inventory.Instance.AddTool(item.Tool, item.Tool.buyQuantity);
                TutorialConsole.Log($"Bought {RarityUtility.RarityLabel(item.Rarity)} " +
                    $"{item.Tool.toolName}.");
                AudioManager.Play(SoundEvent.ItemBought);
                break;
        }
    }

    // ── Sell ──────────────────────────────────────────────────

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

            // Colored mutation prefix
            string mutPrefix = "";
            if (sample.Mutations != null && sample.Mutations.Count > 0)
            {
                var parts = new List<string>();
                foreach (var m in sample.Mutations)
                {
                    string hex = "#" + ColorUtility.ToHtmlStringRGB(m.tintColor);
                    parts.Add($"<color={hex}>{m.mutationName}</color>");
                }
                mutPrefix = string.Join(" ", parts) + " ";
            }

            string goldHex = "#" + ColorUtility.ToHtmlStringRGB(UIColors.FloatingGold);
            string label   = $"Sell {mutPrefix}{sample.DisplayName}  " +
                             $"x{captured.Count}  " +
                             $"<color={goldHex}>+{total} coins</color>";

            MakeButton(sellContainer, label, Vector2.zero,
                new Vector2(ButtonW, ButtonH),
                SellButtonColor(sample.Mutations?.Count ?? 0),
                () => SellGroup(captured));
        }

        if (noHarvestText != null)
            noHarvestText.gameObject.SetActive(!hasItems);
    }

    Color SellButtonColor(int mutationCount)
    {
        if (mutationCount == 0) return new Color(0.25f, 0.35f, 0.25f, 1f);
        if (mutationCount == 1) return new Color(0.30f, 0.45f, 0.30f, 1f);
        return new Color(0.35f, 0.55f, 0.35f, 1f);
    }

    void SellGroup(List<HarvestedCrop> group)
    {
        foreach (var crop in new List<HarvestedCrop>(group))
        {
            GameManager.Instance.AddCoins(crop.SellValue);
            Inventory.Instance.RemoveHarvest(crop);
            EventBus.Raise_CropSold(crop);
        }
        AudioManager.Play(SoundEvent.ItemSold);
        RefreshSellButtons();
    }

    void SellAll()
    {
        var all = Inventory.Instance.GetAllHarvested();
        if (all.Count == 0) { TutorialConsole.Warn("Nothing to sell."); return; }

        int total = 0;
        foreach (var crop in all)
        {
            total += crop.SellValue;
            Inventory.Instance.RemoveHarvest(crop);
            EventBus.Raise_CropSold(crop);
        }

        GameManager.Instance.AddCoins(total);
        AudioManager.Play(SoundEvent.ItemSold);
        TutorialConsole.Log($"Sold everything for {total} coins.");
        RefreshSellButtons();
    }

    // ── UI helpers ────────────────────────────────────────────

    GameObject MakePanel(Transform parent, Vector2 size, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = UIColors.ShopPanel;
        return go;
    }

    GameObject MakeScrollArea(Transform parent, Vector2 pos, Vector2 size)
    {
        var scroll = new GameObject("Scroll", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scroll.transform.SetParent(parent, false);
        var srt = scroll.GetComponent<RectTransform>();
        srt.sizeDelta = size; srt.anchoredPosition = pos;
        scroll.GetComponent<Image>().color          = UIColors.ShopScroll;
        scroll.GetComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.sizeDelta = Vector2.zero; crt.anchoredPosition = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childForceExpandWidth = true;
        vlg.childControlHeight = vlg.childForceExpandHeight = false;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scroll.GetComponent<ScrollRect>();
        sr.content = crt; sr.horizontal = false;
        sr.vertical = true; sr.scrollSensitivity = 20f;
        return scroll;
    }

    GameObject MakeButton(Transform parent, string label, Vector2 pos,
        Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Btn", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);

        var txt = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txt.transform.SetParent(go.transform, false);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.sizeDelta = new Vector2(-8f, 0f); trt.anchoredPosition = Vector2.zero;

        var tmp = txt.GetComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = FontBody;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.richText = true;
        return go;
    }

    GameObject MakeText(Transform parent, string content, int fontSize,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Txt", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.richText = true;
        return go;
    }
}