#nullable enable
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

// Custom Dialogue Presenter for Yarn Spinner 3.1.
// Subclasses DialoguePresenterBase — add this component to any scene GameObject,
// then drag it into the DialogueRunner's "Dialogue Presenters" list in the Inspector.
// Disable or remove the default LinePresenter and OptionsPresenter — this replaces both.
//
// UI rules observed:
//   — All Image backgrounds: raycastTarget = false
//   — All TextMeshProUGUI:   raycastTarget = false
//   — Button Image components keep raycastTarget = true (Unity default)
public class DialoguePanel : DialoguePresenterBase
{
    public static DialoguePanel? Instance { get; private set; }
    public static bool           IsOpen   { get; private set; }

    [Header("Yarn Spinner")]
    [Tooltip("The scene's DialogueRunner. Auto-found at Start if blank.")]
    public DialogueRunner? dialogueRunner;

    // ── Layout constants ──────────────────────────────────────
    private const float PanelW       = 740f;
    private const float PanelH       = 300f;
    private const float PortraitW    = 180f;
    private const float PortraitH    = 240f;
    private const float Padding      = 14f;
    private const float NameH        = 32f;
    private const float TextH        = 110f;
    private const float OptionH      = 44f;
    private const float OptionGap    = 6f;
    private const float ContinueBtnH = 36f;
    private const float ContinueBtnW = 130f;
    private const int   FontName     = 18;
    private const int   FontText     = 16;
    private const int   FontOption   = 15;
    private const int   FontContinue = 13;
    private const int   MaxOptions   = 4;

    // ── Runtime UI refs ───────────────────────────────────────
    private GameObject?      _panel;
    private Image?           _portraitImage;
    private TextMeshProUGUI? _speakerNameTmp;
    private TextMeshProUGUI? _dialogueTextTmp;
    private GameObject?      _continueBtn;
    private GameObject?      _optionsContainer;
    private GameObject[]     _optionButtons = new GameObject[MaxOptions];

    // ── Dialogue state ────────────────────────────────────────
    private CharacterData?  _currentCharacter;
    private bool            _advanceRequested = false;
    private DialogueOption? _selectedOption   = null;
    private bool            _typewriterDone   = false;

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        BuildUI();
        _panel?.SetActive(false);
    }

    // ── Public entry point ────────────────────────────────────

    public void Open(CharacterData character)
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("DialoguePanel: no DialogueRunner assigned or found in scene.");
            return;
        }
        if (string.IsNullOrEmpty(character.yarnStartNode))
        {
            Debug.LogError($"DialoguePanel: {character.characterName} has no yarnStartNode set.");
            return;
        }

        _currentCharacter = character;

        if (RelationshipManager.Instance != null)
            RelationshipManager.Instance.CurrentCharacter = character;

        if (_portraitImage != null && character.portraitSprite != null)
            _portraitImage.sprite = character.portraitSprite;

        dialogueRunner.StartDialogue(character.yarnStartNode);
    }

    // ── DialoguePresenterBase overrides ───────────────────────

    public override YarnTask OnDialogueStartedAsync()
    {
        IsOpen = true;
        _panel?.SetActive(true);
        SetContinueVisible(false);
        SetOptionsVisible(false);
        AudioManager.Play(SoundEvent.DialogueOpen);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        IsOpen = false;
        _panel?.SetActive(false);
        _advanceRequested = false;
        _selectedOption   = null;
        _typewriterDone   = false;
        if (RelationshipManager.Instance != null)
            RelationshipManager.Instance.CurrentCharacter = null;
        AudioManager.Play(SoundEvent.DialogueClose);
        return YarnTask.CompletedTask;
    }

    // Displays a line with typewriter effect, waits for Continue or cancellation.
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = !string.IsNullOrEmpty(line.CharacterName)
            ? line.CharacterName
            : _currentCharacter?.characterName ?? "";

        if (_speakerNameTmp != null) _speakerNameTmp.text = speaker;

        UpdatePortrait(talking: true);

        _typewriterDone   = false;
        _advanceRequested = false;
        StartCoroutine(TypewriterCoroutine(line.TextWithoutCharacterName.Text, token));

        // IsNextContentRequested replaces obsolete IsNextLineRequested (YS 3.1)
        // IsImmediateCancellationRequested was removed — IsNextContentRequested covers both cases
        while (!_typewriterDone && !token.IsNextContentRequested)
            await YarnTask.Yield();

        if (_dialogueTextTmp != null)
            _dialogueTextTmp.text = line.TextWithoutCharacterName.Text;
        UpdatePortrait(talking: false);

        SetContinueVisible(true);
        _advanceRequested = false;

        while (!_advanceRequested && !token.IsNextContentRequested)
            await YarnTask.Yield();

        SetContinueVisible(false);
    }

    // Displays branching options; returns the selected DialogueOption? (YS 3.1 signature).
    // CS0672 suppressed — we must override this obsolete base method to intercept options.
