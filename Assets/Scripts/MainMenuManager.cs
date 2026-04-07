using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    private GameObject menuCanvasObj;
    private Canvas menuCanvas;
    
    // Animating Title Letters
    private List<RectTransform> animLetters = new List<RectTransform>();
    private List<Vector2> originalPositions = new List<Vector2>();
    private float timeElapsed = 0f;

    private GameObject rulesPopup;

    public void Initialize()
    {
        BuildMenuCanvas();
        ShowMenu(); // Menü seslerini ve görünürlüğünü başlat
    }

    public void ShowMenu()
    {
        if (menuCanvasObj != null)
            menuCanvasObj.SetActive(true);
        else
            BuildMenuCanvas();

        // Menü ambiyans sesini başlat
        AudioManager.Instance?.Play(AudioManager.SoundType.MenuAmbiance);
    }

    private void BuildMenuCanvas()
    {
        menuCanvasObj = new GameObject("MainMenuCanvas");
        menuCanvas = menuCanvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceCamera; // Xiaomi ve Yüksek Çözünürlüklü Cihazlar İçin Güvenli Mod
        menuCanvas.worldCamera = Camera.main;
        menuCanvas.planeDistance = 0.5f; 
        menuCanvas.sortingOrder = 150; // In-game HUD (100) üstünde
 
        CanvasScaler scaler = menuCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280); 
        scaler.matchWidthOrHeight = 0.5f;
        menuCanvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem garantisi
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 2. Koyu Arkaplan (Overlay)
        GameObject bgObj = new GameObject("MenuOverlay");
        bgObj.transform.SetParent(menuCanvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f); // Koyu siyahımsı şeffaf
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 3. Animasyonlu Ana Başlık (MAGNETIC)
        CreateAnimatedTitle("MAGNETIC", new Vector2(0, 400));
        
        // Klasik Alt Başlık (MAYHEM)
        GameObject subTitleObj = CreateTextObj("MAYHEM", 48, FontStyle.Bold, new Color(1f, 0.4f, 0.1f));
        SetAnchorsAndOffset(subTitleObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300));
        // Alt çizgi dekor
        GameObject lineInfo = new GameObject("SubLine");
        lineInfo.transform.SetParent(menuCanvasObj.transform, false);
        Image lineImg = lineInfo.AddComponent<Image>();
        lineImg.color = new Color(1f, 0.5f, 0f, 0.8f);
        SetAnchorsAndOffset(lineInfo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 270), new Vector2(250, 4));

        // 4. Play Butonu ("OYUNA BAŞLA")
        GameObject playBtnObj = CreateButton("PlayButton", "▶ OYUNA BAŞLA", 50, new Color(1f, 0.7f, 0.1f), new Color(0.1f, 0.1f, 0.1f));
        SetAnchorsAndOffset(playBtnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(600, 120));
        playBtnObj.GetComponent<Button>().onClick.AddListener(OnPlayClicked);

        // 5. Rules Butonu ("KURALLAR")
        GameObject rulesBtnObj = CreateButton("RulesButton", "KURALLAR", 35, new Color(0.2f, 0.8f, 0.8f), new Color(0.1f, 0.1f, 0.1f));
        SetAnchorsAndOffset(rulesBtnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(400, 80));
        rulesBtnObj.GetComponent<Button>().onClick.AddListener(OnRulesClicked);

        CreateRulesPopup();
    }

    private void CreateAnimatedTitle(string word, Vector2 centerOffset)
    {
        // Harfleri ayrı ayrı yarat
        float letterSpacing = 85f;
        float totalWidth = (word.Length - 1) * letterSpacing;
        float startX = -totalWidth / 2f;

        animLetters.Clear();
        originalPositions.Clear();

        for (int i = 0; i < word.Length; i++)
        {
            float posX = startX + i * letterSpacing;
            string letter = word[i].ToString();

            GameObject letterObj = CreateTextObj(letter, 110, FontStyle.Bold, Color.white);
            RectTransform rect = letterObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            // Konumlandır
            Vector2 finalPos = new Vector2(posX, centerOffset.y);
            rect.anchoredPosition = finalPos;
            rect.sizeDelta = new Vector2(100, 150);

            // Rastgele Renk Efekti (Gradientimsi - Beyaz ve sarımtrak/turuncu geçiş)
            float t = (float)i / (word.Length - 1); // 0 to 1
            Color col = Color.Lerp(Color.white, new Color(1f, 0.8f, 0f), t);
            // Son harfleri daha turuncu yap
            if (i > word.Length - 3) col = new Color(1f, 0.5f, 0.1f);
            
            letterObj.GetComponent<Text>().color = col;

            // Gölge ekle
            Shadow shadow = letterObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(6, -6);

            animLetters.Add(rect);
            originalPositions.Add(finalPos);
        }
    }

    private void Update()
    {
        if (animLetters.Count == 0 || menuCanvasObj == null || !menuCanvasObj.activeSelf) return;

        timeElapsed += Time.deltaTime * 3f; // Animasyon hızı

        for (int i = 0; i < animLetters.Count; i++)
        {
            // Dalga (Sine wave) hareketi - offseti var
            float wave = Mathf.Sin(timeElapsed + (i * 0.7f)) * 25f; // Zıplama yüksekliği
            
            // X ekseninde de hafif bir sallanma eklenebilir ama sade dikey zıplama şıktır
            animLetters[i].anchoredPosition = originalPositions[i] + new Vector2(0, wave);
        }
    }

    private Font GetMenuFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        // Android/Unity 6+ için garanti çözüm: OS Font names içinden ilkini çek
        if (font == null)
        {
            try {
                string[] osFonts = Font.GetOSInstalledFontNames();
                if (osFonts.Length > 0) font = Font.CreateDynamicFontFromOSFont(osFonts[0], 16);
            } catch {}
        }
        
        // Hâlâ null ise (çok nadir), Arial olarak zorla
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        
        return font;
    }

    private GameObject CreateTextObj(string content, int fontSize, FontStyle style, Color color)
    {
        GameObject txtObj = new GameObject("TextObj");
        txtObj.transform.SetParent(menuCanvasObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = GetMenuFont();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        return txtObj;
    }

    private void SetAnchorsAndOffset(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size = default)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        if (size != default) rect.sizeDelta = size;
        else rect.sizeDelta = new Vector2(800, 200); // Default genişlik
    }

    private GameObject CreateButton(string objName, string textContent, int fontSize, Color bgColor, Color textColor)
    {
        GameObject btnObj = new GameObject(objName);
        btnObj.transform.SetParent(menuCanvasObj.transform, false);
        
        // Background
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        // Button Logic
        Button btn = btnObj.AddComponent<Button>();
        
        // Text child
        GameObject txtObj = CreateTextObj(textContent, fontSize, FontStyle.Bold, textColor);
        txtObj.transform.SetParent(btnObj.transform, false);
        SetAnchorsAndOffset(txtObj, Vector2.zero, Vector2.one, Vector2.zero); // Fill

        return btnObj;
    }

    private void CreateRulesPopup()
    {
        rulesPopup = new GameObject("RulesPopup");
        rulesPopup.transform.SetParent(menuCanvasObj.transform, false);
        Image bgImg = rulesPopup.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.98f); // Koyu panel
        SetAnchorsAndOffset(rulesPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 1000));
        
        // Çerçeve/Outline
        Outline outline = rulesPopup.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 0.8f);
        outline.effectDistance = new Vector2(3, -3);

        // Title
        GameObject title = CreateTextObj("KURALLAR", 55, FontStyle.Bold, Color.white);
        title.transform.SetParent(rulesPopup.transform, false);
        SetAnchorsAndOffset(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(600, 100));

        // Kurallar Metni
        string rulesBody = 
        "1. Her oyuncunun kendine ait taşları vardır.\n\n" +
        "2. Sırası gelen oyuncu, kendi rezervinden bir taşı ortadaki masaya bırakır.\n\n" +
        "3. Süre sınırı vardır, süre dolarsa taş havaya uçar ve sıranı kaybedersin.\n\n" +
        "4. Mıknatısları birbirine değdirmeden, stratejik yerleştir! Eğer taşları yapıştırırsan, hepsi geri döner ve eksi yersin.\n\n" +
        "5. Elindeki taşları ilk bitiren kazanır!";

        GameObject body = CreateTextObj(rulesBody, 36, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));
        body.transform.SetParent(rulesPopup.transform, false);
        Text bodyTxt = body.GetComponent<Text>();
        bodyTxt.alignment = TextAnchor.UpperLeft;
        SetAnchorsAndOffset(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0, -60), new Vector2(0, 0)); // Top offseti title'dan düşerek
        // Anchor fill custom offsets:
        RectTransform rect = body.GetComponent<RectTransform>();
        rect.offsetMin = new Vector2(40, 120); // bottom ve left padding
        rect.offsetMax = new Vector2(-40, -180); // top ve right padding

        // Kapat Butonu
        GameObject closeBtn = CreateButton("CloseRules", "KAPAT", 40, new Color(0.8f, 0.2f, 0.2f), Color.white);
        closeBtn.transform.SetParent(rulesPopup.transform, false);
        SetAnchorsAndOffset(closeBtn, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 80));
        closeBtn.GetComponent<Button>().onClick.AddListener(() => { rulesPopup.SetActive(false); });

        rulesPopup.SetActive(false); // Başlangıçta gizli
    }

    private void OnPlayClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.ButtonClick);
        AudioManager.Instance?.StopAmbiance();
        menuCanvasObj.SetActive(false);

        // KESİN ÇÖZÜM: Watchdog destekli, Shader'ları garantili Intro animasyonunu Başlat
        if (IntroController.Instance != null && GameManager.Instance != null)
        {
            IntroController.Instance.PlayIntro(GameManager.Instance.currentPlayer, () => {
                GameManager.Instance.StartGameFromMenu();
            });
        }
        else
        {
            // Eğer hiyerarşide Intro nesnesi bulunamazsa oyunu başlat 
            GameManager.Instance?.StartGameFromMenu();
        }
    }

    private void OnRulesClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.ButtonClick);
        if (rulesPopup != null)
        {
            rulesPopup.SetActive(true);
        }
    }
}
