using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Renders the hotbar strip with background bar, slot number labels, and the INV button.
// All three elements are Canvas siblings — none are children of each other.
//
// Selected slot → thin green border (outline Image added BEFORE background Image as a child,
//                  so it renders behind and peeks out around the edges)
// Click occupied slot  → always equip (no toggle-off)
// Click empty slot     → deselect (empty hands)
// Press number key 1-0 → same logic via Inventory.SelectSlot
public class InventoryHotbar : MonoBehaviour
{
    public static InventoryHotbar Instance;

    // ── Layout ──────────────────────────────────────────────────
    private const int   SlotCount   = Inventory.MaxSlots;
    private const float SlotSize    = 70f;
    private const float SlotGap     = 6f;
    private const float BarPadX     = 8f;
    private const float BarPadY     = 8f;
    private const float BarBottomY  = 10f;
    private const float InvBtnSize  = 45f;
    private const float InvBtnGap   = 8f;
    private const float OutlinePx   = 3f;  // how far the outline peeks out from behind the background

    // ── Unified font sizes ───────────────────────────────────────
    private const int FontUI   = 16;
    private const int FontItem = 18;
    private const int FontTag  = 13;

    private struct SlotEl
    {
        public RectTransform   Rt;
        public Image           Outline;     // green Image added first → renders behind Bg
        public Image           Bg;          // background Image added second → renders in front
        public TextMeshProUGUI Main;
        public TextMeshProUGUI Tag;
        public TextMeshProUGUI Num;
    }

    private SlotEl[]   _slots;
    private GameObject _bar;
    private float      _barTopY;
    private bool       _built;

    void Awake() { Instance = this; }