#pragma warning disable CS0672
    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options,
        CancellationToken cancellationToken)
    {
        _selectedOption = null;
        SetContinueVisible(false);
        BuildOptionButtons(options);
        SetOptionsVisible(true);

        while (_selectedOption == null && !cancellationToken.IsCancellationRequested)
            await YarnTask.Yield();

        SetOptionsVisible(false);
        DialogueOption? result = _selectedOption;
        _selectedOption = null;
        return result;
    }
#pragma warning restore CS0672

    // ── Typewriter coroutine ──────────────────────────────────

    IEnumerator TypewriterCoroutine(string fullText, LineCancellationToken token)
    {
        const float charDelay = 0.03f;
        if (_dialogueTextTmp != null) _dialogueTextTmp.text = "";

        for (int i = 0; i <= fullText.Length; i++)
        {
            // IsNextContentRequested covers both skip-to-end and immediate cancel (YS 3.1)
            if (token.IsNextContentRequested) break;
            if (_dialogueTextTmp != null)
                _dialogueTextTmp.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(charDelay);
        }

        if (_dialogueTextTmp != null) _dialogueTextTmp.text = fullText;
        UpdatePortrait(talking: false);
        _typewriterDone = true;
    }

    // ── UI state helpers ──────────────────────────────────────

    void UpdatePortrait(bool talking)
    {
        if (_portraitImage == null || _currentCharacter == null) return;
        Sprite? target = (talking && _currentCharacter.portraitSpriteTalking != null)
            ? _currentCharacter.portraitSpriteTalking
            : _currentCharacter.portraitSprite;
        if (target != null) _portraitImage.sprite = target;
    }

    void SetContinueVisible(bool visible) { _continueBtn?.SetActive(visible); }
    void SetOptionsVisible(bool visible)  { _optionsContainer?.SetActive(visible); }

    void BuildOptionButtons(DialogueOption[] options)
    {
        if (_optionsContainer == null) return;

        foreach (var btn in _optionButtons)
            if (btn != null) Destroy(btn);

        int   count = Mathf.Min(options.Length, MaxOptions);
        float yPos  = 0f;

        for (int i = 0; i < count; i++)
        {
            DialogueOption opt      = options[i];
            DialogueOption captured = opt;
            string         text     = opt.Line.TextWithoutCharacterName.Text;
            bool           avail    = opt.IsAvailable;

            var btn = MakeButton(
                _optionsContainer.transform,
                avail ? text : $"<color=#888888>{text}</color>",
                new Vector2(0f, yPos),
                new Vector2(0f, -OptionH),
                avail ? UIColors.DialogueOption : UIColors.PanelDark,
                () => { if (avail) _selectedOption = captured; });

            _optionButtons[i] = btn;
            yPos -= OptionH + OptionGap;
        }

        if (_optionsContainer.TryGetComponent<RectTransform>(out var crt))
            crt.sizeDelta = new Vector2(0f, count * (OptionH + OptionGap));
    }

    // ── UI construction ───────────────────────────────────────

    void BuildUI()
    {
        Canvas? canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DialoguePanel: no Canvas found in scene.");
            return;
        }

        // Root panel
        _panel = new GameObject("DialoguePanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _panel.transform.SetParent(canvas.transform, false);

        var panelRt              = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRt.pivot            = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta        = new Vector2(PanelW, PanelH);
        panelRt.anchoredPosition = new Vector2(0f, -100f);

        var panelImg           = _panel.GetComponent<Image>();
        panelImg.color         = UIColors.DialoguePanel;
        panelImg.raycastTarget = false;

        // Portrait frame
        var portraitFrame = new GameObject("PortraitFrame",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitFrame.transform.SetParent(_panel.transform, false);

        var pfRt              = portraitFrame.GetComponent<RectTransform>();
        pfRt.anchorMin        = new Vector2(0f, 0.5f);
        pfRt.anchorMax        = new Vector2(0f, 0.5f);
        pfRt.pivot            = new Vector2(0f, 0.5f);
        pfRt.sizeDelta        = new Vector2(PortraitW, PortraitH);
        pfRt.anchoredPosition = new Vector2(Padding, 0f);
        portraitFrame.GetComponent<Image>().color         = UIColors.DialoguePortrait;
        portraitFrame.GetComponent<Image>().raycastTarget = false;

        var portraitGo = new GameObject("Portrait",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitGo.transform.SetParent(portraitFrame.transform, false);

        var piRt = portraitGo.GetComponent<RectTransform>();
        piRt.anchorMin = new Vector2(0.05f, 0.05f);
        piRt.anchorMax = new Vector2(0.95f, 0.95f);
        piRt.offsetMin = Vector2.zero;
        piRt.offsetMax = Vector2.zero;
        _portraitImage                = portraitGo.GetComponent<Image>();
        _portraitImage.preserveAspect = true;
        _portraitImage.raycastTarget  = false;

        // Right column
        float rightX = Padding + PortraitW + Padding;
        float rightW = PanelW - rightX - Padding;

        // Speaker name
        var nameGo = new GameObject("SpeakerName",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(_panel.transform, false);
        var nameRt              = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin        = new Vector2(0f, 1f);
        nameRt.anchorMax        = new Vector2(0f, 1f);
        nameRt.pivot            = new Vector2(0f, 1f);
        nameRt.sizeDelta        = new Vector2(rightW, NameH);
        nameRt.anchoredPosition = new Vector2(rightX, -Padding);
        _speakerNameTmp               = nameGo.GetComponent<TextMeshProUGUI>();
        _speakerNameTmp.fontSize      = FontName;
        _speakerNameTmp.fontStyle     = FontStyles.Bold;
        _speakerNameTmp.color         = UIColors.DialogueName;
        _speakerNameTmp.alignment     = TextAlignmentOptions.Left;
        _speakerNameTmp.raycastTarget = false;

        // Dialogue text
        var textGo = new GameObject("DialogueText",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt              = textGo.GetComponent<RectTransform>();
        textRt.anchorMin        = new Vector2(0f, 1f);
        textRt.anchorMax        = new Vector2(0f, 1f);
        textRt.pivot            = new Vector2(0f, 1f);
        textRt.sizeDelta        = new Vector2(rightW, TextH);
        textRt.anchoredPosition = new Vector2(rightX, -Padding - NameH - 6f);
        _dialogueTextTmp                  = textGo.GetComponent<TextMeshProUGUI>();
        _dialogueTextTmp.fontSize         = FontText;
        _dialogueTextTmp.color            = UIColors.TextPrimary;
        _dialogueTextTmp.alignment        = TextAlignmentOptions.TopLeft;
        _dialogueTextTmp.textWrappingMode = TextWrappingModes.Normal; // replaces obsolete enableWordWrapping
        _dialogueTextTmp.overflowMode     = TextOverflowModes.Truncate;
        _dialogueTextTmp.raycastTarget    = false;

        // Options container
        _optionsContainer = new GameObject("Options", typeof(RectTransform));
        _optionsContainer.transform.SetParent(_panel.transform, false);
        var optRt              = _optionsContainer.GetComponent<RectTransform>();
        optRt.anchorMin        = new Vector2(0f, 0f);
        optRt.anchorMax        = new Vector2(0f, 0f);
        optRt.pivot            = new Vector2(0f, 1f);
        optRt.sizeDelta        = new Vector2(rightW, MaxOptions * (OptionH + OptionGap));
        optRt.anchoredPosition = new Vector2(rightX,
            -PanelH * 0.5f + Padding + MaxOptions * (OptionH + OptionGap));

        // Continue button
        _continueBtn = MakeButton(_panel.transform, "Continue ▶",
            new Vector2(PanelW * 0.5f - Padding - ContinueBtnW * 0.5f,
                        -PanelH * 0.5f + Padding + ContinueBtnH * 0.5f),
            new Vector2(ContinueBtnW, ContinueBtnH),
            UIColors.DialogueOption,
            () => _advanceRequested = true);
    }

    // ── Button factory ────────────────────────────────────────

    GameObject MakeButton(Transform parent, string label,
        Vector2 anchoredPos, Vector2 size, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;

        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var lbl = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lbl.transform.SetParent(go.transform, false);

        var lrt              = lbl.GetComponent<RectTransform>();
        lrt.anchorMin        = Vector2.zero;
        lrt.anchorMax        = Vector2.one;
        lrt.sizeDelta        = new Vector2(-8f, 0f);
        lrt.anchoredPosition = Vector2.zero;

        var tmp               = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text              = label;
        tmp.fontSize          = label.Length < 20 ? FontContinue : FontOption;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.color             = UIColors.TextPrimary;
        tmp.richText          = true;
        tmp.textWrappingMode  = TextWrappingModes.Normal; // replaces obsolete enableWordWrapping
        tmp.raycastTarget     = false;

        return go;
    }
}
