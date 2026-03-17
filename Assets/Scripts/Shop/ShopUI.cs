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

    // ── Font sizes ────────────────────────────────────────────
    private const int FontTitle  = 44;
    private const int FontHeader = 28;
    private const int FontBody   = 24;
    private const int FontSmall  = 22;

    // ── Panel layout ──────────────────────────────────────────
    private const float PanelW      = 560f;
    private const float PanelH      = 880f;
    private const float ButtonW     = 520f;
    private const float ButtonH     = 60f;
    private const float ScrollBuyH  = 280f;
    private const float ScrollSellH = 180f;
    private const float Gap         = 16f;

    // ── Hold-to-buy settings ──────────────────────────────────
    private const float HoldInitialDelay  = 0.4f;   // seconds before repeat starts
    private const float HoldRepeatMin     = 0.15f;  // slowest repeat interval
    private const float HoldRepeatMax     = 0.02f;  // fastest repeat interval
    private const float HoldAccelDuration = 3f;     // seconds to reach max speed

    private GameObject      shopPanel;
    private Transform       stockContainer;
    private Transform       sellContainer;
    private TextMeshProUGUI noHarvestText;
    private TextMeshProUGUI refreshTimerText;
    private bool            isOpen = false;

    // ── Hold-to-buy state ─────────────────────────────────────
    private ShopItem  _heldItem;
    private bool      _holdActive     = false;
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

    // ── Build UI ──────────────────────────────────────────────

    void BuildShopUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        shopPanel     = MakePanel(canvas.transform, new Vector2(PanelW, PanelH), "ShopPanel");

        var shopRt = shopPanel.GetComponent<RectTransform>();
        shopRt.anchorMin = new Vector2(0.5f, 0.5f);
        shopRt.anchorMax = new Vector2(0.5f, 0.5f);
        shopRt.pivot     = new Vector2(0.5f, 0.5f);

        float top    = PanelH * 0.5f;
        float titleH = 52f;

        MakeText(shopPanel.transform, "Shop", FontTitle,
            new Vector2(0f, top - titleH * 0.5f - Gap),
            new Vector2(PanelW - 20f, titleH));

        float timerY = top - titleH - Gap * 2f - 15f;
        refreshTimerText = MakeText(shopPanel.transform, "", FontSmall,
            new Vector2(0f, timerY),
            new Vector2(PanelW - 20f, 30f)).GetComponent<TextMeshProUGUI>();

        float buyHeaderY = timerY - 30f - Gap;
        MakeText(shopPanel.transform, "— Buy —", FontHeader,
            new Vector2(0f, buyHeaderY),
            new Vector2(PanelW - 20f, 36f));

        float buyScrollY = buyHeaderY - 36f * 0.5f - ScrollBuyH * 0.5f - Gap;
        var   stockScroll = MakeScrollArea(shopPanel.transform,
            new Vector2(0f, buyScrollY),
            new Vector2(PanelW - 20f, ScrollBuyH));
        stockContainer = stockScroll.transform.Find("Content");

        float sellHeaderY = buyScrollY - ScrollBuyH * 0.5f - Gap - 18f;
        MakeText(shopPanel.transform, "— Sell —", FontHeader,
            new Vector2(0f, sellHeaderY),
            new Vector2(PanelW - 20f, 36f));

        float sellBtnY = sellHeaderY - 36f * 0.5f - ButtonH * 0.5f - Gap * 0.5f;
        MakeButton(shopPanel.transform, "Sell All",
            new Vector2(0f, sellBtnY),
            new Vector2(160f, ButtonH),
            UIColors.ShopBtnSellAll, () => SellAll());

        float sellScrollY = sellBtnY - ButtonH * 0.5f - ScrollSellH * 0.5f - Gap * 0.5f;
        var   sellScroll  = MakeScrollArea(shopPanel.transform,
            new Vector2(0f, sellScrollY),
            new Vector2(PanelW - 20f, ScrollSellH));
        sellContainer = sellScroll.transform.Find("Content");

        var noHarvestObj = MakeText(shopPanel.transform, "Nothing to sell yet.", FontBody,
            new Vector2(0f, sellScrollY),
            new Vector2(PanelW - 20f, 36f));
        noHarvestText = noHarvestObj.GetComponent<TextMeshProUGUI>();

        float closeBtnY = sellScrollY - ScrollSellH * 0.5f - ButtonH * 0.5f - Gap;
        MakeButton(shopPanel.transform, "Close",
            new Vector2(0f, closeBtnY),
            new Vector2(160f, ButtonH),
            UIColors.ShopBtnClose, () => CloseShop());
    }

    // ── Show / Hide ───────────────────────────────────────────

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

    void OnStockRefreshed()
    {
        if (isOpen) BuildStockButtons();
    }

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

            GameObject btn = MakeButton(stockContainer,
                $"{item.DisplayName}  —  {item.Price} coins",
                Vector2.zero, new Vector2(ButtonW, ButtonH),
                btnColor, () => BuyItem(captured));

            // Add hold-to-buy only for consumable items (seeds)
            // Non-consumable items like tools and placeables use single click only
            if (item.Type == ShopItem.ItemType.Seed)
                AddHoldToBuy(btn, captured);
        }
    }

    // Attaches pointer down/up events for hold-to-buy behaviour
    void AddHoldToBuy(GameObject btn, ShopItem item)
    {
        var trigger = btn.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ => StartHold(item));
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ => StopHold());
        trigger.triggers.Add(pointerUp);

        var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(_ => StopHold());
        trigger.triggers.Add(pointerExit);
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
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }
    }

    IEnumerator HoldBuyRoutine()
    {
        // Initial delay before repeat starts
        yield return new WaitForSeconds(HoldInitialDelay);

        float holdTime = 0f;

        while (_holdActive && _heldItem != null)
        {
            BuyItem(_heldItem);

            holdTime += HoldRepeatMin;

            // Gradually decrease interval as hold time increases
            float t        = Mathf.Clamp01(holdTime / HoldAccelDuration);
            float interval = Mathf.Lerp(HoldRepeatMin, HoldRepeatMax, t);

            yield return new WaitForSeconds(interval);
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
                Inventory.Instance.AddTool(item.Tool);
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

            var    captured = kvp.Value;
            var    sample   = kvp.Value[0];
            int    total    = 0;
            foreach (var h in captured) total += h.SellValue;

            string label = $"Sell {sample.DisplayName} " +
                           $"{RankUtility.RankLabel(sample.Rank)} " +
                           $"x{captured.Count}  —  +{total} coins";

            MakeButton(sellContainer, label, Vector2.zero,
                new Vector2(ButtonW, ButtonH),
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
        AudioManager.Play(SoundEvent.ItemSold);
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
        AudioManager.Play(SoundEvent.ItemSold);
        TutorialConsole.Log($"Sold everything for {total} coins.");
        RefreshSellButtons();
    }

    Color ColorForRank(Rank rank)
    {
        return RankUtility.RankButtonColor(rank);
    }

    // ── UI Helpers ────────────────────────────────────────────

    GameObject MakePanel(Transform parent, Vector2 size, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = UIColors.ShopPanel;
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
        scroll.GetComponent<Image>().color          = UIColors.ShopScroll;
        scroll.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin        = new Vector2(0f, 1f);
        crt.anchorMax        = new Vector2(1f, 1f);
        crt.pivot            = new Vector2(0.5f, 1f);
        crt.sizeDelta        = Vector2.zero;
        crt.anchoredPosition = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8;
        vlg.padding              = new RectOffset(10, 10, 10, 10);
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
        trt.sizeDelta        = new Vector2(-8f, 0f);
        trt.anchoredPosition = Vector2.zero;

        var tmp = txt.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = FontBody;
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
}