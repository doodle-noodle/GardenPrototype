using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryHotbar : MonoBehaviour
{
    public static InventoryHotbar Instance;

    // ── Layout ────────────────────────────────────────────────
    private const int   FontBody       = 20;
    private const int   FontLabel      = 14;
    private const float SlotW          = 80f;
    private const float SlotH          = 72f;
    private const float SlotSpacing    = 6f;
    private const float PanelPadding   = 10f;
    private const float BottomPad      = 20f;

    // ── Colors ────────────────────────────────────────────────
    private static readonly Color ColorEmpty    = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color ColorSeed     = new Color(0.20f, 0.20f, 0.20f, 1f);
    private static readonly Color ColorHarvest  = new Color(0.20f, 0.18f, 0.10f, 1f);
    private static readonly Color ColorSelected = new Color(0.20f, 0.55f, 0.20f, 1f);
    private static readonly Color ColorPanel    = new Color(0.08f, 0.08f, 0.08f, 0.90f);
    private static readonly Color ColorDimText  = new Color(1f,   1f,   1f,   0.25f);
    private static readonly Color ColorSeedTag  = new Color(0.30f, 0.80f, 0.30f, 1f);
    private static readonly Color ColorHarvTag  = new Color(0.90f, 0.70f, 0.10f, 1f);

    // ── Slot sub-elements ─────────────────────────────────────
    private struct SlotElements
    {
        public GameObject    Root;
        public Image         Background;
        public TextMeshProUGUI MainLabel;
        public TextMeshProUGUI TypeLabel;
        public Button        Btn;
    }

    private SlotElements[] slotElements;

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        EventBus.OnSeedAdded     += _ => Refresh();
        EventBus.OnSeedUsed      += _ => Refresh();
        EventBus.OnCropHarvested += _ => Refresh();
        EventBus.OnCropSold      += _ => Refresh();
        EventBus.OnShopClosed    += Refresh;
    }

    void OnDestroy()
    {
        EventBus.OnSeedAdded     -= _ => Refresh();
        EventBus.OnSeedUsed      -= _ => Refresh();
        EventBus.OnCropHarvested -= _ => Refresh();
        EventBus.OnCropSold      -= _ => Refresh();
        EventBus.OnShopClosed    -= Refresh;
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
        Canvas canvas  = FindFirstObjectByType<Canvas>();
        int    count   = Inventory.MaxSlots;
        float  panelW  = count * SlotW + (count - 1) * SlotSpacing + PanelPadding * 2f;
        float  panelH  = SlotH + PanelPadding * 2f;

        GameObject panel = new GameObject("InventoryHotbar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BottomPad);
        rt.sizeDelta        = new Vector2(panelW, panelH);
        panel.GetComponent<Image>().color = ColorPanel;

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
        // Root
        GameObject root = new GameObject($"Slot_{index + 1}", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        var srt = root.GetComponent<RectTransform>();
        srt.anchorMin        = new Vector2(0.5f, 0.5f);
        srt.anchorMax        = new Vector2(0.5f, 0.5f);
        srt.pivot            = new Vector2(0.5f, 0.5f);
        srt.sizeDelta        = new Vector2(SlotW, SlotH);
        srt.anchoredPosition = new Vector2(xPos, 0f);

        // Main label (crop name + count)
        var mainLabel = MakeLabel(root.transform, "MainLabel",
            new Vector2(0f, 4f), new Vector2(-4f, -20f),
            FontBody, TextAlignmentOptions.Center);

        mainLabel.text  = $"[{index + 1}]";
        mainLabel.color = ColorDimText;

        // Type label (SEED / CROP) at bottom of slot
        var typeLabel = MakeLabel(root.transform, "TypeLabel",
            new Vector2(0f, -SlotH * 0.5f + 10f), new Vector2(-4f, 18f),
            FontLabel, TextAlignmentOptions.Center);

        typeLabel.color = ColorDimText;

        return new SlotElements
        {
            Root       = root,
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
            {
                SetEmptySlot(el, i);
            }
            else if (slot.Type == InventoryItemType.Seed)
            {
                bool isSelected = Inventory.Instance.SelectedSeed == slot.Crop;
                SetSeedSlot(el, slot, isSelected);
            }
            else if (slot.Type == InventoryItemType.Harvest)
            {
                SetHarvestSlot(el, slot);
            }
        }
    }

    void SetEmptySlot(SlotElements el, int index)
    {
        el.Background.color = ColorEmpty;
        el.MainLabel.text   = $"[{index + 1}]";
        el.MainLabel.color  = ColorDimText;
        el.TypeLabel.text   = "";
    }

    void SetSeedSlot(SlotElements el, InventorySlot slot, bool isSelected)
    {
        el.Background.color = isSelected ? ColorSelected : ColorSeed;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.SeedCount}";
        el.MainLabel.color  = Color.white;
        el.TypeLabel.text   = "SEED";
        el.TypeLabel.color  = ColorSeedTag;

        CropData captured = slot.Crop;
        el.Btn.onClick.AddListener(() =>
        {
            Inventory.Instance.SelectSeed(captured);
            Refresh();
        });
    }

    void SetHarvestSlot(SlotElements el, InventorySlot slot)
    {
        el.Background.color = ColorHarvest;
        el.MainLabel.text   = $"{slot.Crop.cropName}\nx{slot.Harvested.Count}";
        el.MainLabel.color  = Color.white;
        el.TypeLabel.text   = "CROP";
        el.TypeLabel.color  = ColorHarvTag;
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
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize,
        TextAlignmentOptions alignment)
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
        tmp.color     = Color.white;

        return tmp;
    }
}