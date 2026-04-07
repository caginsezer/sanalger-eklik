using UnityEngine;

/// <summary>
/// Sahne başlatıcı - Tüm sistemleri sırayla ayağa kaldırır.
/// Dikey (Portrait) mod ve güvenli shader yüklemeleri içerir.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        if (GameManager.Instance != null) return;

        Debug.Log("=== Miknatıs Oyunu Baslatiliyor ===");

        // 1. EventSystem (Tıklamalar için kritik)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Temel Sistemler (Managerlar)
        if (Object.FindAnyObjectByType<AudioManager>() == null)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        // IntroController artık opsiyonel, hata riskine karşı devredışı bırakıldı
        
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // 3. Kamera Kurulumu (TÜM mevcut kameraları temizle ve sıfırdan yarat)
        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in allCams) Object.DestroyImmediate(c.gameObject);
        
        GameObject camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        Camera mainCam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.28f, 0.16f, 0.07f); 
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 100f;
        mainCam.orthographic = true;
        mainCam.orthographicSize = 12.0f; // Geniş alan
        mainCam.transform.position = new Vector3(0, 15f, 0);
        mainCam.transform.rotation = Quaternion.Euler(90f, 0, 0);
        
        // Xiaomi/Adreno optimizasyonu (URP üzerinde Post-process kapat)
        var camData = camObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (camData == null) camData = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (camData != null) camData.renderPostProcessing = false;

        // Global Volume/Post-process temizliği (Kesin çözüm)
        try {
            var volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
            foreach (var vol in volumes) Object.DestroyImmediate(vol.gameObject);
        } catch {}

        // 4. Kamera Kontrolcüsü
        CameraController camController = mainCam.gameObject.GetComponent<CameraController>();
        if (camController == null) camController = mainCam.gameObject.AddComponent<CameraController>();
        camController.SetupExistingCamera(mainCam);

        // 5. Işıklandırma
        SetupLighting();

        // 6. Oyun Dünyası Bileşenleri
        GameObject boardObj = new GameObject("BoardController");
        BoardSetup boardSetup = boardObj.AddComponent<BoardSetup>();
        boardSetup.CreateBoard();

        GameObject reserveObj = new GameObject("StoneReserveManager");
        reserveObj.AddComponent<StoneReserveManager>();

        GameObject placementObj = new GameObject("PlacementController");
        PlacementController placement = placementObj.AddComponent<PlacementController>();
        placement.Initialize(boardSetup, mainCam);

        // 7. Arayüzler (UI)
        GameObject uiObj = new GameObject("UIManager");
        UIManager ui = uiObj.AddComponent<UIManager>();
        ui.Initialize();

        GameObject menuObj = new GameObject("MainMenuManager");
        MainMenuManager menu = menuObj.AddComponent<MainMenuManager>();
        menu.Initialize();

        // 8. Mobil ve Uygulama Ayarları
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("=== Sahne Kurulumu Tamamlandı (Dikey Mod) ===");
    }

    static void SetupLighting()
    {
        Light existingLight = Object.FindAnyObjectByType<Light>();
        if (existingLight == null)
        {
            existingLight = new GameObject("DirectionalLight").AddComponent<Light>();
        }

        existingLight.type = LightType.Directional;
        existingLight.color = new Color(1f, 0.95f, 0.85f);
        existingLight.intensity = 1.6f;
        existingLight.shadows = LightShadows.Soft;
        existingLight.shadowStrength = 1.0f;
        existingLight.transform.rotation = Quaternion.Euler(35f, -45f, 0f);

        GameObject fillLight = new GameObject("FillLight");
        Light fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.6f, 0.7f, 1f);
        fill.intensity = 0.5f;
        fillLight.transform.rotation = Quaternion.Euler(30, 150, 0);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
    }
}
