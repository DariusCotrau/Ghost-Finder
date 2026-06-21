using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI-ul camerei de asteptare (waiting room), construit din cod. Pus pe un
/// GameObject propriu in scena Lobby (NU pe NetworkManager) ca sa dispara cand
/// se incarca scena de joc. Conexiunea e pornita deja de NetworkBootstrap pe
/// baza alegerii din Main Menu.
///
/// Stari:
///  - Conectare: spinner text pana cand clientul/serverul e gata.
///  - Lobby: lista jucatori (nume + GATA/asteapta), buton READY (toggle local),
///    START (doar host, activ cand min jucatori + toti gata), IESIRE.
/// La deconectare -> revine in Main Menu.
/// </summary>
public class LobbyController : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";

    private Font font;

    private GameObject connectingPanel;
    private Text connectingText;

    private GameObject lobbyPanel;
    private Text headerText;
    private Text playersText;
    private Button readyBtn;
    private Text readyBtnLabel;
    private Button startBtn;

    private static readonly Color ColPanel = new Color(0.08f, 0.08f, 0.10f, 0.94f);
    private static readonly Color ColBtn = new Color(0.18f, 0.20f, 0.26f, 1f);
    private static readonly Color ColAccent = new Color(0.30f, 0.55f, 0.85f, 1f);
    private static readonly Color ColGreen = new Color(0.22f, 0.50f, 0.25f, 1f);
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
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
        var nm = NetworkManager.Singleton;
        bool connected = nm != null && (nm.IsClient || nm.IsServer);
        var gm = GameManager.Instance;
        bool started = gm != null && gm.MatchStarted.Value;

        // In lobby cursorul trebuie sa fie mereu liber (sa poti da click).
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Cat timp se conecteaza sau se atribuie roluri -> doar mesaj.
        bool ready = connected && gm != null && !started;
        connectingPanel.SetActive(!ready);
        lobbyPanel.SetActive(ready);

        if (!ready)
        {
            connectingText.text = !connected ? "Conectare..." : "Se pregateste lobby-ul...";
            return;
        }

        RefreshLobby(nm, gm);
    }

    // ---------------- Refresh ----------------

    private LobbyPlayer LocalPlayer()
    {
        foreach (var lp in GameManager.AllPlayers())
            if (lp.IsOwner) return lp;
        return null;
    }

    private void RefreshLobby(NetworkManager nm, GameManager gm)
    {
        var players = GameManager.AllPlayers();
        string mode = nm.IsHost ? "HOST" : (nm.IsServer ? "SERVER" : "CLIENT");
        headerText.text = $"LOBBY ({mode})   Jucatori: {players.Length} / min {GameManager.MinPlayers}";

        var sb = new System.Text.StringBuilder();
        foreach (var lp in players)
        {
            string state = lp.IsReady.Value ? "<GATA>" : "asteapta";
            string me = lp.IsOwner ? "  (tu)" : "";
            sb.AppendLine($"{lp.DisplayName.Value}{me}: {state}");
        }
        playersText.text = sb.ToString();

        // Buton ready local.
        var local = LocalPlayer();
        if (local != null)
        {
            bool r = local.IsReady.Value;
            readyBtnLabel.text = r ? "ANULEAZA GATA" : "SUNT GATA";
            readyBtn.image.color = r ? ColGreen : ColBtn;
        }

        // Start: doar host, activ cand se poate.
        startBtn.gameObject.SetActive(nm.IsServer);
        if (nm.IsServer) startBtn.interactable = gm.CanStart;
    }

    // ---------------- Actiuni ----------------

    private void OnReady()
    {
        var local = LocalPlayer();
        if (local != null) local.ToggleReady();
    }

    private void OnStart()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartMatch();
    }

    private void OnLeave()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsServer))
            nm.Shutdown(); // OnStopped -> revine in meniu
        else
            SceneManager.LoadScene(mainMenuScene);
    }

    // ---------------- Construire UI ----------------

    private void BuildUI()
    {
        var canvasGO = new GameObject("LobbyCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        BuildBackdrop(canvasGO.transform);
        BuildConnectingPanel(canvasGO.transform);
        BuildLobbyPanel(canvasGO.transform);
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

    private void BuildConnectingPanel(Transform parent)
    {
        connectingPanel = Panel(parent, new Vector2(440, 200));
        Label(connectingPanel.transform, "GHOST FINDER", 30, FontStyle.Bold, ColAccent, new Vector2(0, 50), new Vector2(400, 44));
        connectingText = Label(connectingPanel.transform, "Conectare...", 18, FontStyle.Italic, ColText, new Vector2(0, -10), new Vector2(400, 30));
        MakeButton(connectingPanel.transform, "INAPOI IN MENIU", new Vector2(0, -60), new Vector2(300, 40), ColDanger).onClick.AddListener(OnLeave);
    }

    private void BuildLobbyPanel(Transform parent)
    {
        lobbyPanel = Panel(parent, new Vector2(520, 560));
        headerText = Label(lobbyPanel.transform, "", 20, FontStyle.Bold, ColAccent, new Vector2(0, 240), new Vector2(480, 30));
        Label(lobbyPanel.transform, "--- Jucatori ---", 16, FontStyle.Bold, ColText, new Vector2(0, 200), new Vector2(480, 24));
        playersText = Label(lobbyPanel.transform, "", 17, FontStyle.Normal, ColText, new Vector2(0, 60), new Vector2(480, 260));
        playersText.alignment = TextAnchor.UpperCenter;

        readyBtn = MakeButton(lobbyPanel.transform, "SUNT GATA", new Vector2(0, -130), new Vector2(460, 50), ColBtn);
        readyBtn.onClick.AddListener(OnReady);
        readyBtnLabel = readyBtn.GetComponentInChildren<Text>();

        startBtn = MakeButton(lobbyPanel.transform, "START MECI", new Vector2(0, -190), new Vector2(460, 48), ColAccent);
        startBtn.onClick.AddListener(OnStart);

        MakeButton(lobbyPanel.transform, "IESIRE", new Vector2(0, -245), new Vector2(460, 40), ColDanger).onClick.AddListener(OnLeave);
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
