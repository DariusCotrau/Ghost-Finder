using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI-ul din timpul meciului, construit din cod. Pus pe un GameObject in scena
/// de joc. Afiseaza:
///  - HUD: timer + rolul tau.
///  - Pauza (ESC): Continua / Inapoi in lobby (host) / Deconectare.
///  - Final meci: castigatorul + Inapoi in lobby (host) / Deconectare.
/// La deconectare -> Main Menu.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";

    // Citit de MouseLook ca sa elibereze cursorul cand un meniu e deschis.
    public static bool MenuOpen { get; private set; }

    private Font font;

    private GameObject hudPanel;
    private Text timerText;
    private Text roleText;

    private GameObject pausePanel;
    private Button pauseLobbyBtn;

    private GameObject endPanel;
    private Text endText;
    private Button endLobbyBtn;

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

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientStopped += OnStopped;
            nm.OnServerStopped += OnStopped;
        }
    }

    private void OnDestroy()
    {
        MenuOpen = false;
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientStopped -= OnStopped;
            nm.OnServerStopped -= OnStopped;
        }
    }

    private void OnStopped(bool _) => SceneManager.LoadScene(mainMenuScene);

    private void Update()
    {
        var gm = GameManager.Instance;
        var nm = NetworkManager.Singleton;
        bool connected = nm != null && (nm.IsClient || nm.IsServer);
        bool started = gm != null && gm.MatchStarted.Value;
        bool ended = gm != null && gm.MatchEnded.Value;
        bool playing = connected && started && !ended;

        // Pauza (doar in meci).
        if (playing && Input.GetKeyDown(KeyCode.Escape))
            pausePanel.SetActive(!pausePanel.activeSelf);
        if (!playing) pausePanel.SetActive(false);

        endPanel.SetActive(connected && ended);
        if (connected && ended) RefreshEnd(nm, gm);

        hudPanel.SetActive(playing && !pausePanel.activeSelf);
        if (hudPanel.activeSelf) RefreshHud(gm);

        if (pausePanel.activeSelf) pauseLobbyBtn.gameObject.SetActive(nm.IsServer);

        MenuOpen = pausePanel.activeSelf || endPanel.activeSelf;
    }

    private void RefreshHud(GameManager gm)
    {
        int t = Mathf.CeilToInt(gm.TimeRemaining.Value);
        timerText.text = $"{t / 60:00}:{t % 60:00}";

        PlayerRole role = PlayerRole.None;
        foreach (var lp in GameManager.AllPlayers())
            if (lp.IsOwner) { role = lp.Role.Value; break; }
        roleText.text = role == PlayerRole.Hunter ? "HUNTER" : (role == PlayerRole.Ghost ? "GHOST" : "");
        roleText.color = role == PlayerRole.Ghost ? new Color(0.6f, 0.3f, 1f) : ColAccent;
    }

    private void RefreshEnd(NetworkManager nm, GameManager gm)
    {
        endText.text = $"MECI TERMINAT\nCastiga: {gm.Winner.Value}";
        endLobbyBtn.gameObject.SetActive(nm.IsServer);
    }

    // ---------------- Actiuni ----------------

    private void OnResume() => pausePanel.SetActive(false);

    private void OnLobby()
    {
        pausePanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.ReturnToLobby();
    }

    private void OnDisconnect()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsServer)) nm.Shutdown();
        else SceneManager.LoadScene(mainMenuScene);
    }

    // ---------------- Construire UI ----------------

    private void BuildUI()
    {
        var canvasGO = new GameObject("GameHUDCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        BuildHud(canvasGO.transform);
        BuildPausePanel(canvasGO.transform);
        BuildEndPanel(canvasGO.transform);
    }

    private void BuildHud(Transform parent)
    {
        hudPanel = new GameObject("Hud", typeof(RectTransform));
        hudPanel.transform.SetParent(parent, false);
        var rt = hudPanel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -16);
        rt.sizeDelta = new Vector2(300, 80);

        timerText = Label(hudPanel.transform, "00:00", 34, FontStyle.Bold, ColText, new Vector2(0, -18), new Vector2(300, 44));
        roleText = Label(hudPanel.transform, "", 18, FontStyle.Bold, ColAccent, new Vector2(0, -54), new Vector2(300, 26));
    }

    private void BuildPausePanel(Transform parent)
    {
        pausePanel = Panel(parent, new Vector2(380, 320));
        Label(pausePanel.transform, "PAUZA", 30, FontStyle.Bold, ColAccent, new Vector2(0, 110), new Vector2(340, 44));
        MakeButton(pausePanel.transform, "CONTINUA", new Vector2(0, 40), new Vector2(320, 46), ColBtn).onClick.AddListener(OnResume);
        pauseLobbyBtn = MakeButton(pausePanel.transform, "INAPOI IN LOBBY", new Vector2(0, -20), new Vector2(320, 46), ColAccent);
        pauseLobbyBtn.onClick.AddListener(OnLobby);
        MakeButton(pausePanel.transform, "DECONECTARE", new Vector2(0, -80), new Vector2(320, 46), ColDanger).onClick.AddListener(OnDisconnect);
        pausePanel.SetActive(false);
    }

    private void BuildEndPanel(Transform parent)
    {
        endPanel = Panel(parent, new Vector2(420, 300));
        endText = Label(endPanel.transform, "", 24, FontStyle.Bold, ColAccent, new Vector2(0, 70), new Vector2(380, 80));
        endLobbyBtn = MakeButton(endPanel.transform, "INAPOI IN LOBBY", new Vector2(0, -30), new Vector2(360, 46), ColAccent);
        endLobbyBtn.onClick.AddListener(OnLobby);
        MakeButton(endPanel.transform, "DECONECTARE", new Vector2(0, -90), new Vector2(360, 44), ColDanger).onClick.AddListener(OnDisconnect);
        endPanel.SetActive(false);
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

        Label(go.transform, label, 17, FontStyle.Bold, Color.white, Vector2.zero, dim);
        return btn;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }
    }
}
