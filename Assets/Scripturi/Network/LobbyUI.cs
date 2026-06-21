using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lobby UI uGUI construit din cod (zero wiring de Canvas). Pune-l pe un
/// GameObject gol in scena LOBBY si merge.
///
/// Doua panouri:
///  - Conectare: titlu, IP, butoane Host / Client, status.
///  - Lobby: mod + nr jucatori, status meci, lista jucatori+roluri,
///    butoane Start / Reset (doar host) si Deconectare.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Aspect")]
    public string title = "GHOST FINDER";

    private Font font;
    private string joinAddress = "127.0.0.1";
    private string status = "";

    // Conectare
    private GameObject connectPanel;
    private InputField ipField;
    private Text connectStatus;

    // Lobby
    private GameObject lobbyPanel;
    private Text headerText;
    private Text matchText;
    private Text playersText;
    private Button startBtn;
    private Button resetBtn;

    private static readonly Color ColPanel = new Color(0.08f, 0.08f, 0.10f, 0.94f);
    private static readonly Color ColBtn = new Color(0.18f, 0.20f, 0.26f, 1f);
    private static readonly Color ColAccent = new Color(0.30f, 0.55f, 0.85f, 1f);
    private static readonly Color ColText = new Color(0.92f, 0.92f, 0.95f, 1f);

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
    }

    private void Update()
    {
        var nm = NetworkManager.Singleton;
        bool connected = nm != null && (nm.IsClient || nm.IsServer);

        connectPanel.SetActive(!connected);
        lobbyPanel.SetActive(connected);

        if (connectStatus != null) connectStatus.text = status;
        if (connected) RefreshLobby(nm);
    }

    // ---------------- Actiuni ----------------

    private void OnHost()
    {
        SetAddress(ipField != null ? ipField.text : joinAddress);
        NetworkManager.Singleton.StartHost();
    }

    private void OnClient()
    {
        SetAddress(ipField != null ? ipField.text : joinAddress);
        if (NetworkManager.Singleton.StartClient())
            status = "Conectare...";
    }

    private void OnStart()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.StartMatch();
    }

    private void OnReset()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.ResetMatch();
    }

    private void OnDisconnect()
    {
        NetworkManager.Singleton.Shutdown();
        status = "";
    }

    private void SetAddress(string addr)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.ConnectionData.Address = string.IsNullOrWhiteSpace(addr) ? "127.0.0.1" : addr.Trim();
    }

    // ---------------- Refresh lobby ----------------

    private void RefreshLobby(NetworkManager nm)
    {
        LobbyPlayer[] players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        string mode = nm.IsHost ? "HOST" : (nm.IsServer ? "SERVER" : "CLIENT");
        headerText.text = $"Mod: {mode}   |   Jucatori: {players.Length}";

        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (gm.MatchEnded.Value)
                matchText.text = $">>> MECI TERMINAT - Castiga: {gm.Winner.Value} <<<";
            else if (gm.MatchStarted.Value)
            {
                int t = Mathf.CeilToInt(gm.TimeRemaining.Value);
                matchText.text = $"MECI PORNIT   |   Timp: {t / 60:00}:{t % 60:00}";
            }
            else
                matchText.text = $"In asteptare (min {GameManager.MinPlayers} jucatori)...";
        }
        else
        {
            matchText.text = "(GameManager indisponibil)";
        }

        // Lista jucatori
        var sb = new System.Text.StringBuilder();
        foreach (var lp in players)
        {
            string role = lp.Role.Value == PlayerRole.None ? "(neatribuit)" : lp.Role.Value.ToString();
            sb.AppendLine($"{lp.DisplayName.Value}: {role}");
        }
        playersText.text = sb.ToString();

        // Butoane host: vizibile + interactabile dupa stare.
        bool isServer = nm.IsServer && gm != null;
        bool started = gm != null && gm.MatchStarted.Value;

        startBtn.gameObject.SetActive(isServer && !started);
        resetBtn.gameObject.SetActive(isServer && started);
        if (isServer && !started)
            startBtn.interactable = gm.CanStart;
    }

    // ---------------- Construire UI ----------------

    private void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("LobbyCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        BuildConnectPanel(canvasGO.transform);
        BuildLobbyPanel(canvasGO.transform);
    }

    private void BuildConnectPanel(Transform parent)
    {
        connectPanel = Panel(parent, new Vector2(440, 360));

        Label(connectPanel.transform, title, 34, FontStyle.Bold, ColAccent, new Vector2(0, 130), new Vector2(400, 50));
        Label(connectPanel.transform, "LOBBY (LAN)", 18, FontStyle.Normal, ColText, new Vector2(0, 95), new Vector2(400, 30));

        Label(connectPanel.transform, "IP server (pentru Client):", 16, FontStyle.Normal, ColText, new Vector2(0, 50), new Vector2(380, 24));
        ipField = Field(connectPanel.transform, joinAddress, new Vector2(0, 18), new Vector2(360, 36));

        var host = MakeButton(connectPanel.transform, "HOST (Server + Joc)", new Vector2(0, -34), new Vector2(360, 46), ColAccent);
        host.onClick.AddListener(OnHost);

        var client = MakeButton(connectPanel.transform, "CLIENT (Join)", new Vector2(0, -90), new Vector2(360, 46), ColBtn);
        client.onClick.AddListener(OnClient);

        connectStatus = Label(connectPanel.transform, "", 15, FontStyle.Italic, ColText, new Vector2(0, -140), new Vector2(380, 24));
    }

    private void BuildLobbyPanel(Transform parent)
    {
        lobbyPanel = Panel(parent, new Vector2(480, 560));

        headerText = Label(lobbyPanel.transform, "", 20, FontStyle.Bold, ColAccent, new Vector2(0, 240), new Vector2(440, 30));
        matchText = Label(lobbyPanel.transform, "", 17, FontStyle.Normal, ColText, new Vector2(0, 205), new Vector2(440, 28));

        Label(lobbyPanel.transform, "--- Jucatori ---", 16, FontStyle.Bold, ColText, new Vector2(0, 165), new Vector2(440, 24));
        playersText = Label(lobbyPanel.transform, "", 16, FontStyle.Normal, ColText, new Vector2(0, 40), new Vector2(440, 220));
        playersText.alignment = TextAnchor.UpperCenter;

        startBtn = MakeButton(lobbyPanel.transform, "START MECI (atribuie roluri)", new Vector2(0, -120), new Vector2(420, 46), ColAccent);
        startBtn.onClick.AddListener(OnStart);

        resetBtn = MakeButton(lobbyPanel.transform, "RESET (lobby nou)", new Vector2(0, -120), new Vector2(420, 46), ColBtn);
        resetBtn.onClick.AddListener(OnReset);

        var disc = MakeButton(lobbyPanel.transform, "DECONECTARE", new Vector2(0, -180), new Vector2(420, 40), new Color(0.5f, 0.18f, 0.20f, 1f));
        disc.onClick.AddListener(OnDisconnect);
    }

    // ---------------- Helpers uGUI ----------------

    private GameObject Panel(Transform parent, Vector2 size)
    {
        var go = new GameObject("Panel", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = ColPanel;
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
        var img = go.GetComponent<Image>();
        img.color = color;
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

    private void EnsureEventSystem()
    {
        // Trebuie sa supravietuiasca schimbarii de scena (Lobby -> Joc) ca butoanele
        // RESET/Deconectare din timpul meciului sa functioneze. Il punem sub acest
        // GameObject (care e pe NetworkManager = DontDestroyOnLoad).
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
            existing.transform.SetParent(transform, false);
        else
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }
    }
}
