using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 50-slot storage panel (5 rows × 10 columns).
// Uses the same outline-behind-background pattern as InventoryHotbar for selection feedback.
public class InventoryPanel : MonoBehaviour
{
    public static InventoryPanel Instance;
    public static bool IsOpen { get; private set; }

    private const int   Cols      = 10;
    private const int   Rows      = 5;
    private const float SlotSize  = 70f;
    private const float SlotGap   = 6f;
    private const float PadX      = 8f;
    private const float PadY      = 8f;
    private const float PanelGap  = 10f;
    private const float TitleH    = 30f;
    private const float OutlinePx = 3f;   // matches hotbar

    private const int FontUI   = 16;
    private const int FontItem = 18;
    private const int FontTag  = 13;

    private GameObject _panel;
    private SlotEl[]   _slots;
    private float      _hotbarTopY;
    private bool       _built;
    private bool       _isOpen;

    private struct SlotEl
    {
        public Image           Outline;
        public Image           Bg;
        public TextMeshProUGUI Main;
        public TextMeshProUGUI Tag;
    }

    private System.Action<CropData>      _onSeedAdded, _onSeedUsed;
    private System.Action<HarvestedCrop> _onHarvested, _onSold;
    private System.Action<ToolData>      _onToolAdded, _onToolUsed;

    void Awake()
    {
        Instance     = this;
        _onSeedAdded = _ => { if (_isOpen) Refresh(); };
        _onSeedUsed  = _ => { if (_isOpen) Refresh(); };
        _onHarvested = _ => { if (_isOpen) Refresh(); };
        _onSold      = _ => { if (_isOpen) Refresh(); };
        _onToolAdded = _ => { if (_isOpen) Refresh(); };
        _onToolUsed  = _ => { if (_isOpen) Refresh(); };

        EventBus.OnSeedAdded     += _onSeedAdded;
        EventBus.OnSeedUsed      += _onSeedUsed;
        EventBus.OnCropHarvested += _onHarvested;
        EventBus.OnCropSold      += _onSold;
        EventBus.OnToolAdded     += _onToolAdded;
        EventBus.OnToolUsed      += _onToolUsed;
    }

    void OnDestroy()
    {
        EventBus.OnSeedAdded     -= _onSeedAdded;
        EventBus.OnSeedUsed      -= _onSeedUsed;
        EventBus.OnCropHarvested -= _onHarvested;
        EventBus.OnCropSold      -= _onSold;
        EventBus.OnToolAdded     -= _onToolAdded;
        EventBus.OnToolUsed      -= _onToolUsed;
    }

    public void SetHotbarTopY(float y)
    {
        _hotbarTopY = y;
        if (!_built) BuildPanel();
    }

    // ── Build ─────────────────────────────────────────────────

    void BuildPanel()
    {
        _built = true;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("InventoryPanel: no Canvas found."); return; }

        float gridW  = Cols * SlotSize + (Cols - 1) * SlotGap;
        float gridH  = Rows * SlotSize + (Rows - 1) * SlotGap;
        float panelW = gridW + PadX * 2f;
        float panelH = gridH + PadY * 2f + TitleH;

        _panel = new GameObject("InventoryStoragePanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _panel.transform.SetParent(canvas.transform, false);

        var panelRt              = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);
        panelRt.anchoredPosition = new Vector2(0f, _hotbarTopY + PanelGap);

        var c          = UIColors.SlotEmpty;
        var panelImg   = _panel.GetComponent<Image>();
        panelImg.color = new Color(c.r, c.g, c.b, 0.90f);
        panelImg.raycastTarget = false;

        // Title
        var titleGo = new GameObject("Title",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(_panel.transform, false);
        var titleRt              = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0f, 1f);
        titleRt.anchorMax        = new Vector2(1f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.sizeDelta        = new Vector2(0f, TitleH);
        titleRt.anchoredPosition = Vector2.zero;
        var titleTmp           = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text          = "Inventory";
        titleTmp.fontSize      = FontUI;
        titleTmp.fontStyle     = FontStyles.Bold;
        titleTmp.alignment     = TextAlignmentOptions.Center;
        titleTmp.color         = UIColors.TextPrimary;
        titleTmp.raycastTarget = false;

        // Slot grid
        _slots = new SlotEl[Cols * Rows];
        float startX   = -gridW * 0.5f + SlotSize * 0.5f;
        float gridTopY = -TitleH - PadY - SlotSize * 0.5f;

        for (int i = 0; i < Cols * Rows; i++)
        {
            int   col = i % Cols;
            int   row = i / Cols;
            float x   = startX + col * (SlotSize + SlotGap);
            float y   = gridTopY - row * (SlotSize + SlotGap);
            _slots[i] = BuildSlot(_panel.transform, i, x, y);
        }

        _panel.SetActive(false);
    }

