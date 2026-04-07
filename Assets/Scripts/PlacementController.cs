using UnityEngine;

/// <summary>
/// Taşı yan panelden sürükleyerek daire içine bırakma mekaniği.
/// Oyuncu 1 (sol panel) ve Oyuncu 2 (sağ panel) kendi taraflarından taş çeker.
/// </summary>
public class PlacementController : MonoBehaviour
{
    private BoardSetup boardSetup;
    private Camera mainCamera;
    private bool canPlace = true;

    private Texture2D handCursorTex;
    private GameObject handCursorObj;

    // Sürükleme durumu
    private MagnetPiece draggedStone = null;
    private bool isDragging = false;
    private int draggingPlayerID = 0;

    // Panel sınırları (oval dışı tıklama alanları)
    // Board radius Z = 4.9, yani sınır yaklaşık 5.0f
    private const float PANEL_THRESHOLD = 5.0f;

    public void Initialize(BoardSetup board, Camera camera)
    {
        boardSetup = board;
        mainCamera = camera;

        // El ikonunu yükle
        string texPath = System.IO.Path.Combine(Application.dataPath, "Textures", "hand_cursor.png");
        if (System.IO.File.Exists(texPath))
        {
            byte[] fd = System.IO.File.ReadAllBytes(texPath);
            handCursorTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            handCursorTex.LoadImage(fd);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentState != GameManager.GameState.WaitingForPlacement)
        {
            CancelDrag();
            return;
        }

        // Zaman aşımından dolayı sıra aniden değişirse (zaman bitmesi)
        if (isDragging && draggingPlayerID != GameManager.Instance.currentPlayer)
        {
            CancelDrag();
            return;
        }

        // Mobil/Editor input
        bool useTouchInput = false;
#if !UNITY_EDITOR
        if (Application.isMobilePlatform && Input.touchSupported)
            useTouchInput = true;
#endif
        if (useTouchInput)
            HandleTouch();
        else
            HandleMouse();
    }

