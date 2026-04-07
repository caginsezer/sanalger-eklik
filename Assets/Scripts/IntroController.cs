using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Oyun başında havalı bir "Manyetik Giriş" (Intro) sekansı yaratır.
/// </Dikey mobil uyumlu, yüksek kaliteli animasyonlu metinler ve 3D parçalar.
/// </summary>
public class IntroController : MonoBehaviour
{
    public static IntroController Instance { get; private set; }

    private GameObject introCanvasObj;
    private Canvas introCanvas;
    private Text mainMessageText;
    private Text subMessageText;
    private Image overlayBg;

    private List<GameObject> introFragments = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private System.Action cachedOnComplete;
    private bool introFinished = false;

    public void PlayIntro(int startingPlayer, System.Action onComplete)
    {
        cachedOnComplete = onComplete;
        introFinished = false;
        
        // WATCHDOG (Güvenlik Köpeği): Animasyon takılırsa maksimum 8 saniye sonra oyunu zorla başlat
        Invoke(nameof(ForceComplete), 8.0f);
        
        StartCoroutine(IntroSequence(startingPlayer));
    }

    private void ForceComplete()
    {
        if (introFinished) return;
        Debug.LogWarning("[Watchdog] Intro timeout tetiklendi. Siyah ekranı geçmek için oyun zorla başlatılıyor!");
        introFinished = true;
        ClearFragments();
        if (introCanvasObj != null) introCanvasObj.SetActive(false);
        cachedOnComplete?.Invoke();
    }