    void Start()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("InventoryHotbar: no Canvas in scene."); return; }

        BuildBar(canvas.transform);
        BuildInvButton(canvas.transform);
        BuildSlots(canvas.transform);

        _built = true;
        Refresh();

        InventoryPanel.Instance?.SetHotbarTopY(_barTopY);

        EventBus.OnSeedAdded     += _ => Refresh();
        EventBus.OnSeedUsed      += _ => Refresh();
        EventBus.OnCropHarvested += _ => Refresh();
        EventBus.OnToolAdded     += _ => Refresh();
        EventBus.OnToolUsed      += _ => Refresh();
    }

    // ── Background bar ───────────────────────────────────────────

    void BuildBar(Transform canvasParent)
    {
        float totalSlotsW = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
        float barW        = totalSlotsW + BarPadX * 2f;
        float barH        = SlotSize    + BarPadY * 2f;

        _bar = new GameObject("HotbarBar",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _bar.transform.SetParent(canvasParent, false);

        var rt              = _bar.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(barW, barH);
        rt.anchoredPosition = new Vector2(0f, BarBottomY);

        var c             = UIColors.SlotEmpty;
        var img           = _bar.GetComponent<Image>();
        img.color         = new Color(c.r, c.g, c.b, 0.90f);
        img.raycastTarget = false;

        _barTopY = BarBottomY + barH;
    }

    // ── INV button ───────────────────────────────────────────────

    void BuildInvButton(Transform canvasParent)
    {
        float barW = SlotCount * SlotSize + (SlotCount - 1) * SlotGap + BarPadX * 2f;

        var go = new GameObject("InvButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(canvasParent, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.sizeDelta        = new Vector2(InvBtnSize, InvBtnSize);
        rt.anchoredPosition = new Vector2(-barW * 0.5f - InvBtnGap, BarBottomY);

        var c             = UIColors.SlotEmpty;
        go.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 0.90f);
        go.GetComponent<Button>().onClick.AddListener(() => InventoryPanel.Instance?.Toggle());

        var lbl = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lbl.transform.SetParent(go.transform, false);
        var lrt              = lbl.GetComponent<RectTransform>();
        lrt.anchorMin        = Vector2.zero;
        lrt.anchorMax        = Vector2.one;
        lrt.offsetMin        = Vector2.zero;
        lrt.offsetMax        = Vector2.zero;
        var tmp              = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text             = "INV";
        tmp.fontSize         = FontUI;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = UIColors.TextPrimary;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // ── Slot GameObjects ─────────────────────────────────────────

    void BuildSlots(Transform canvasParent)
    {
        _slots = new SlotEl[SlotCount];
        float totalSlotsW = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
        float startX      = -totalSlotsW * 0.5f + SlotSize * 0.5f;
        float slotBottomY = BarBottomY + BarPadY;

        for (int i = 0; i < SlotCount; i++)
            _slots[i] = BuildSlot(canvasParent, i, startX + i * (SlotSize + SlotGap), slotBottomY);
    }

    SlotEl BuildSlot(Transform canvasParent, int index, float x, float bottomY)
    {
        // Slot container — has Button and DragHandler but NO Image of its own.
        // Children provide all visuals. This allows children to render in front of each other
        // in insertion order (first child = behind, last child = in front).
        var go = new GameObject($"HotbarSlot{index}",
            typeof(RectTransform), typeof(Button));
        go.transform.SetParent(canvasParent, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(SlotSize, SlotSize);
        rt.anchoredPosition = new Vector2(x, bottomY);

        int captured = index;
        go.GetComponent<Button>().onClick.AddListener(() => { SelectSlot(captured); Refresh(); });

        var drag       = go.AddComponent<InventoryDragHandler>();
        drag.IsHotbar  = true;
        drag.SlotIndex = index;

        // ── CHILD 1: Outline Image — added FIRST so it renders BEHIND the background.
        // Its rect is OutlinePx larger on every side, so it peeks out as a border.
        // Hidden by default; shown when this slot is selected.
        var outlineGo = new GameObject("Outline",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineGo.transform.SetParent(go.transform, false);
        var ort           = outlineGo.GetComponent<RectTransform>();
        ort.anchorMin     = Vector2.zero;
        ort.anchorMax     = Vector2.one;
        ort.offsetMin     = new Vector2(-OutlinePx, -OutlinePx);  // expand beyond parent bounds
        ort.offsetMax     = new Vector2( OutlinePx,  OutlinePx);
        var outlineImg           = outlineGo.GetComponent<Image>();
        outlineImg.color         = UIColors.SlotSelected;
        outlineImg.raycastTarget = false;
        outlineGo.SetActive(false);

        // ── CHILD 2: Background Image — added SECOND so it renders IN FRONT of the outline.
        // It fills the slot exactly, covering the outline's centre and leaving only its
        // outer edges visible as a border.
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
        bgImg.raycastTarget = true;  // this child receives clicks, which bubble up to the Button

        // ── Slot number — visible on empty slots only
        string numLabel = index < 9 ? (index + 1).ToString() : "0";
        var    numTmp   = MakeTMP(go.transform, "Num",
                              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontUI);
        numTmp.text      = numLabel;
        numTmp.alignment = TextAlignmentOptions.Center;
        numTmp.color     = new Color(1f, 1f, 1f, 0.28f);

        // ── Item name / count
        var mainTmp = MakeTMP(go.transform, "Main",
            new Vector2(0f, 0.28f), new Vector2(1f, 1f),
            new Vector2(-4f, 0f), new Vector2(0f, 2f), FontItem);
        mainTmp.alignment        = TextAlignmentOptions.Center;
        mainTmp.enableAutoSizing = true;
        mainTmp.fontSizeMin      = 9f;
        mainTmp.fontSizeMax      = FontItem;

        // ── Type tag
        var tagTmp = MakeTMP(go.transform, "Tag",
            new Vector2(0f, 0f), new Vector2(1f, 0.28f),
            new Vector2(-4f, 0f), new Vector2(0f, 2f), FontTag);
        tagTmp.alignment = TextAlignmentOptions.Center;

        return new SlotEl
        {
            Rt      = rt,
            Outline = outlineImg,
            Bg      = bgImg,
            Main    = mainTmp,
            Tag     = tagTmp,
            Num     = numTmp
        };
    }

    // ── Keyboard input ───────────────────────────────────────────

    void Update()
    {
        if (DialoguePanel.IsOpen || EvolutionConfirmPanel.IsOpen) return;
        for (int i = 0; i < SlotCount; i++)
        {
            KeyCode key = i < 9 ? (KeyCode)((int)KeyCode.Alpha1 + i) : KeyCode.Alpha0;
            if (Input.GetKeyDown(key)) { SelectSlot(i); break; }
        }
    }

    // ── Selection ────────────────────────────────────────────────

    void SelectSlot(int index)
    {
        Inventory.Instance.SelectSlot(index);
        Refresh();
    }

    // ── Refresh ──────────────────────────────────────────────────

    public void Refresh()
    {
        if (!_built || _slots == null) return;
        var selectedSlot = Inventory.Instance.SelectedSlot;
        for (int i = 0; i < SlotCount; i++)
        {
            var inv = Inventory.Instance.Slots[i];
            var el  = _slots[i];
            bool sel = !inv.IsEmpty && inv == selectedSlot;
            RefreshSlotEl(el, inv, sel);
        }
    }

    void RefreshSlotEl(SlotEl el, InventorySlot inv, bool selected)
    {
        // Outline peeks out behind the background when selected
        el.Outline.gameObject.SetActive(selected);

        if (inv.IsEmpty)
        {
            el.Bg.color  = UIColors.SlotEmpty;
            el.Main.text = ""; el.Tag.text = "";
            el.Num.color = new Color(1f, 1f, 1f, 0.28f);
            return;
        }

        el.Num.color = new Color(0f, 0f, 0f, 0f); // hidden when item present

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

    // ── Static helper ────────────────────────────────────────────

    public static void CancelPlacementIfActive()
    {
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing)
            PlacementController.Instance.CancelPlacement();
    }

    // ── TMP factory ──────────────────────────────────────────────

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