using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Meniul principal, construit complet din cod (zero wiring de Canvas in Editor).
/// Pune scriptul pe un GameObject gol in scena "MainMenu".
///
/// Panouri:
///  - Root: titlu, camp NUME jucator, butoane PLAY / SETARI / IESIRE.
///  - Play: alegere HOST / JOIN (IP) -> salveaza si incarca scena "Lobby".
///  - Setari: volum, sensibilitate mouse, fullscreen. Persistate in PlayerPrefs.
///
/// Numele jucatorului si IP-ul sunt salvate in PlayerPrefs si citite de lobby.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Config")]
    public string title = "GHOST FINDER";
    public string lobbySceneName = "Lobby";

    // Chei PlayerPrefs partajate cu lobby-ul / restul jocului.
    public const string KeyName = "gf_player_name";
    public const string KeyIp = "gf_join_ip";
    public const string KeyVolume = "gf_volume";
    public const string KeySensitivity = "gf_sensitivity";
    public const string KeyFullscreen = "gf_fullscreen";
    // "host" sau "join" - citit de bootstrap-ul de retea din scena Lobby.
    public const string KeyNetRole = "gf_net_role";

    private Font font;

    private GameObject rootPanel;
    private GameObject playPanel;
    private GameObject settingsPanel;

    private InputField nameField;
    private InputField ipField;
    private Text nameWarn;

    private static readonly Color ColPanel = new Color(0.08f, 0.08f, 0.10f, 0.94f);
    private static readonly Color ColBtn = new Color(0.18f, 0.20f, 0.26f, 1f);
    private static readonly Color ColAccent = new Color(0.30f, 0.55f, 0.85f, 1f);
    private static readonly Color ColDanger = new Color(0.5f, 0.18f, 0.20f, 1f);
    private static readonly Color ColText = new Color(0.92f, 0.92f, 0.95f, 1f);

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
        ShowRoot();
    }

    // ---------------- Actiuni ----------------

    private void OnPlay()
    {
        if (!ValidName()) return;
        SaveName();
        ShowPlay();
    }

    private void OnHost()
    {
        if (!ValidName()) { ShowRoot(); return; }
        SaveName();
        PlayerPrefs.SetString(KeyNetRole, "host");
        PlayerPrefs.Save();
        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnJoin()
    {
        if (!ValidName()) { ShowRoot(); return; }
        SaveName();
        string ip = string.IsNullOrWhiteSpace(ipField.text) ? "127.0.0.1" : ipField.text.Trim();
        PlayerPrefs.SetString(KeyIp, ip);
        PlayerPrefs.SetString(KeyNetRole, "join");
        PlayerPrefs.Save();
        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool ValidName()
    {
        bool ok = nameField != null && nameField.text.Trim().Length >= 1;
        if (nameWarn != null) nameWarn.gameObject.SetActive(!ok);
        return ok;
    }

    private void SaveName()
    {
        PlayerPrefs.SetString(KeyName, nameField.text.Trim());
        PlayerPrefs.Save();
    }

    // ---------------- Navigare panouri ----------------

    private void ShowRoot()
    {
        rootPanel.SetActive(true);
        playPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void ShowPlay()
    {
        rootPanel.SetActive(false);
        playPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    private void ShowSettings()
    {
        rootPanel.SetActive(false);
        playPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // ---------------- Construire UI ----------------

    private void BuildUI()
    {
        var canvasGO = new GameObject("MainMenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        BuildBackdrop(canvasGO.transform);
        BuildRootPanel(canvasGO.transform);
        BuildPlayPanel(canvasGO.transform);
        BuildSettingsPanel(canvasGO.transform);
    }

    private void BuildBackdrop(Transform parent)
    {
        var go = new GameObject("Backdrop", typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void BuildRootPanel(Transform parent)
    {
        rootPanel = Panel(parent, new Vector2(480, 460));
        Label(rootPanel.transform, title, 40, FontStyle.Bold, ColAccent, new Vector2(0, 170), new Vector2(440, 56));

        Label(rootPanel.transform, "Nume jucator:", 16, FontStyle.Normal, ColText, new Vector2(0, 110), new Vector2(420, 24));
        nameField = Field(rootPanel.transform, PlayerPrefs.GetString(KeyName, ""), new Vector2(0, 78), new Vector2(360, 36));
        nameWarn = Label(rootPanel.transform, "Introdu un nume!", 14, FontStyle.Italic, ColDanger, new Vector2(0, 50), new Vector2(360, 20));
        nameWarn.gameObject.SetActive(false);

        MakeButton(rootPanel.transform, "PLAY", new Vector2(0, 2), new Vector2(360, 50), ColAccent).onClick.AddListener(OnPlay);
        MakeButton(rootPanel.transform, "SETARI", new Vector2(0, -58), new Vector2(360, 46), ColBtn).onClick.AddListener(ShowSettings);
        MakeButton(rootPanel.transform, "IESIRE", new Vector2(0, -114), new Vector2(360, 46), ColDanger).onClick.AddListener(OnQuit);
    }

    private void BuildPlayPanel(Transform parent)
    {
        playPanel = Panel(parent, new Vector2(480, 420));
        Label(playPanel.transform, "MULTIPLAYER (LAN)", 26, FontStyle.Bold, ColAccent, new Vector2(0, 150), new Vector2(440, 40));

        MakeButton(playPanel.transform, "HOST (creeaza lobby)", new Vector2(0, 80), new Vector2(380, 50), ColAccent).onClick.AddListener(OnHost);

        Label(playPanel.transform, "IP server (pentru Join):", 16, FontStyle.Normal, ColText, new Vector2(0, 30), new Vector2(420, 24));
        ipField = Field(playPanel.transform, PlayerPrefs.GetString(KeyIp, "127.0.0.1"), new Vector2(0, -2), new Vector2(360, 36));
        MakeButton(playPanel.transform, "JOIN", new Vector2(0, -58), new Vector2(380, 50), ColBtn).onClick.AddListener(OnJoin);

        MakeButton(playPanel.transform, "INAPOI", new Vector2(0, -150), new Vector2(380, 44), ColDanger).onClick.AddListener(ShowRoot);
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = Panel(parent, new Vector2(500, 460));
        Label(settingsPanel.transform, "SETARI", 30, FontStyle.Bold, ColAccent, new Vector2(0, 180), new Vector2(440, 44));

        // Volum
        Label(settingsPanel.transform, "Volum", 16, FontStyle.Normal, ColText, new Vector2(-150, 110), new Vector2(160, 24));
        var volLabel = Label(settingsPanel.transform, "", 16, FontStyle.Bold, ColText, new Vector2(170, 110), new Vector2(80, 24));
        var volSlider = MakeSlider(settingsPanel.transform, new Vector2(10, 110), new Vector2(220, 24),
            0f, 1f, PlayerPrefs.GetFloat(KeyVolume, 1f));
        volSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(KeyVolume, v);
            volLabel.text = Mathf.RoundToInt(v * 100f) + "%";
        });
        volLabel.text = Mathf.RoundToInt(volSlider.value * 100f) + "%";
        AudioListener.volume = volSlider.value;

        // Sensibilitate mouse
        Label(settingsPanel.transform, "Sensibilitate", 16, FontStyle.Normal, ColText, new Vector2(-150, 55), new Vector2(180, 24));
        var sensLabel = Label(settingsPanel.transform, "", 16, FontStyle.Bold, ColText, new Vector2(170, 55), new Vector2(80, 24));
        var sensSlider = MakeSlider(settingsPanel.transform, new Vector2(10, 55), new Vector2(220, 24),
            0.5f, 5f, PlayerPrefs.GetFloat(KeySensitivity, 2f));
        sensSlider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(KeySensitivity, v);
            sensLabel.text = v.ToString("0.0");
        });
        sensLabel.text = sensSlider.value.ToString("0.0");

        // Fullscreen toggle
        var fsToggle = MakeToggle(settingsPanel.transform, "Fullscreen", new Vector2(0, 0), new Vector2(300, 30),
            PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1);
        fsToggle.onValueChanged.AddListener(on =>
        {
            Screen.fullScreen = on;
            PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        });

        MakeButton(settingsPanel.transform, "INAPOI", new Vector2(0, -170), new Vector2(380, 46), ColAccent)
            .onClick.AddListener(() => { PlayerPrefs.Save(); ShowRoot(); });
    }

    // ---------------- Helpers uGUI ----------------

    private GameObject Panel(Transform parent, Vector2 size)
    {
        var go = new GameObject("Panel", typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = ColPanel;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return go;
    }

    private Text Label(Transform parent, string text, int size, FontStyle style, Color color, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject("Label", typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        return t;
    }

    private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 dim, Color color)
    {
        var go = new GameObject("Button", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = color * 1.25f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
        btn.colors = colors;

        Label(go.transform, label, 18, FontStyle.Bold, Color.white, Vector2.zero, dim);
        return btn;
    }

    private InputField Field(Transform parent, string defaultText, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject("InputField", typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;

        var textComp = Label(go.transform, "", 16, FontStyle.Normal, ColText, Vector2.zero, dim - new Vector2(16, 0));
        textComp.alignment = TextAnchor.MiddleLeft;
        textComp.supportRichText = false;

        var field = go.GetComponent<InputField>();
        field.textComponent = textComp;
        field.text = defaultText;
        return field;
    }

    private Slider MakeSlider(Transform parent, Vector2 pos, Vector2 dim, float min, float max, float val)
    {
        var go = new GameObject("Slider", typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;

        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(go.transform, false);
        bg.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 1f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-5, 0);

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = ColAccent;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.sizeDelta = new Vector2(10, 0);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle", typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<Image>().color = ColText;
        var hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(18, 0);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;
        return slider;
    }

    private Toggle MakeToggle(Transform parent, string label, Vector2 pos, Vector2 dim, bool on)
    {
        var go = new GameObject("Toggle", typeof(Toggle));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;

        var box = new GameObject("Box", typeof(Image));
        box.transform.SetParent(go.transform, false);
        box.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.20f, 1f);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0, 0.5f);
        boxRt.pivot = new Vector2(0, 0.5f);
        boxRt.anchoredPosition = new Vector2(-dim.x / 2f + 14, 0);
        boxRt.sizeDelta = new Vector2(26, 26);

        var check = new GameObject("Check", typeof(Image));
        check.transform.SetParent(box.transform, false);
        check.GetComponent<Image>().color = ColAccent;
        var checkRt = check.GetComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero; checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = new Vector2(4, 4); checkRt.offsetMax = new Vector2(-4, -4);

        var lbl = Label(go.transform, label, 16, FontStyle.Normal, ColText, new Vector2(20, 0), dim);
        lbl.alignment = TextAnchor.MiddleLeft;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        return toggle;
    }

    private void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }
    }
}
