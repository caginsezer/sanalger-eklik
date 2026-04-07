using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Yöneticisi - HUD, sıra göstergesi, durum mesajları, kazanma ekranı
/// Mobil dikey ekran (Portrait 1080x1920) uyumlu
/// </summary>
public class UIManager : MonoBehaviour
{
    private Text turnText;
    private Text player1MagnetsText;
    private Text player2MagnetsText;
    private Text player1ScoreText;
    private Text player2ScoreText;
    private Text statusText;
    private GameObject winPanel;
    private Text winText;
    private Button restartButton;
    private Image turnIndicatorBg;
    private Image player1Panel;
    private Image player2Panel;
    private Image timerBarFill;
    private Text timerText;

    private Canvas mainCanvas;
    private GameObject canvasObj; // ShowGameUI erişimi için field

    public void Initialize()
    {
        CreateUI();
        SubscribeToEvents();
    }

    private Font GetDefaultFont()
    {
        // Unity 6 uyumlu font yükleme
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (font == null)
        {
            string[] fontNames = Font.GetOSInstalledFontNames();
            if (fontNames.Length > 0)
                font = Font.CreateDynamicFontFromOSFont(fontNames[0], 14);
        }
        return font;
    }

    private void CreateUI()
    {
        // Ana Canvas
        canvasObj = new GameObject("UICanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceCamera; // Overlay yerine Camera (Xiaomi Fix)
        mainCanvas.worldCamera = Camera.main;
        mainCanvas.planeDistance = 0.5f; 
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Mobil (Dikey) Oyun Alanı
        scaler.referenceResolution = new Vector2(720, 1280); // Daha stabil bir oran (9:16)
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ========== ÜST BAR - LANDSCAPE LAYOUT ==========

        // --- ANA BAŞLIK ---
        CreateText(canvasObj.transform, "GameTitle", "MAGNETIC MAYHEM",
            36, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-300, -50), new Vector2(300, -10));

        // Sol Alt: Oyuncu 1 (Sen - Aşağıda)
        GameObject p1Bar = CreatePanel(canvasObj.transform, "P1Bar",
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), // Bottom Left
            Vector2.zero, new Vector2(0, 10)); // Yukarı kalkık
        
        // **Panel arkaplanı kaldırıldı (görünmez)**
        player1Panel = p1Bar.GetComponent<Image>();
        player1Panel.color = new Color(0, 0, 0, 0); 
        player1Panel.raycastTarget = false; // Tıklamayı engellemesin
        
        RectTransform p1Rect = p1Bar.GetComponent<RectTransform>();
        p1Rect.offsetMin = new Vector2(30, 80); // Zaman çubuğuna (timer) çarpmaması için yukarı kaldırıldı
        p1Rect.offsetMax = new Vector2(0, 150);
        p1Bar.layer = 5;