    private IEnumerator IntroSequence(int startingPlayer)
    {
        BuildIntroCanvas();
        
        // 1. PHASE: Karanlık Başlangıç ve Parçalar
        overlayBg.color = new Color(0.05f, 0.05f, 0.08f, 0f);
        float t = 0f;
        while (t < 0.5f) {
            t += Time.deltaTime;
            overlayBg.color = new Color(0.05f, 0.05f, 0.08f, Mathf.SmoothStep(0f, 0.85f, t / 0.5f));
            yield return null;
        }

        // 3D Parçaları Oluştur (Mıknatıs parçaları havada süzülür)
        CreateFragments();
        
        // 2. PHASE: "HAZIR OL" Mesajı
        mainMessageText.text = "HAZIR OL...";
        mainMessageText.color = new Color(1, 1, 1, 0);
        mainMessageText.rectTransform.localScale = Vector3.one * 0.5f;

        // SES: Ufak bir yükselen ses (GameStart'tan önce)
        AudioManager.Instance?.Play(AudioManager.SoundType.TurnChange);

        t = 0f;
        while (t < 0.5f) {
            t += Time.deltaTime;
            float p = t / 0.5f;
            mainMessageText.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, p));
            mainMessageText.rectTransform.localScale = Vector3.one * Mathf.SmoothStep(0.5f, 1.1f, p);
            AnimateFragments(p * 0.4f);
            yield return null;
        }

        // 3. PHASE: "OYUNCU X BAŞLIYOR" - Duraksamadan geçiş
        Color pColor = (startingPlayer == 1) ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.2f, 0.1f);
        string pName = (startingPlayer == 1) ? "MAVİ" : "KIRMIZI";

        subMessageText.text = $"{pName} BAŞLIYOR!";
        subMessageText.color = new Color(pColor.r, pColor.g, pColor.b, 0f);
        subMessageText.rectTransform.localScale = Vector3.one * 0.5f;

        // SES: Ana başlama efekti (Hemen başlasın)
        AudioManager.Instance?.Play(AudioManager.SoundType.GameStart);

        t = 0f;
        while (t < 0.7f) {
            t += Time.deltaTime;
            float p = t / 0.7f;
            
            // Üstteki metni yavaşça silerken alttakini parlat
            mainMessageText.color = new Color(1, 1, 1, 1f - p * 2f);
            subMessageText.rectTransform.localScale = Vector3.one * Mathf.SmoothStep(0.5f, 1.1f, p);
            subMessageText.color = new Color(pColor.r, pColor.g, pColor.b, Mathf.SmoothStep(0, 1, p * 2));
            
            AnimateFragments(0.4f + p * 0.6f);
            yield return null;
        }

        // 4. PHASE: TEMİZLİK VE GEÇİŞ
        t = 0f;
        while (t < 0.5f) {
            t += Time.deltaTime;
            float p = t / 0.5f;
            overlayBg.color = new Color(0.05f, 0.05f, 0.08f, Mathf.Lerp(0.85f, 0, p));
            subMessageText.color = new Color(pColor.r, pColor.g, pColor.b, 1f - p);
            yield return null;
        }

        // Eğer Watchdog zaten bitirmişse, tekrar çalıştırma
        if (!introFinished)
        {
            introFinished = true;
            CancelInvoke(nameof(ForceComplete)); // Watchdog'u iptal et
            ClearFragments();
            introCanvasObj.SetActive(false);
            cachedOnComplete?.Invoke();
        }
    }

    private void BuildIntroCanvas()
    {
        if (introCanvasObj != null) {
            introCanvasObj.SetActive(true);
            return;
        }

        introCanvasObj = new GameObject("IntroCanvas");
        introCanvas = introCanvasObj.AddComponent<Canvas>();
        introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        introCanvas.sortingOrder = 100; // En üstte

        CanvasScaler scaler = introCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        // Overlay Arkaplan
        GameObject bgObj = new GameObject("IntroOverlay");
        bgObj.transform.SetParent(introCanvasObj.transform, false);
        overlayBg = bgObj.AddComponent<Image>();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Ana Metin ("HAZIR OL")
        mainMessageText = CreateUIText("MainMessage", 120, FontStyle.Bold, Color.white);
        mainMessageText.rectTransform.anchoredPosition = new Vector2(0, 100);

        // Alt Metin ("OYUNCU X BAŞLIYOR")
        subMessageText = CreateUIText("SubMessage", 90, FontStyle.Bold, Color.white);
        subMessageText.rectTransform.anchoredPosition = new Vector2(0, -50);
        
        // Gölge efekti (shadow) için dekoratif dokunuşlar
        mainMessageText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(8, -8);
        subMessageText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -6);
    }

    private Text CreateUIText(string name, int size, FontStyle style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(introCanvasObj.transform, false);
        Text t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000, 300);
        return t;
    }

    private void CreateFragments()
    {
        ClearFragments();
        for (int i = 0; i < 20; i++)
        {
            GameObject frag = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
            frag.name = "IntroFragment_" + i;
            frag.transform.position = new Vector3(Random.Range(-5f, 5f), 8f, Random.Range(-5f, 5f));
            frag.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
            Destroy(frag.GetComponent<Collider>());

            MeshRenderer r = frag.GetComponent<MeshRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material m = (s != null) ? new Material(s) : null;
            if (m != null)
            {
                Color c = (Random.value > 0.5f) ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.2f, 0.1f);
                m.SetColor("_BaseColor", c);
                m.SetColor("_EmissionColor", c * 2f);
                m.EnableKeyword("_EMISSION");
                r.material = m;
            }

            introFragments.Add(frag);
        }
    }

    private void AnimateFragments(float progress)
    {
        for (int i = 0; i < introFragments.Count; i++)
        {
            if (introFragments[i] == null) continue;
            // Merkeze doğru manyetik çekim spirali
            float angle = (i * 15f) + (progress * 360f);
            float rad = (5f * (1f - progress)) + (Mathf.Sin(i + progress * 5f) * 0.5f);
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * rad;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * rad;
            float y = 5f * (1f - progress) + 0.5f;

            introFragments[i].transform.position = new Vector3(x, y, z);
            introFragments[i].transform.Rotate(Vector3.up * 180f * Time.deltaTime);
        }
    }

    private void ClearFragments()
    {
        foreach (var f in introFragments) if (f != null) Destroy(f);
        introFragments.Clear();
    }
}
