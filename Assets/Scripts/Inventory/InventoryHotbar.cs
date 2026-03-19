using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryHotbar : MonoBehaviour
{
    public static InventoryHotbar Instance;

    private const int   FontBody     = 14;
    private const int   FontLabel    = 10;
    private const float SlotSize     = 60f;
    private const float SlotSpacing  = 5f;
    private const float PanelPadding = 10f;
    private const float BottomPad    = 20f;
    private const float MaxWidthPct  = 0.9f;
    private const float InvBtnSize   = 44f;
    private const float InvBtnGap    = 10f;

    private struct SlotElements
    {
        public Image           Background;
        public TextMeshProUGUI MainLabel;
        public TextMeshProUGUI TypeLabel;
        public Button          Btn;
    }

    private SlotElements[] slotElements;

    private System.Action<CropData>      _onSeedAdded, _onSeedUsed;
    private System.Action<HarvestedCrop> _onCropHarvested, _onCropSold;
    private System.Action<ToolData>      _onToolAdded, _onToolUsed;
    private System.Action                _onShopClosed;

    void Awake()
    {
        Instance         = this;
        _onSeedAdded     = _ => Refresh();
        _onSeedUsed      = _ => Refresh();
        _onCropHarvested = _ => Refresh();
        _onCropSold      = _ => Refresh();
        _onToolAdded     = _ => Refresh();
        _onToolUsed      = _ => Refresh();
        _onShopClosed    = Refresh;

        EventBus.OnSeedAdded     += _onSeedAdded;
        EventBus.OnSeedUsed      += _onSeedUsed;
        EventBus.OnCropHarvested += _onCropHarvested;
        EventBus.OnCropSold      += _onCropSold;
        EventBus.OnToolAdded     += _onToolAdded;
        EventBus.OnToolUsed      += _onToolUsed;
        EventBus.OnShopClosed    += _onShopClosed;
    }

    void OnDestroy()
    {
        EventBus.OnSeedAdded     -= _onSeedAdded;
        EventBus.OnSeedUsed      -= _onSeedUsed;
        EventBus.OnCropHarvested -= _onCropHarvested;
        EventBus.OnCropSold      -= _onCropSold;
        EventBus.OnToolAdded     -= _onToolAdded;
        EventBus.OnToolUsed      -= _onToolUsed;
        EventBus.OnShopClosed    -= _onShopClosed;
    }

    void Start()
    {
        BuildHotbar();
        Refresh();
    }

    void Update()
    {
        if (ShopUI.IsOpen) return;
        HandleNumberKeys();
    }

    void BuildHotbar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        int    count  = Inventory.MaxSlots;

        float totalFixed  = count * SlotSize + (count - 1) * SlotSpacing + PanelPadding * 2f;
        float maxWidth    = Screen.width * MaxWidthPct;
        float scaleFactor = totalFixed > maxWidth ? maxWidth / totalFixed : 1f;
        float slotSz      = SlotSize  * scaleFactor;
        int   bodyFont    = Mathf.Max(9, Mathf.RoundToInt(FontBody  * scaleFactor));
        int   labelFont   = Mathf.Max(7, Mathf.RoundToInt(FontLabel * scaleFactor));

        float panelW = count * slotSz + (count - 1) * SlotSpacing + PanelPadding * 2f;
        float panelH = slotSz + PanelPadding * 2f;

        var panel = new GameObject("InventoryHotbar",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var rt              = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BottomPad);
        rt.sizeDelta        = new Vector2(panelW, panelH);

        var panelImg           = panel.GetComponent<Image>();
        panelImg.color         = UIColors.SlotPanel;
        panelImg.raycastTarget = false;

        slotElements = new SlotElements[count];
        for (int i = 0; i < count; i++)
        {
            float xPos = -(count - 1) * (slotSz + SlotSpacing) * 0.5f
                         + i * (slotSz + SlotSpacing);
            slotElements[i] = BuildSlot(panel.transform, i, xPos, slotSz,
                bodyFont, labelFont);
        }

        // Inventory button — separate sibling, left of hotbar
        float hotbarLeftEdge = -(panelW * 0.5f);
        float invBtnX        = hotbarLeftEdge - InvBtnGap - InvBtnSize * 0.5f;

        var invBtn = new GameObject("InventoryBtn",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        invBtn.transform.SetParent(canvas.transform, false);

        var ibRt              = invBtn.GetComponent<RectTransform>();
        ibRt.anchorMin        = new Vector2(0.5f, 0f);
        ibRt.anchorMax        = new Vector2(0.5f, 0f);
        ibRt.pivot            = new Vector2(0.5f, 0f);
        ibRt.sizeDelta        = new Vector2(InvBtnSize, InvBtnSize);
        ibRt.anchoredPosition = new Vector2(invBtnX, BottomPad);
        invBtn.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.38f, 0.95f);

        var ibLbl = new GameObject("L",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        ibLbl.transform.SetParent(invBtn.transform, false);
        var ibLblRt         = ibLbl.GetComponent<RectTransform>();
        ibLblRt.anchorMin   = Vector2.zero;
        ibLblRt.anchorMax   = Vector2.one;
        ibLblRt.sizeDelta   = Vector2.zero;
        ibLblRt.anchoredPosition = Vector2.zero;
        var ibTmp           = ibLbl.GetComponent<TextMeshProUGUI>();
        ibTmp.text          = "INV";
        ibTmp.fontSize      = 13f;
        ibTmp.fontStyle     = FontStyles.Bold;
        ibTmp.alignment     = TextAlignmentOptions.Center;
        ibTmp.color         = Color.white;
        ibTmp.raycastTarget = false;

        invBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            CancelPlacementIfActive();
            InventoryPanel.Instance?.Toggle();
        });

        InventoryPanel.Instance?.SetHotbarTopY(BottomPad + panelH);
    }

    SlotElements BuildSlot(Transform parent, int index, float xPos,
        float slotSz, int bodyFont, int labelFont)
    {
        var root = new GameObject($"Slot_{index + 1}",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        var srt              = root.GetComponent<RectTransform>();
        srt.anchorMin        = new Vector2(0.5f, 0.5f);
        srt.anchorMax        = new Vector2(0.5f, 0.5f);
        srt.pivot            = new Vector2(0.5f, 0.5f);
        srt.sizeDelta        = new Vector2(slotSz, slotSz);
        srt.anchoredPosition = new Vector2(xPos, 0f);

        var img           = root.GetComponent<Image>();
        img.raycastTarget = true;

        // Drag handler — allows dragging slots to inventory panel and back
        var drag       = root.AddComponent<InventoryDragHandler>();
        drag.IsHotbar  = true;
        drag.SlotIndex = index;

        var main = MakeLabel(root.transform, "M",
            new Vector2(0f, 3f), new Vector2(-4f, -16f), bodyFont);
        main.text  = $"[{index + 1}]";
        main.color = UIColors.TextDim;

        var tag = MakeLabel(root.transform, "T",
            new Vector2(0f, -slotSz * 0.5f + 7f), new Vector2(-4f, 14f), labelFont);
        tag.color = UIColors.TextDim;

        return new SlotElements
        {
            Background = img,
            MainLabel  = main,
            TypeLabel  = tag,
            Btn        = root.GetComponent<Button>()
        };
    }

    public void Refresh()
    {
        if (slotElements == null) return;
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var slot = Inventory.Instance.Slots[i];
            var el   = slotElements[i];
            el.Btn.onClick.RemoveAllListeners();

            if (slot.IsEmpty)                                SetEmpty(el, i);
            else if (slot.Type == InventoryItemType.Seed)    SetSeed(el, slot);
            else if (slot.Type == InventoryItemType.Harvest) SetHarvest(el, slot);
            else if (slot.Type == InventoryItemType.Tool)    SetTool(el, slot);
        }
    }

    void SetEmpty(SlotElements el, int index)
    {
        el.Background.color = UIColors.SlotEmpty;
        el.MainLabel.text   = $"[{index + 1}]";
        el.MainLabel.color  = UIColors.TextDim;
        el.TypeLabel.text   = "";
        el.Btn.onClick.AddListener(() =>
        {
            Inventory.Instance.Deselect();
            Refresh();
        });
    }

    void SetSeed(SlotElements el, InventorySlot slot)
    {
        bool selected       = Inventory.Instance.SelectedSeed == slot.Crop;
        el.Background.color = selected ? UIColors.SlotSelected : UIColors.SlotSeed;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.SeedCount}";
        el.MainLabel.color  = UIColors.TextPrimary;
        el.TypeLabel.text   = "SEED";
        el.TypeLabel.color  = UIColors.TagSeed;
        CropData captured   = slot.Crop;
        el.Btn.onClick.AddListener(() =>
        {
            CancelPlacementIfActive();
            Inventory.Instance.SelectSeed(captured);
            Refresh();
        });
    }

    void SetHarvest(SlotElements el, InventorySlot slot)
    {
        el.Background.color = UIColors.SlotHarvest;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.Harvested.Count}";
        el.MainLabel.color  = UIColors.TextPrimary;
        el.TypeLabel.text   = "CROP";
        el.TypeLabel.color  = UIColors.TagHarvest;
    }

    void SetTool(SlotElements el, InventorySlot slot)
    {
        bool selected       = Inventory.Instance.SelectedSlot == slot;
        el.Background.color = selected ? UIColors.SlotSelected : slot.Tool.toolColor;
        el.MainLabel.text   = slot.Tool.isConsumable
                              ? $"{slot.Tool.toolName}\nx{slot.ToolCount}"
                              : slot.Tool.toolName;
        el.MainLabel.color  = UIColors.TextPrimary;
        el.TypeLabel.text   = "TOOL";
        el.TypeLabel.color  = UIColors.TextPrimary;
        ToolData captured   = slot.Tool;
        el.Btn.onClick.AddListener(() =>
        {
            CancelPlacementIfActive();
            Inventory.Instance.SelectTool(captured);
            Refresh();
        });
    }

    void HandleNumberKeys()
    {
        for (int i = 0; i < Inventory.MaxSlots && i < 9; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
            CancelPlacementIfActive();
            Inventory.Instance.SelectSlot(i);
            Refresh();
        }
    }

    public static void CancelPlacementIfActive()
    {
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing)
            PlacementController.Instance.CancelPlacement();
    }

    TextMeshProUGUI MakeLabel(Transform parent, string name,
        Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize      = fontSize;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = UIColors.TextPrimary;
        tmp.raycastTarget = false;
        return tmp;
    }
}