using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPanel : MonoBehaviour
{
    public static InventoryPanel Instance;
    public static bool IsOpen { get; private set; }

    private const int   Cols      = 10;
    private const int   Rows      = 5;
    private const float SlotSize  = 60f;
    private const float SlotGap   = 4f;
    private const float Padding   = 10f;
    private const float HeaderH   = 28f;
    private const float PanelGap  = 6f;
    private const int   FontBody  = 13;
    private const int   FontLabel = 9;

    private GameObject _panel;
    private SlotEl[]   _slots;
    private bool       _isOpen;
    private float      _hotbarTopY;
    private bool       _built;

    private struct SlotEl
    {
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
        if (_built) return;
        _built = true;
        BuildPanel();
    }

    void BuildPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        float gridW  = Cols * SlotSize + (Cols - 1) * SlotGap;
        float gridH  = Rows * SlotSize + (Rows - 1) * SlotGap;
        float panelW = gridW + Padding * 2f;
        float panelH = gridH + Padding * 2f + HeaderH;

        _panel = new GameObject("InventoryStoragePanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _panel.transform.SetParent(canvas.transform, false);

        var panelRt              = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);
        panelRt.anchoredPosition = new Vector2(0f, _hotbarTopY + PanelGap);

        var panelImg           = _panel.GetComponent<Image>();
        panelImg.color         = new Color(0.1f, 0.1f, 0.14f, 0.97f);
        panelImg.raycastTarget = false;

        // Header
        var header = new GameObject("Header",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        header.transform.SetParent(_panel.transform, false);
        var hrt              = header.GetComponent<RectTransform>();
        hrt.anchorMin        = new Vector2(0f, 1f);
        hrt.anchorMax        = new Vector2(1f, 1f);
        hrt.pivot            = new Vector2(0.5f, 1f);
        hrt.sizeDelta        = new Vector2(0f, HeaderH);
        hrt.anchoredPosition = Vector2.zero;
        var hTmp             = header.GetComponent<TextMeshProUGUI>();
        hTmp.text            = "Inventory";
        hTmp.fontSize        = 15f;
        hTmp.fontStyle       = FontStyles.Bold;
        hTmp.alignment       = TextAlignmentOptions.Center;
        hTmp.color           = new Color(0.9f, 0.9f, 0.9f, 1f);
        hTmp.raycastTarget   = false;

        // Slot grid
        int   total  = Cols * Rows;
        _slots       = new SlotEl[total];
        float startX = -(gridW * 0.5f) + SlotSize * 0.5f;
        float startY = -(HeaderH + Padding + SlotSize * 0.5f);

        for (int i = 0; i < total; i++)
        {
            int   col = i % Cols;
            int   row = i / Cols;
            float x   = startX + col * (SlotSize + SlotGap);
            float y   = startY - row * (SlotSize + SlotGap);
            _slots[i] = BuildSlot(_panel.transform, i, x, y);
        }

        _panel.SetActive(false);
    }

    SlotEl BuildSlot(Transform parent, int index, float x, float y)
    {
        var go = new GameObject($"S{index}",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(SlotSize, SlotSize);
        rt.anchoredPosition = new Vector2(x, y);

        var img           = go.GetComponent<Image>();
        img.color         = UIColors.SlotEmpty;
        img.raycastTarget = true;

        var drag       = go.AddComponent<InventoryDragHandler>();
        drag.IsHotbar  = false;
        drag.SlotIndex = index;

        var main = MakeTMP(go.transform, "M",
            new Vector2(0f, 3f), new Vector2(-4f, -16f), FontBody);
        var tag = MakeTMP(go.transform, "T",
            new Vector2(0f, -SlotSize * 0.5f + 7f), new Vector2(-4f, 13f), FontLabel);

        return new SlotEl { Bg = img, Main = main, Tag = tag };
    }

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

    public void Refresh()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = Inventory.Instance.StorageSlots[i];
            var el   = _slots[i];

            if (slot.IsEmpty)
            {
                el.Bg.color  = UIColors.SlotEmpty;
                el.Main.text = ""; el.Tag.text = "";
            }
            else if (slot.Type == InventoryItemType.Seed)
            {
                el.Bg.color   = UIColors.SlotSeed;
                el.Main.text  = $"{slot.Crop.cropName}\nx{slot.SeedCount}";
                el.Main.color = UIColors.TextPrimary;
                el.Tag.text   = "SEED"; el.Tag.color = UIColors.TagSeed;
            }
            else if (slot.Type == InventoryItemType.Harvest)
            {
                el.Bg.color   = UIColors.SlotHarvest;
                el.Main.text  = $"{slot.Crop.cropName}\nx{slot.Harvested.Count}";
                el.Main.color = UIColors.TextPrimary;
                el.Tag.text   = "CROP"; el.Tag.color = UIColors.TagHarvest;
            }
            else if (slot.Type == InventoryItemType.Tool)
            {
                el.Bg.color   = slot.Tool.toolColor;
                el.Main.text  = slot.Tool.isConsumable
                                ? $"{slot.Tool.toolName}\nx{slot.ToolCount}"
                                : slot.Tool.toolName;
                el.Main.color = UIColors.TextPrimary;
                el.Tag.text   = "TOOL"; el.Tag.color = UIColors.TextPrimary;
            }
        }
    }

    TextMeshProUGUI MakeTMP(Transform parent, string name,
        Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var tmp               = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize          = fontSize;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.color             = UIColors.TextDim;
        tmp.textWrappingMode  = TextWrappingModes.Normal; // replaces obsolete enableWordWrapping
        tmp.raycastTarget     = false;
        return tmp;
    }
}