        player1MagnetsText = CreateText(p1Bar.transform, "P1Title", "PLAYER 1 (YOU): 12 STONES LEFT",
            24, FontStyle.Bold, Color.white,
            new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0f, 1f), // Sola hizalı
            new Vector2(0, -2), new Vector2(0, -2));
        player1MagnetsText.alignment = TextAnchor.MiddleLeft;

        player1ScoreText = CreateText(p1Bar.transform, "P1Score", "SCORE: 0",
            28, FontStyle.Bold, Color.white,
            new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0f, 0.5f), // Sola hizalı
            new Vector2(0, 4), new Vector2(0, -4));
        player1ScoreText.alignment = TextAnchor.MiddleLeft;

        // Sol Üst: Oyuncu 2 (Bot - Yukarıda)
        GameObject p2Bar = CreatePanel(canvasObj.transform, "P2Bar",
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), // Top Left
            Vector2.zero, new Vector2(0, -10)); // Aşağı sarkık
        
        // **Panel arkaplanı kaldırıldı (görünmez)**
        player2Panel = p2Bar.GetComponent<Image>();
        player2Panel.color = new Color(0, 0, 0, 0); 
        player2Panel.raycastTarget = false; // Tıklamayı engellemesin
        
        RectTransform p2Rect = p2Bar.GetComponent<RectTransform>();
        p2Rect.offsetMin = new Vector2(30, -110); // Magnetic Mayhem yazısı ile çakışmaması için biraz daha aşağı taşındı
        p2Rect.offsetMax = new Vector2(0, -40);
        p2Bar.layer = 5;

        player2MagnetsText = CreateText(p2Bar.transform, "P2Title", "PLAYER 2 (BOT): 12 STONES LEFT",
            24, FontStyle.Bold, Color.white,
            new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0f, 1f), // Sola hizalı
            new Vector2(0, -2), new Vector2(0, -2));
        player2MagnetsText.alignment = TextAnchor.MiddleLeft;

        player2ScoreText = CreateText(p2Bar.transform, "P2Score", "SCORE: 0",
            28, FontStyle.Bold, Color.white,
            new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0f, 0.5f), // Sola hizalı
            new Vector2(0, 4), new Vector2(0, -4));
        player2ScoreText.alignment = TextAnchor.MiddleLeft;

        // ========== ALT BAR - YOUR TURN & TIMER ==========
        
        GameObject bottomBar = CreatePanel(canvasObj.transform, "StatusBar",
            new Vector2(0.35f, 0), new Vector2(0.65f, 0), new Vector2(0.5f, 0),
            Vector2.zero, new Vector2(0, 15));
        // Koyu kapsül arka plan
        Image statusImg = bottomBar.GetComponent<Image>();
        statusImg.color = new Color(0.1f, 0.15f, 0.2f, 0.85f);
        statusImg.raycastTarget = false; // Tıklamayı engellemesin

        RectTransform statusRect = bottomBar.GetComponent<RectTransform>();
        statusRect.offsetMin = new Vector2(0, 10);
        statusRect.offsetMax = new Vector2(0, 65);
        bottomBar.layer = 5;

        // Turn Text (YOUR TURN!)
        turnText = CreateText(bottomBar.transform, "TurnText", "YOUR TURN!",
            30, FontStyle.Bold, new Color(0.2f, 0.9f, 0.9f), // Parlak cyan
            new Vector2(0, 0.4f), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        // Timer Bar in bottom
        GameObject timerBarObj = CreatePanel(bottomBar.transform, "TimerBar",
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.3f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        timerBarObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        
        GameObject timerFillObj = CreatePanel(timerBarObj.transform, "TimerFill",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        timerBarFill = timerFillObj.GetComponent<Image>();
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        timerBarFill.color = Color.green;

        timerText = CreateText(timerBarObj.transform, "TimerText", "10.0s",
            16, FontStyle.Bold, Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        // (Eski ayrı status text kaldırıldı, yeri TurnText'e verildi)

        // ========== KAZANMA EKRANI ==========
        CreateWinScreen(canvasObj.transform);

        // ========== TALİMATLAR ==========
        CreateInstructions(canvasObj.transform);

        // ========== ALT KÖŞE BUTONLARI: UNDO (Sol) + SETTINGS (Sağ) ==========
        CreateCornerButtons(canvasObj.transform);

        // Başlangıçta ana menü açık olacağı için in-game UI'ı gizle
        canvasObj.SetActive(false);
    }

    public void ShowGameUI()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(true);
        }
    }

    private void CreateWinScreen(Transform parent)
    {
        winPanel = CreatePanel(parent, "WinPanel",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        RectTransform winRect = winPanel.GetComponent<RectTransform>();
        winRect.offsetMin = Vector2.zero;
        winRect.offsetMax = Vector2.zero;
        winPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        GameObject winBox = CreatePanel(winPanel.transform, "WinBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(700, 400), Vector2.zero);
        winBox.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        winText = CreateText(winBox.transform, "WinText", "Oyuncu 1 Kazandı!",
            52, FontStyle.Bold, Color.white,
            new Vector2(0, 0.4f), new Vector2(1, 0.9f), new Vector2(0.5f, 0.5f),
            new Vector2(20, 0), new Vector2(-20, 0));

        CreateText(winBox.transform, "CongratsText", "Tebrikler!",
            36, FontStyle.Normal, new Color(1f, 0.85f, 0.3f),
            new Vector2(0, 0.25f), new Vector2(1, 0.45f), new Vector2(0.5f, 0.5f),
            new Vector2(20, 0), new Vector2(-20, 0));

        GameObject btnObj = CreatePanel(winBox.transform, "RestartButton",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(350, 70), new Vector2(0, 50));
        btnObj.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f, 1f);

        restartButton = btnObj.AddComponent<Button>();
        restartButton.onClick.AddListener(OnRestartClicked);

        ColorBlock colors = restartButton.colors;
        colors.highlightedColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        colors.pressedColor = new Color(0.15f, 0.55f, 0.25f, 1f);
        restartButton.colors = colors;

        CreateText(btnObj.transform, "RestartBtnText", "Yeniden Başla",
            30, FontStyle.Bold, Color.white,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        winPanel.SetActive(false);
    }

    private void CreateInstructions(Transform parent)
    {
        // Kullanıcı isteği üzerine ekrandaki gereksiz talimat yazıları kaldırıldı
        // (Dokun: Mıknatıs Yerleştir, 2 Parmak: Kamera vs.)
    }

    // ─── Köşe Butonları ─────────────────────────────────────────────────────
    private GameObject settingsPopup;
    private Text       soundBtnText;
    private bool       soundEnabled = true;

    private void CreateCornerButtons(Transform parent)
    {
        // ── SOL ALT: UNDO butonu ──────────────────────────────
        GameObject undoBtn = new GameObject("UndoButton");
        undoBtn.transform.SetParent(parent, false);
        undoBtn.layer = 5; // UI Layer

        Image undoBg = undoBtn.AddComponent<Image>();
        undoBg.color = new Color(0.15f, 0.18f, 0.22f, 0.92f);
        undoBg.raycastTarget = true; // Tıklanabilir olmalı!

        Button undoB = undoBtn.AddComponent<Button>();

        RectTransform ur = undoBtn.GetComponent<RectTransform>();
        ur.anchorMin = ur.anchorMax = new Vector2(0f, 0f);
        ur.pivot     = new Vector2(0f, 0f);
        // Biraz daha sola ve aşağıya çekildi, boyutu küçültüldü
        ur.anchoredPosition = new Vector2(15, 15);
        ur.sizeDelta        = new Vector2(120, 50);

        CreateInlineText(undoBtn.transform, "↩ UNDO", 22, new Color(0.75f, 0.75f, 0.85f));
        AddOutline(undoBtn, new Color(0.3f, 0.45f, 0.65f));
        undoB.onClick.AddListener(OnUndoClicked);

        // ── SAĞ ALT: SETTINGS butonu ─────────────────────────
        GameObject settBtn = new GameObject("SettingsButton");
        settBtn.transform.SetParent(parent, false);
        settBtn.layer = 5; // UI Layer

        Image settBg = settBtn.AddComponent<Image>();
        settBg.color = new Color(0.15f, 0.18f, 0.22f, 0.92f);
        settBg.raycastTarget = true;

        Button settB = settBtn.AddComponent<Button>();

        RectTransform sr = settBtn.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = new Vector2(1f, 0f);
        sr.pivot     = new Vector2(1f, 0f);
        // Biraz daha sağa ve aşağıya çekildi, boyutu küçültüldü
        sr.anchoredPosition = new Vector2(-15, 15);
        sr.sizeDelta        = new Vector2(140, 50);

        CreateInlineText(settBtn.transform, "⚙ SETTINGS", 20, new Color(0.75f, 0.85f, 0.75f));
        AddOutline(settBtn, new Color(0.3f, 0.65f, 0.45f));
        settB.onClick.AddListener(OnSettingsClicked);

        // ── Settings Popup ────────────────────────────────────
        CreateSettingsPopup(parent);
    }

    private void CreateSettingsPopup(Transform parent)
    {
        // Yarı şeffaf koyu overlay
        settingsPopup = new GameObject("SettingsPopup");
        settingsPopup.transform.SetParent(parent, false);
        Image bg = settingsPopup.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.96f);

        RectTransform pr = settingsPopup.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot     = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;
        pr.sizeDelta = new Vector2(700, 820);
        AddOutline(settingsPopup, new Color(0.2f, 0.65f, 0.4f));

        // Başlık
        CreateInlineText(settingsPopup.transform, "⚙  AYARLAR", 52, Color.white, new Vector2(0.5f,1f), new Vector2(0,-70));

        // Neon ayırıcı çizgi
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(settingsPopup.transform, false);
        Image div = divider.AddComponent<Image>();
        div.color = new Color(0.2f, 0.65f, 0.4f, 0.8f);
        RectTransform dr = divider.GetComponent<RectTransform>();
        dr.anchorMin = new Vector2(0.1f,1f); dr.anchorMax = new Vector2(0.9f,1f);
        dr.pivot = new Vector2(0.5f,1f); dr.anchoredPosition = new Vector2(0,-110);
        dr.sizeDelta = new Vector2(0, 3);

        // Ses Açık/Kapalı
        CreateSettingsRow(settingsPopup.transform, "🔊  Ses",
            new Vector2(0.5f, 1f), new Vector2(0, -160),
            "Açık", OnSoundToggle, ref soundBtnText);

        // Süre seçme
        CreateSettingsRowLabel(settingsPopup.transform,
            "⏱  Süre Limiti: 10 sn",
            new Vector2(0.5f, 1f), new Vector2(0, -330));
        CreateTimeButtons(settingsPopup.transform, new Vector2(0, -400));

        // Yeniden Başla
        CreateSettingsBigButton(settingsPopup.transform,
            "🔄  Yeni Oyun", new Vector2(0.5f, 0f), new Vector2(0, 160),
            new Color(0.2f, 0.6f, 0.95f), OnNewGameFromSettings);

        // Kapat
        CreateSettingsBigButton(settingsPopup.transform,
            "✕  KAPAT", new Vector2(0.5f, 0f), new Vector2(0, 70),
            new Color(0.75f, 0.2f, 0.2f), () => settingsPopup.SetActive(false));

        settingsPopup.SetActive(false);
    }

    private Text timerLabelText; // süre label referansı

    private void CreateSettingsRow(Transform parent, string label,
        Vector2 anchor, Vector2 pos, string btnLabel,
        UnityEngine.Events.UnityAction callback, ref Text btnTextRef)
    {
        // Label
        GameObject lbl = new GameObject("RowLabel");
        lbl.transform.SetParent(parent, false);
        Text lt = lbl.AddComponent<Text>();
        lt.font = GetDefaultFont(); lt.text = label;
        lt.fontSize = 36; lt.color = new Color(0.85f, 0.85f, 0.85f);
        lt.alignment = TextAnchor.MiddleCenter;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = lr.anchorMax = anchor; lr.pivot = new Vector2(0.5f, 0.5f);
        lr.anchoredPosition = pos; lr.sizeDelta = new Vector2(600, 60);

        // Buton
        GameObject btn = new GameObject("RowBtn");
        btn.transform.SetParent(parent, false);
        Image bi = btn.AddComponent<Image>(); bi.color = new Color(0.2f, 0.55f, 0.3f);
        Button b = btn.AddComponent<Button>(); b.onClick.AddListener(callback);
        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = anchor; br.pivot = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = new Vector2(pos.x, pos.y - 70); br.sizeDelta = new Vector2(280, 60);

        GameObject btnTxtObj = new GameObject("BtnTxt");
        btnTxtObj.transform.SetParent(btn.transform, false);
        Text bt = btnTxtObj.AddComponent<Text>();
        bt.font = GetDefaultFont(); bt.text = btnLabel;
        bt.fontSize = 30; bt.fontStyle = FontStyle.Bold;
        bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;
        RectTransform btr = btnTxtObj.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        btnTextRef = bt;
    }

    private void CreateSettingsRowLabel(Transform parent, string text, Vector2 anchor, Vector2 pos)
    {
        GameObject lbl = new GameObject("RowLabel2");
        lbl.transform.SetParent(parent, false);
        Text lt = lbl.AddComponent<Text>();
        lt.font = GetDefaultFont(); lt.text = text;
        lt.fontSize = 34; lt.color = new Color(0.85f, 0.85f, 0.85f);
        lt.alignment = TextAnchor.MiddleCenter;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = lr.anchorMax = anchor; lr.pivot = new Vector2(0.5f, 0.5f);
        lr.anchoredPosition = pos; lr.sizeDelta = new Vector2(600, 60);
        timerLabelText = lt;
    }

    // Seçili zaman buton renkleri için referans listesi
    private Image[] timeBtnImages;
    private int     selectedTimeIndex = 1; // varsayılan: 10s (index 1)

    private void CreateTimeButtons(Transform parent, Vector2 center)
    {
        int[] times = { 5, 10, 15, 20 };
        float spacing = 148f;
        float startX  = -spacing * (times.Length - 1) / 2f;

        timeBtnImages = new Image[times.Length];

        for (int i = 0; i < times.Length; i++)
        {
            int t   = times[i];
            int idx = i;

            GameObject btn = new GameObject($"TimeBtn_{t}");
            btn.transform.SetParent(parent, false);

            Image bi = btn.AddComponent<Image>();
            bi.color = (t == 10)
                ? new Color(0.2f, 0.65f, 1f)    // varsayılan seçili
                : new Color(0.22f, 0.25f, 0.30f);
            timeBtnImages[i] = bi; // referansı sakla!

            Button b = btn.AddComponent<Button>();
            b.onClick.AddListener(() => OnTimeSelected(t, idx));

            RectTransform br = btn.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = new Vector2(startX + i * spacing, center.y);
            br.sizeDelta = new Vector2(128, 64);

            // Seçili ise beyaz metin, diğerleri gri
            Color tc = (t == 10) ? Color.white : new Color(0.65f, 0.65f, 0.65f);
            CreateInlineText(btn.transform, $"{t}s", 30, tc);
            AddOutline(btn, (t == 10)
                ? new Color(0.1f, 0.5f, 0.9f)
                : new Color(0.15f, 0.17f, 0.20f));
        }
    }

    private void CreateSettingsBigButton(Transform parent, string label,
        Vector2 anchor, Vector2 pos, Color color,
        UnityEngine.Events.UnityAction callback)
    {
        GameObject btn = new GameObject("BigBtn");
        btn.transform.SetParent(parent, false);
        Image bi = btn.AddComponent<Image>(); bi.color = color;
        Button b = btn.AddComponent<Button>(); b.onClick.AddListener(callback);
        AddOutline(btn, Color.black);

        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = anchor; br.pivot = new Vector2(0.5f, 0f);
        br.anchoredPosition = pos; br.sizeDelta = new Vector2(520, 80);

        CreateInlineText(btn.transform, label, 32, Color.white);
    }

    private void CreateInlineText(Transform parent, string text, int size, Color color,
        Vector2? anchor = null, Vector2? offset = null)
    {
        GameObject g = new GameObject("InlineText");
        g.transform.SetParent(parent, false);
        Text t = g.AddComponent<Text>();
        t.font = GetDefaultFont(); t.text = text;
        t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter;
        
        // YENİ: Eğer butonda bir yazıysa tıklamayı engellemesin (arkadaki butona gitsin)
        if (parent != null && parent.GetComponent<Button>() != null)
        {
            t.raycastTarget = false;
        }

        RectTransform r = g.GetComponent<RectTransform>();
        if (anchor.HasValue && offset.HasValue)
        {
            r.anchorMin = r.anchorMax = anchor.Value;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = offset.Value;
            r.sizeDelta = new Vector2(600, 80);
        }
        else
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
        
        g.layer = 5; // UI Layer
    }

    private static void AddOutline(GameObject obj, Color color)
    {
        Outline ol = obj.AddComponent<Outline>();
        ol.effectColor = color;
        ol.effectDistance = new Vector2(2, -2);
    }

    // ─── Settings Callbacks ────────────────────────────────────────────────
    private void OnUndoClicked()
    {
        Debug.Log("[UIManager] UNDO butonu tiklandi!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UndoLastMove();
        }
    }

    private void OnSettingsClicked()
    {
        if (settingsPopup != null)
            settingsPopup.SetActive(!settingsPopup.activeSelf);
    }

    private void OnSoundToggle()
    {
        soundEnabled = !soundEnabled;
        if (soundBtnText != null)
            soundBtnText.text = soundEnabled ? "Açık" : "Kapalı";
        AudioListener.volume = soundEnabled ? 1f : 0f;
    }

    private void OnTimeSelected(int seconds, int index)
    {
        // GameManager'a uygula (anlık değişir)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.maxTurnTime = seconds;
            // Eğer oyun şu an oynanıyorsa zamanlayıcı da güncellenir
        }

        // Label güncelle
        if (timerLabelText != null)
            timerLabelText.text = $"⏱  Süre Limiti: {seconds} sn";

        // Tüm butonların renklerini güncelle: sadece seçili mavi, geri kalanlar gri
        selectedTimeIndex = index;
        if (timeBtnImages != null)
        {
            for (int i = 0; i < timeBtnImages.Length; i++)
            {
                if (timeBtnImages[i] == null) continue;
                bool isSelected = (i == index);
                timeBtnImages[i].color = isSelected
                    ? new Color(0.2f, 0.65f, 1f)    // Parlak mavi = SEÇILI
                    : new Color(0.22f, 0.25f, 0.30f); // Koyu gri = seçili değil

                // Kenarlık outline rengi de değişsin
                Outline ol = timeBtnImages[i].GetComponent<Outline>();
                if (ol != null)
                    ol.effectColor = isSelected
                        ? new Color(0.1f, 0.5f, 0.9f)
                        : new Color(0.15f, 0.17f, 0.20f);
            }
        }
    }

    private void OnNewGameFromSettings()
    {
        settingsPopup.SetActive(false);
        // İn-game UI'ı gizle, menüye dön
        canvasObj?.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.currentState = GameManager.GameState.MainMenu;
        MainMenuManager menu = Object.FindAnyObjectByType<MainMenuManager>();
        menu?.ShowMenu();
    }

    // ========== EVENT HANDLERS ==========

    private void SubscribeToEvents()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnTurnChanged += OnTurnChanged;
        GameManager.Instance.OnMagnetsUpdated += OnMagnetsUpdated;
        GameManager.Instance.OnScoresUpdated += OnScoresUpdated;
        GameManager.Instance.OnStatusMessage += OnStatusMessage;
        GameManager.Instance.OnGameOver += OnGameOver;
        GameManager.Instance.OnTimerUpdated += OnTimerUpdated;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnTurnChanged -= OnTurnChanged;
        GameManager.Instance.OnMagnetsUpdated -= OnMagnetsUpdated;
        GameManager.Instance.OnScoresUpdated -= OnScoresUpdated;
        GameManager.Instance.OnStatusMessage -= OnStatusMessage;
        GameManager.Instance.OnGameOver -= OnGameOver;
        GameManager.Instance.OnTimerUpdated -= OnTimerUpdated;
    }

    private void OnTurnChanged(int player)
    {
        if (turnText != null)
        {
            if (player == 1)
            {
                turnText.text = "YOUR TURN!";
                turnText.color = new Color(0.2f, 0.9f, 0.9f); // Cyan
            }
            else
            {
                turnText.text = "BOT'S TURN!";
                turnText.color = new Color(1f, 0.4f, 0.2f); // Orange/Red
            }
            // turnIndicatorBg artık yok kullanmıyoruz
        }

        if (player1Panel != null && player2Panel != null)
        {
            // Arkaplan hep gizli
            player1Panel.color = new Color(0, 0, 0, 0);
            player2Panel.color = new Color(0, 0, 0, 0);

            // Metinleri karartarak sırayı belli et
            if (player == 1)
            {
                player1MagnetsText.color = new Color(1f, 1f, 1f, 1f);
                player2MagnetsText.color = new Color(1f, 1f, 1f, 0.4f);
            }
            else
            {
                player1MagnetsText.color = new Color(1f, 1f, 1f, 0.4f);
                player2MagnetsText.color = new Color(1f, 1f, 1f, 1f);
            }
        }
    }

    private void OnMagnetsUpdated(int p1Count, int p2Count)
    {
        if (player1MagnetsText != null)
            player1MagnetsText.text = $"PLAYER 1 (YOU): {p1Count} STONES LEFT";
        if (player2MagnetsText != null)
            player2MagnetsText.text = $"PLAYER 2 (BOT): {p2Count} STONES LEFT";
    }

    private void OnScoresUpdated(int p1Score, int p2Score)
    {
        if (player1ScoreText != null)
            player1ScoreText.text = $"SCORE: {p1Score}";
        if (player2ScoreText != null)
            player2ScoreText.text = $"SCORE: {p2Score}";
    }

    private void OnStatusMessage(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void OnTimerUpdated(float normalizedTime)
    {
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = normalizedTime;
            
            // Renk değiştir (Süre azaldıkça kırmızıya dön)
            timerBarFill.color = Color.Lerp(Color.red, Color.green, normalizedTime);
            
            // Yazıyı güncelle
            if (timerText != null)
            {
                float maxSeconds = GameManager.Instance != null ? GameManager.Instance.maxTurnTime : 10f;
                float seconds = normalizedTime * maxSeconds; 
                timerText.text = seconds.ToString("F1") + "s";
            }
        }
    }

    private void OnGameOver(int winner)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Color winColor = GameManager.Instance.GetPlayerColor(winner);
            if (winText != null)
            {
                winText.text = $"Oyuncu {winner} Kazandi!";
                winText.color = winColor;
            }
        }
    }

    private void OnRestartClicked()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        MagnetPiece.ClearAllMagnets();

        PlacementController pc = Object.FindAnyObjectByType<PlacementController>();
        if (pc != null) pc.ResetPreview();

        // In-game UI'yı gizle ve Ana Menü'ye dön
        canvasObj?.SetActive(false);
        GameManager.Instance.currentState = GameManager.GameState.MainMenu;

        // Ana Menüyü tekrar aç
        MainMenuManager menu = Object.FindAnyObjectByType<MainMenuManager>();
        if (menu != null)
            menu.ShowMenu();
    }

    // ========== UI HELPER METHODS ==========

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        panel.layer = 5; // UI Layer
        return panel;
    }

    private Text CreateText(Transform parent, string name, string content,
        int fontSize, FontStyle style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        return text;
    }
}