    SlotEl BuildSlot(Transform parent, int index, float x, float y)
    {
        // Container — Button and DragHandler, no Image of its own
        var go = new GameObject($"InvSlot{index}",
            typeof(RectTransform), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(SlotSize, SlotSize);
        rt.anchoredPosition = new Vector2(x, y);

        go.GetComponent<Button>().onClick.AddListener(() => { });

        var drag       = go.AddComponent<InventoryDragHandler>();
        drag.IsHotbar  = false;
        drag.SlotIndex = index;

        // CHILD 1: Outline — behind background, expands OutlinePx beyond slot bounds
        var outlineGo = new GameObject("Outline",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineGo.transform.SetParent(go.transform, false);
        var ort           = outlineGo.GetComponent<RectTransform>();
        ort.anchorMin     = Vector2.zero;
        ort.anchorMax     = Vector2.one;
        ort.offsetMin     = new Vector2(-OutlinePx, -OutlinePx);
        ort.offsetMax     = new Vector2( OutlinePx,  OutlinePx);
        var outlineImg           = outlineGo.GetComponent<Image>();
        outlineImg.color         = UIColors.SlotSelected;
        outlineImg.raycastTarget = false;
        outlineGo.SetActive(false);

        // CHILD 2: Background — in front of outline, fills slot exactly
        var bgGo = new GameObject("Background",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        var bgRt       = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg           = bgGo.GetComponent<Image>();
        bgImg.color         = UIColors.SlotEmpty;
        bgImg.raycastTarget = true;

        var main = MakeTMP(go.transform, "Main",
            new Vector2(0f, 0.28f), new Vector2(1f, 1f),
            new Vector2(-4f, 0f), new Vector2(0f, 2f), FontItem);
        main.alignment        = TextAlignmentOptions.Center;
        main.enableAutoSizing = true;
        main.fontSizeMin      = 9f;
        main.fontSizeMax      = FontItem;

        var tag = MakeTMP(go.transform, "Tag",
            new Vector2(0f, 0f), new Vector2(1f, 0.28f),
            new Vector2(-4f, 0f), new Vector2(0f, 2f), FontTag);
        tag.alignment = TextAlignmentOptions.Center;

        return new SlotEl { Outline = outlineImg, Bg = bgImg, Main = main, Tag = tag };
    }

    // ── Toggle / close ────────────────────────────────────────

    public void Toggle()
    {
        if (!_built) return;
        _isOpen = !_isOpen;
        IsOpen  = _isOpen;
        _panel.SetActive(_isOpen);
        if (_isOpen)
        {
            if (PlacementController.Instance != null &&
                PlacementController.Instance.IsPlacing)
                PlacementController.Instance.CancelPlacement();
            Refresh();
        }
    }

    public void CloseIfOpen()
    {
        if (!_isOpen || _panel == null) return;
        _isOpen = false;
        IsOpen  = false;
        _panel.SetActive(false);
    }

    // ── Refresh ───────────────────────────────────────────────

    public void Refresh()
    {
        if (_slots == null) return;
        var selectedSlot = Inventory.Instance.SelectedSlot;

        for (int i = 0; i < _slots.Length; i++)
        {
            var inv = Inventory.Instance.StorageSlots[i];
            var el  = _slots[i];
            bool sel = !inv.IsEmpty && inv == selectedSlot;

            el.Outline.gameObject.SetActive(sel);

            if (inv.IsEmpty)
            {
                el.Bg.color  = UIColors.SlotEmpty;
                el.Main.text = ""; el.Tag.text = "";
                continue;
            }
            switch (inv.Type)
            {
                case InventoryItemType.Seed:
                    el.Bg.color   = UIColors.SlotSeed;
                    el.Main.text  = $"{inv.Crop.cropName}\nx{inv.SeedCount}";
                    el.Main.color = UIColors.TextPrimary;
                    el.Tag.text   = "SEED"; el.Tag.color = UIColors.TagSeed;
                    break;
                case InventoryItemType.Harvest:
                    el.Bg.color   = UIColors.SlotHarvest;
                    el.Main.text  = $"{inv.Crop.cropName}\nx{inv.Harvested.Count}";
                    el.Main.color = UIColors.TextPrimary;
                    el.Tag.text   = "CROP"; el.Tag.color = UIColors.TagHarvest;
                    break;
                case InventoryItemType.Tool:
                    el.Bg.color   = inv.Tool.toolColor;
                    el.Main.text  = inv.Tool.isConsumable
                        ? $"{inv.Tool.toolName}\nx{inv.ToolCount}" : inv.Tool.toolName;
                    el.Main.color = UIColors.TextPrimary;
                    el.Tag.text   = "TOOL"; el.Tag.color = UIColors.TextPrimary;
                    break;
            }
        }
    }

    // ── TMP factory ───────────────────────────────────────────

    TextMeshProUGUI MakeTMP(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 sizeDelta, Vector2 anchoredPos, int fontSize)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin; rt.anchorMax = anchorMax;
        rt.sizeDelta        = sizeDelta; rt.anchoredPosition = anchoredPos;
        var tmp              = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize         = fontSize;
        tmp.color            = UIColors.TextDim;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}