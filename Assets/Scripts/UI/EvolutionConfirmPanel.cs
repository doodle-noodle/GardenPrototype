using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

// Shown when a plot enters Ready with a valid evolution path.
// Blocks all other farm input while open (same pattern as DialoguePanel / ShopUI).
//
// Scene setup: Add this component to any GameObject. No prefab required — UI is code-generated.
public class EvolutionConfirmPanel : MonoBehaviour
{
    public static EvolutionConfirmPanel Instance { get; private set; }
    public static bool                  IsOpen   { get; private set; }

    // References filled by Open()
    private FarmPlot      _plot;
    private EvolutionPath _path;

    // UI refs
    private GameObject      _panel;
    private TextMeshProUGUI _promptTmp;

    // Layout
    private const float PanelW   = 420f;
    private const float PanelH   = 180f;
    private const float Padding  = 18f;
    private const float BtnW     = 160f;
    private const float BtnH     = 44f;
    private const float BtnGap   = 16f;

    void Awake()
    {
        Instance = this;
        BuildUI();
        _panel.SetActive(false);
    }

    // ── Public entry ──────────────────────────────────────────

    public void Open(FarmPlot plot, EvolutionPath path)
    {
        if (plot == null || path?.characterData == null) return;
        _plot = plot;
        _path = path;

        string cropName = plot.ActiveCrop?.cropName ?? "Plant";
        if (_promptTmp != null)
            _promptTmp.text = $"{cropName} wants to evolve.\nProceed?";

        IsOpen = true;
        _panel.SetActive(true);
    }

    // ── Button handlers ───────────────────────────────────────

    void OnYes()
    {
        IsOpen = false;
        _panel.SetActive(false);

        // ── Cutscene stub ─────────────────────────────────────
        // Full cutscene (3D model → 2D sprite transform animation) will be
        // implemented in a future session and slotted in here.
        //
        // For now: if CharacterData has an evolutionYarnNode defined, run it via
        // DialoguePanel (which shows the Yarn cutscene text and waits for input).
        // After that completes, EvolvePlot is called from OnCutsceneComplete().
        //
        // If no Yarn node is defined, evolve immediately.

        string yarnNode = _path.characterData.evolutionYarnNode;
        if (!string.IsNullOrEmpty(yarnNode) && DialoguePanel.Instance != null)
        {
            // Temporarily override the character so the cutscene Yarn node can run
            // inside the existing DialoguePanel presenter infrastructure.
            // DialoguePanel.OnDialogueCompleteAsync will fire OnCutsceneComplete via event.
            EventBus.OnDialogueComplete += OnCutsceneComplete;
            DialoguePanel.Instance.OpenCutscene(_path.characterData, yarnNode);
        }
        else
        {
            // No cutscene node — evolve directly
            CompleteEvolution();
        }
    }

    void OnCutsceneComplete()
    {
        EventBus.OnDialogueComplete -= OnCutsceneComplete;
        CompleteEvolution();
    }

    void CompleteEvolution()
    {
        if (_plot != null && _path?.characterData != null)
            _plot.EvolvePlot(_path.characterData);
        _plot = null;
        _path = null;
    }

    void OnNo()
    {
        IsOpen = false;
        _panel.SetActive(false);

        string cropName = _plot?.ActiveCrop?.cropName ?? "Plant";
        _plot?.DeclineEvolution();

        // Show "decided not to evolve" message via TutorialConsole (already called in
        // FarmPlot.DeclineEvolution), but also in the dialogue panel for more visibility.
        // If no dialogue is running, just let TutorialConsole carry it.
        TutorialConsole.Log($"{cropName} decided not to evolve.");

        _plot = null;
        _path = null;
    }

    // ── UI construction ───────────────────────────────────────

    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("EvolutionConfirmPanel: no Canvas found in scene.");
            return;
        }

        _panel = new GameObject("EvolutionConfirmPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _panel.transform.SetParent(canvas.transform, false);

        var rt              = _panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(PanelW, PanelH);
        rt.anchoredPosition = Vector2.zero;

        var img           = _panel.GetComponent<Image>();
        img.color         = UIColors.DialoguePanel;
        img.raycastTarget = false;

        // Prompt text
        var promptGo = new GameObject("Prompt",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        promptGo.transform.SetParent(_panel.transform, false);
        var prt              = promptGo.GetComponent<RectTransform>();
        prt.anchorMin        = new Vector2(0f, 1f);
        prt.anchorMax        = new Vector2(1f, 1f);
        prt.pivot            = new Vector2(0.5f, 1f);
        prt.sizeDelta        = new Vector2(-Padding * 2f, 80f);
        prt.anchoredPosition = new Vector2(0f, -Padding);
        _promptTmp               = promptGo.GetComponent<TextMeshProUGUI>();
        _promptTmp.fontSize      = 18f;
        _promptTmp.alignment     = TextAlignmentOptions.Center;
        _promptTmp.color         = UIColors.TextPrimary;
        _promptTmp.raycastTarget = false;
        _promptTmp.textWrappingMode = TextWrappingModes.Normal;

        // Yes button
        float btnRowY = -PanelH * 0.5f + Padding + BtnH * 0.5f;
        MakeButton(_panel.transform, "Yes",
            new Vector2(-BtnW * 0.5f - BtnGap * 0.5f, btnRowY),
            new Vector2(BtnW, BtnH),
            UIColors.DialogueOption,
            OnYes);

        // No button
        MakeButton(_panel.transform, "No (cancel evolution)",
            new Vector2(BtnW * 0.5f + BtnGap * 0.5f, btnRowY),
            new Vector2(BtnW + 40f, BtnH),
            UIColors.PanelDark,
            OnNo);
    }

    void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var lblGo = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(go.transform, false);
        var lrt              = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin        = Vector2.zero;
        lrt.anchorMax        = Vector2.one;
        lrt.sizeDelta        = new Vector2(-8f, 0f);
        lrt.anchoredPosition = Vector2.zero;
        var tmp              = lblGo.GetComponent<TextMeshProUGUI>();
        tmp.text             = label;
        tmp.fontSize         = 14f;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = UIColors.TextPrimary;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
