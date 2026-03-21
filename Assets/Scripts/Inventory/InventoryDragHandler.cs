using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Attach to every inventory slot (hotbar + storage panel).
// CanvasGroup.blocksRaycasts = false on proxy is critical —
// without it the dragged object intercepts the OnDrop on the target.
public class InventoryDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [HideInInspector] public bool IsHotbar  = true;
    [HideInInspector] public int  SlotIndex = 0;

    private static InventoryDragHandler _dragging;
    private static GameObject           _proxy;
    private static Canvas               _canvas;

    private Image _bg;
    private Color _originalColor;

    void Awake()
    {
        _bg = GetComponent<Image>();
        if (_bg != null) _originalColor = _bg.color;
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
    }

    // ── Drag ──────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        var slot = GetMySlot();
        if (slot == null || slot.IsEmpty) return;

        _dragging = this;
        CreateProxy(e.position, slot);

        if (_bg != null)
        {
            var c = _originalColor;
            c.a = 0.3f;
            _bg.color = c;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (_proxy == null) return;
        _proxy.GetComponent<RectTransform>().anchoredPosition = ToCanvas(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        DestroyProxy();
        if (_bg != null) _bg.color = _originalColor;
        _dragging = null;
        InventoryHotbar.Instance?.Refresh();
        InventoryPanel.Instance?.Refresh();
    }

    public void OnDrop(PointerEventData e)
    {
        if (_dragging == null || _dragging == this) return;

        var from = GetSlot(_dragging.IsHotbar, _dragging.SlotIndex);
        var to   = GetMySlot();
        if (from == null || to == null) return;

        InventorySlot.Swap(from, to);
        Inventory.Instance.ValidateSelection();
        InventoryHotbar.Instance?.Refresh();
        InventoryPanel.Instance?.Refresh();
    }

    // ── Helpers ───────────────────────────────────────────────

    InventorySlot GetMySlot() => GetSlot(IsHotbar, SlotIndex);

    static InventorySlot GetSlot(bool hotbar, int index)
    {
        if (hotbar)
            return index >= 0 && index < Inventory.MaxSlots
                ? Inventory.Instance.Slots[index] : null;
        return index >= 0 && index < Inventory.StorageSlotCount
            ? Inventory.Instance.StorageSlots[index] : null;
    }

    void CreateProxy(Vector2 screenPos, InventorySlot slot)
    {
        if (_canvas == null) return;
        DestroyProxy();

        _proxy = new GameObject("DragProxy",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(CanvasGroup));
        _proxy.transform.SetParent(_canvas.transform, false);
        _proxy.transform.SetAsLastSibling(); // renders on top within main canvas

        // CRITICAL: blocksRaycasts = false so OnDrop fires on targets beneath proxy
        var cg              = _proxy.GetComponent<CanvasGroup>();
        cg.blocksRaycasts   = false;
        cg.interactable     = false;
        cg.alpha            = 0.85f;

        _proxy.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 0.85f);

        var rt              = _proxy.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(60f, 60f);
        rt.anchoredPosition = ToCanvas(screenPos);

        var lbl = new GameObject("L",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lbl.transform.SetParent(_proxy.transform, false);

        var lrt         = lbl.GetComponent<RectTransform>();
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.sizeDelta   = new Vector2(-4f, -4f);
        lrt.anchoredPosition = Vector2.zero;

        var tmp                    = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text                   = slot.DisplayName;
        tmp.fontSize               = 11f;
        tmp.alignment              = TextAlignmentOptions.Center;
        tmp.color                  = Color.white;
        tmp.textWrappingMode       = TextWrappingModes.Normal;
        tmp.raycastTarget          = false;
    }

    static void DestroyProxy()
    {
        if (_proxy != null) { Object.Destroy(_proxy); _proxy = null; }
    }

    static Vector2 ToCanvas(Vector2 screenPos)
    {
        if (_canvas == null) return screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), screenPos, null, out var local);
        return local;
    }
}