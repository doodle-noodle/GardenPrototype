using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryHotbar : MonoBehaviour
{
    public static InventoryHotbar Instance;

    // ── Layout ────────────────────────────────────────────────
    private const int   FontBody     = 20;
    private const int   FontLabel    = 14;
    private const float SlotW        = 80f;
    private const float SlotH        = 72f;
    private const float SlotSpacing  = 6f;
    private const float PanelPadding = 10f;
    private const float BottomPad    = 20f;

    // ── Slot sub-elements ─────────────────────────────────────
    private struct SlotElements
    {
        public Image            Background;
        public TextMeshProUGUI  MainLabel;
        public TextMeshProUGUI  TypeLabel;
        public Button           Btn;
    }

    private SlotElements[] slotElements;

    // ── Stored event handlers — prevents lambda memory leaks ──
    private System.Action<CropData>      _onSeedAdded;
    private System.Action<CropData>      _onSeedUsed;
    private System.Action<HarvestedCrop> _onCropHarvested;
    private System.Action<HarvestedCrop> _onCropSold;
    private System.Action                _onShopClosed;

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        _onSeedAdded     = _ => Refresh();
        _onSeedUsed      = _ => Refresh();
        _onCropHarvested = _ => Refresh();
        _onCropSold      = _ => Refresh();
        _onShopClosed    = Refresh;

        EventBus.OnSeedAdded     += _onSeedAdded;
        EventBus.OnSeedUsed      += _onSeedUsed;
        EventBus.OnCropHarvested += _onCropHarvested;
        EventBus.OnCropSold      += _onCropSold;
        EventBus.OnShopClosed    += _onShopClosed;
    }

    void OnDestroy()
    {
        EventBus.OnSeedAdded     -= _onSeedAdded;
        EventBus.OnSeedUsed      -= _onSeedUsed;
        EventBus.OnCropHarvested -= _onCropHarvested;
        EventBus.OnCropSold      -= _onCropSold;
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

    // ── Build ─────────────────────────────────────────────────

    void BuildHotbar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        int    count  = Inventory.MaxSlots;
        float  panelW = count * SlotW + (count - 1) * SlotSpacing + PanelPadding * 2f;
        float  panelH = SlotH + PanelPadding * 2f;

        GameObject panel = new GameObject("InventoryHotbar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BottomPad);
        rt.sizeDelta        = new Vector2(panelW, panelH);
        panel.GetComponent<Image>().color = UIColors.SlotPanel;

        slotElements = new SlotElements[count];

        for (int i = 0; i < count; i++)
        {
            float xPos = -((count - 1) * (SlotW + SlotSpacing) * 0.5f)
                         + i * (SlotW + SlotSpacing);
            slotElements[i] = BuildSlot(panel.transform, i, xPos);
        }
    }

    SlotElements BuildSlot(Transform parent, int index, float xPos)
    {
        GameObject root = new GameObject($"Slot_{index + 1}", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        var srt = root.GetComponent<RectTransform>();
        srt.anchorMin        = new Vector2(0.5f, 0.5f);
        srt.anchorMax        = new Vector2(0.5f, 0.5f);
        srt.pivot            = new Vector2(0.5f, 0.5f);
        srt.sizeDelta        = new Vector2(SlotW, SlotH);
        srt.anchoredPosition = new Vector2(xPos, 0f);

        var mainLabel = MakeLabel(root.transform, "MainLabel",
            new Vector2(0f, 4f), new Vector2(-4f, -20f),
            FontBody, TextAlignmentOptions.Center);
        mainLabel.text  = $"[{index + 1}]";
        mainLabel.color = UIColors.TextDim;

        var typeLabel = MakeLabel(root.transform, "TypeLabel",
            new Vector2(0f, -SlotH * 0.5f + 10f), new Vector2(-4f, 18f),
            FontLabel, TextAlignmentOptions.Center);
        typeLabel.color = UIColors.TextDim;

        return new SlotElements
        {
            Background = root.GetComponent<Image>(),
            MainLabel  = mainLabel,
            TypeLabel  = typeLabel,
            Btn        = root.GetComponent<Button>()
        };
    }

    // ── Refresh ───────────────────────────────────────────────

    public void Refresh()
    {
        if (slotElements == null) return;

        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            InventorySlot slot = Inventory.Instance.Slots[i];
            SlotElements  el   = slotElements[i];

            el.Btn.onClick.RemoveAllListeners();

            if (slot.IsEmpty)
                SetEmptySlot(el, i);
            else if (slot.Type == InventoryItemType.Seed)
                SetSeedSlot(el, slot, Inventory.Instance.SelectedSeed == slot.Crop);
            else if (slot.Type == InventoryItemType.Harvest)
                SetHarvestSlot(el, slot);
        }
    }

    void SetEmptySlot(SlotElements el, int index)
    {
        el.Background.color = UIColors.SlotEmpty;
        el.MainLabel.text   = $"[{index + 1}]";
        el.MainLabel.color  = UIColors.TextDim;
        el.TypeLabel.text   = "";
    }

    void SetSeedSlot(SlotElements el, InventorySlot slot, bool isSelected)
    {
        el.Background.color = isSelected ? UIColors.SlotSelected : UIColors.SlotSeed;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.SeedCount}";
        el.MainLabel.color  = UIColors.TextPrimary;
        el.TypeLabel.text   = "SEED";
        el.TypeLabel.color  = UIColors.TagSeed;

        CropData captured = slot.Crop;
        el.Btn.onClick.AddListener(() =>
        {
            Inventory.Instance.SelectSeed(captured);
            Refresh();
        });
    }

    void SetHarvestSlot(SlotElements el, InventorySlot slot)
    {
        el.Background.color = UIColors.SlotHarvest;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.Harvested.Count}";
        el.MainLabel.color  = UIColors.TextPrimary;
        el.TypeLabel.text   = "CROP";
        el.TypeLabel.color  = UIColors.TagHarvest;
    }

    // ── Number keys ───────────────────────────────────────────

    void HandleNumberKeys()
    {
        for (int i = 0; i < Inventory.MaxSlots && i < 9; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            InventorySlot slot = Inventory.Instance.Slots[i];
            if (slot.Type == InventoryItemType.Seed)
            {
                Inventory.Instance.SelectSeed(slot.Crop);
                Refresh();
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    TextMeshProUGUI MakeLabel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.color     = UIColors.TextPrimary;

        return tmp;
    }
}