    // ──────────────────────────────────────────────
    //  MOUSE
    // ──────────────────────────────────────────────
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            MoveDraggedStone(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            TryDropStone(Input.mousePosition);
        }
    }

    // ──────────────────────────────────────────────
    //  TOUCH
    // ──────────────────────────────────────────────
    private void HandleTouch()
    {
        if (Input.touchCount != 1) { CancelDrag(); return; }

        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
            TryStartDrag(t.position);
        else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isDragging)
            MoveDraggedStone(t.position);
        else if (t.phase == TouchPhase.Ended && isDragging)
            TryDropStone(t.position);
    }

    // ──────────────────────────────────────────────
    //  SÜRÜKLEME BAŞLAT
    //  Tıklama yeri oyuncunun panel tarafına düşüyorsa taşı al
    // ──────────────────────────────────────────────
    private void TryStartDrag(Vector2 screenPos)
    {
        // EventSystem UI kontrolü
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        int currentPlayer = GameManager.Instance.currentPlayer;

        // Rezervde taş var mı?
        if (StoneReserveManager.Instance == null || !StoneReserveManager.Instance.HasStones(currentPlayer))
            return;

        // ── 1) Raycast ile tıklanan taşı bul ──
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        GameObject hitStone = null;

        // QueryTriggerInteraction.Collide: Trigger collider'lara da çarpsın
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f, Physics.AllLayers, QueryTriggerInteraction.Collide);
        float closestDist = float.MaxValue;

        // Mevcut oyuncunun taş isim önekini kontrol et ("ReserveStone_P1_" veya "ReserveStone_P2_")
        string prefix = $"ReserveStone_P{currentPlayer}_";
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.name.StartsWith(prefix) && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                hitStone = hit.collider.gameObject;
            }
        }

        // ── 2a) Belirli bir taşa tıklandıysa: o taşı direkt sürükle ──
        if (hitStone != null)
        {
            // O taşı listeden çıkar ve yok et
            if (!StoneReserveManager.Instance.TryConsumeSpecificStone(currentPlayer, hitStone))
                return;

            // Tıklanan taşın dünya pozisyonundan yeni "sürükleme taşı" oluştur
            Vector3 spawnPos = new Vector3(hitStone.transform.position.x, 0.25f, hitStone.transform.position.z);
            draggedStone = MagnetPiece.CreateMagnet(spawnPos, currentPlayer, preview: false);
            draggedStone.isPlaced = false;

            Collider col = draggedStone.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else
        {
            // ── 2b) Raycast taşa çarpmadı: Bölge kontrolü ile fallback ──
            Vector3 worldPos = GetWorldPoint(screenPos);
            if (worldPos == Vector3.positiveInfinity) return;

            bool inPanel = (currentPlayer == 1 && worldPos.z < -PANEL_THRESHOLD) ||
                           (currentPlayer == 2 && worldPos.z >  PANEL_THRESHOLD);
            if (!inPanel) return;

            StoneReserveManager.Instance.TryConsumeStone(currentPlayer);

            Vector3 spawnPos = new Vector3(worldPos.x, 0.25f, worldPos.z);
            draggedStone = MagnetPiece.CreateMagnet(spawnPos, currentPlayer, preview: false);
            draggedStone.isPlaced = false;

            Collider col = draggedStone.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // ── El İkonu ──
        if (handCursorTex != null)
        {
            handCursorObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            handCursorObj.name = "DragHandCursor";
            Destroy(handCursorObj.GetComponent<Collider>());
            handCursorObj.transform.SetParent(draggedStone.transform, false);
            handCursorObj.transform.localPosition = new Vector3(0.35f, 0.4f, -0.35f);
            handCursorObj.transform.localRotation = Quaternion.Euler(90f, 0f, 45f);
            handCursorObj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material mat = new Material(unlitShader);
            mat.mainTexture = handCursorTex;
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3500;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            handCursorObj.GetComponent<MeshRenderer>().material = mat;
        }

        isDragging = true;
        draggingPlayerID = currentPlayer;

        // SES: Taşı alma sesi
        AudioManager.Instance?.Play(AudioManager.SoundType.StonePick);
    }

    // ──────────────────────────────────────────────
    //  SÜRÜKLEME SIRASINDAKİ HAREKET
    // ──────────────────────────────────────────────
    private void MoveDraggedStone(Vector2 screenPos)
    {
        if (draggedStone == null) { isDragging = false; return; }

        Vector3 worldPos = GetWorldPoint(screenPos);
        if (worldPos == Vector3.positiveInfinity) return;

        // Taşı imlecin altında tut
        draggedStone.transform.position = new Vector3(worldPos.x, 0.25f, worldPos.z);

        // Renk sinyali: içerideyse aktif, dışarıdaysa soluk kırmızı
        bool inside = boardSetup.IsWithinBounds(worldPos);
        MeshRenderer r = draggedStone.GetComponent<MeshRenderer>();
        if (r != null)
        {
            Color gl = inside ? GameManager.Instance.GetPlayerColor(GameManager.Instance.currentPlayer) * 1.5f
                              : Color.red * 0.5f;
            r.material.SetColor("_EmissionColor", gl);
        }

        // El ikonuna hafif pompalama (bouncing) efekti verelim
        if (handCursorObj != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 0.05f;
            handCursorObj.transform.localPosition = new Vector3(0.35f, 0.4f, -0.35f + bounce);
        }
    }

    // ──────────────────────────────────────────────
    //  BIRAKMA
    // ──────────────────────────────────────────────
    private void TryDropStone(Vector2 screenPos)
    {
        if (draggedStone == null) { isDragging = false; return; }

        Vector3 worldPos = GetWorldPoint(screenPos);
        Vector3 dropPos = new Vector3(worldPos.x, 0.2f, worldPos.z);

        if (worldPos != Vector3.positiveInfinity && boardSetup.IsWithinBounds(dropPos))
        {
            // Geçerli bırakma: yerleştir
            draggedStone.transform.position = dropPos;
            draggedStone.isPlaced = true;
            draggedStone.ownerPlayer = GameManager.Instance.currentPlayer;

            // Rigidbody dondur
            Rigidbody rb = draggedStone.GetComponent<Rigidbody>();
            if (rb == null) rb = draggedStone.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            // NOT: Taş zaten sürükleme başında rezervden çıkarıldı, tekrar silme

            // Oyun yöneticisine bildir
            GameManager.Instance.OnMagnetPlaced(draggedStone);

            // SES: Yerleştirme sesi
            AudioManager.Instance?.Play(AudioManager.SoundType.StonePlaced);
        }
        else
        {
            // Daire dışına bıraktı - taşı iptal et, rezerve geri koy
            int currentPlayer = GameManager.Instance.currentPlayer;
            Destroy(draggedStone.gameObject);
            StoneReserveManager.Instance?.ReturnStone(currentPlayer);
        }

        // Eli yokedelim
        if (handCursorObj != null) Destroy(handCursorObj);

        draggedStone = null;
        isDragging = false;
    }

    // ──────────────────────────────────────────────
    //  YARDIMCI: Ekran konumunu dünya düzlemine dönüştür
    // ──────────────────────────────────────────────
    private Vector3 GetWorldPoint(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.positiveInfinity;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.2f, 0));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.positiveInfinity;
    }

    private Vector3 GetBoardPosition(Vector2 screenPos) => GetWorldPoint(screenPos);

    private void CancelDrag()
    {
        if (handCursorObj != null) Destroy(handCursorObj);

        if (draggedStone != null)
        {
            // İptal edildi - taşı rezerve geri koy
            int currentPlayer = GameManager.Instance != null ? GameManager.Instance.currentPlayer : 0;
            Destroy(draggedStone.gameObject);
            draggedStone = null;
            if (currentPlayer > 0)
                StoneReserveManager.Instance?.ReturnStone(currentPlayer);
        }
        isDragging = false;
    }

    public void ResetPreview()
    {
        CancelDrag();
        canPlace = true;
    }
}
