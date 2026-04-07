using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mıknatıs parçası - Manyetik çekim fiziği (URP uyumlu)
/// </summary>
public class MagnetPiece : MonoBehaviour
{
    [Header("Manyetik Özellikler")]
    public float attractionRadius = 2.0f;
    public float magneticForce = 15f;
    public float maxMagneticForce = 40f;

    [Header("Durum")]
    public int ownerPlayer = 0;
    public bool isPlaced = false;
    public bool isPreview = false;

    private Rigidbody rb;
    private float fallTimer = 0f;
    private bool isGrounding = false;
    [HideInInspector] public MeshRenderer meshRenderer;
    private static List<MagnetPiece> allMagnets = new List<MagnetPiece>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // meshRenderer: CreateMagnet tarafından child quad'a set edilir, burada overwrite etme
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>(); // fallback
    }

    private void OnEnable()
    {
        if (!isPreview)
            allMagnets.Add(this);
    }

    private void OnDisable()
    {
        allMagnets.Remove(this);
    }

    public static void ClearAllMagnets()
    {
        allMagnets.Clear();
    }

    private float slideSpeed = 10.0f;
    private bool isAnimatingSlide = false;
    private Vector3 targetPosition;

    private void Update()
    {
        if (!isPlaced || isPreview) return;

        // YANLARDAN KAYMA ANIMASYONU (havadan düşme yok)
        if (isAnimatingSlide)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isAnimatingSlide = false;

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }
            }
        }
    }

    /// <summary>
    /// Taşı yan kenarden (oyuncunun tarafından) kaydırarak yerleştir
    /// player=1 sol kenardan, player=2 sağ kenardan gelir
    /// </summary>
    public void StartSlideAnimation(Vector3 target, int playerNumber)
    {
        targetPosition = target;

        // Oyuncuya göre başlangıç noktası (ovalın kenarında, aynı Z'de)
        float boardEdgeX = playerNumber == 1 ? -5.5f : 5.5f;
        Vector3 startPos = new Vector3(boardEdgeX, target.y, target.z);
        transform.position = startPos;

        isAnimatingSlide = true;
    }

    // Eski uyumluluk katmanı (varsa çağrılar için)
    public void StartFallAnimation(Vector3 target)
    {
        StartSlideAnimation(target, ownerPlayer);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isPlaced || isPreview) return;

        MagnetPiece otherMagnet = collision.gameObject.GetComponent<MagnetPiece>();
        if (otherMagnet != null && otherMagnet.isPlaced && !otherMagnet.isPreview)
        {
            Vector3 contactPoint = collision.contacts[0].point;
            // Yeni güçlü efekt — görünür yıldız patlaması
            CollisionBlastEffect.Spawn(contactPoint);
        }
    }

    private static void SpawnCollisionEffect(Vector3 position)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        GameObject fxRoot = new GameObject("CollisionFX");
        fxRoot.transform.position = position;

        // ── 1. MERKEZ FLAŞ (ani beyaz/cyan patlama) ──────────────────────
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "FlashCore";
        flash.transform.SetParent(fxRoot.transform);
        flash.transform.localPosition = Vector3.zero;
        flash.transform.localScale = Vector3.one * 0.35f;
        Destroy(flash.GetComponent<Collider>());
        {
            Material m = new Material(litShader);
            Color fc = new Color(0.5f, 1f, 1f); // parlak cyan-beyaz
            m.SetColor("_BaseColor", fc);
            m.SetColor("_EmissionColor", fc * 8f);
            m.EnableKeyword("_EMISSION");
            SetTransparent(m);
            flash.GetComponent<MeshRenderer>().material = m;
        }
        KinematicSparkMover flashMover = flash.AddComponent<KinematicSparkMover>();
        flashMover.velocity = Vector3.zero;
        flashMover.lifetime = 0.18f;
        flashMover.scaleToZero = true;
        flashMover.expandScale = 3.5f; // Anında büyüyüp söner

        // ── 2. ŞOK DALGASI HALKA (genişleyen düzlem halkası) ────────────
        GameObject ring = new GameObject("ShockRing");
        ring.transform.SetParent(fxRoot.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        {
            // Düz disk: Sphere yatık scale ile halka hissi verir
            GameObject ringMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ringMesh.transform.SetParent(ring.transform, false);
            Destroy(ringMesh.GetComponent<Collider>());
            Material m = new Material(litShader);
            Color rc = new Color(0.2f, 0.85f, 1f, 0.7f); // cyan halka
            m.SetColor("_BaseColor", rc);
            m.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 1f) * 4f);
            m.EnableKeyword("_EMISSION");
            SetTransparent(m);
            ringMesh.GetComponent<MeshRenderer>().material = m;
        }
        KinematicSparkMover ringMover = ring.AddComponent<KinematicSparkMover>();
        ringMover.velocity = Vector3.zero;
        ringMover.lifetime = 0.4f;
        ringMover.expandScale = 14f;   // XZ'de genişler
        ringMover.expandY = false;
        ringMover.fadeOut = true;
        ringMover.scaleToZero = false;

        // ── 3. RADYAL ENERJİ ÇİZGİLERİ (LineRenderer tabanlı ışınlar) ───
        int lineCount = 12;
        for (int i = 0; i < lineCount; i++)
        {
            float angle = (360f / lineCount) * i;
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

            GameObject lineObj = new GameObject($"EnergyLine_{i}");
            lineObj.transform.SetParent(fxRoot.transform);
            lineObj.transform.position = position;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, position);
            lr.SetPosition(1, position + dir * 0.05f); // başlangıçta kısa
            lr.startWidth = Random.Range(0.04f, 0.09f);
            lr.endWidth   = 0f;
            lr.useWorldSpace = true;

            Material lm = new Material(Shader.Find("Sprites/Default") ?? litShader);
            bool isCyan = (i % 2 == 0);
            Color lc = isCyan ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.7f, 0.1f);
            lm.SetColor("_Color", lc);
            lm.SetColor("_EmissionColor", lc * 4f);
            lr.material = lm;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Çizgi animatörü ekle
            EnergyLineAnimator ela = lineObj.AddComponent<EnergyLineAnimator>();
            ela.origin    = position;
            ela.direction = dir;
            ela.maxLength = Random.Range(0.6f, 1.4f);
            ela.lifetime  = Random.Range(0.22f, 0.40f);
        }

        // ── 4. SAÇILAN KIVIRCIMLAR (eski tip süsleyici) ──────────────────
        int sparkCount = 6;
        for (int i = 0; i < sparkCount; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.transform.SetParent(fxRoot.transform);
            spark.transform.localPosition = Vector3.zero;
            spark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.10f);
            Destroy(spark.GetComponent<Collider>());

            Material m = new Material(litShader);
            Color sc = (i % 2 == 0) ? new Color(0.3f, 0.95f, 1f) : new Color(1f, 0.9f, 0.2f);
            m.SetColor("_BaseColor", sc);
            m.SetColor("_EmissionColor", sc * 5f);
            m.EnableKeyword("_EMISSION");
            SetTransparent(m);
            spark.GetComponent<MeshRenderer>().material = m;

            float a2 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            KinematicSparkMover sm = spark.AddComponent<KinematicSparkMover>();
            sm.velocity = new Vector3(Mathf.Cos(a2), Random.Range(0.2f, 0.5f), Mathf.Sin(a2))
                          * Random.Range(1.8f, 3.5f);
            sm.lifetime    = Random.Range(0.25f, 0.5f);
            sm.scaleToZero = true;
            sm.fadeOut     = true;
        }

        Object.Destroy(fxRoot, 1.5f);
    }

    private static void SetTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
    }

    /// <summary>
    /// URP uyumlu malzeme oluştur
    /// </summary>
    private static Material CreateURPMaterial(Color color, bool transparent = false)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
            urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null)
            urpShader = Shader.Find("Standard");

        Material mat = new Material(urpShader);
        mat.SetColor("_BaseColor", color);

        if (transparent)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            // Matte taş ayarları (transparent versiyonu)
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Smoothness", 0.1f);
        }
        else
        {
            // TAŞ = Matte, kaük, pürüzlü! (cam/ayna değil)
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Smoothness", 0.15f);
        }

        return mat;
    }

    /// <summary>
    /// Mıknatıs oluşturucu - Fabrika metodu (URP uyumlu)
    /// </summary>
    public static MagnetPiece CreateMagnet(Vector3 position, int playerNumber, bool preview = false)
    {
        // Taş görselini Quad olarak oluştur (tepeden bakış)
        GameObject magnetObj = new GameObject(preview ? "MagnetPreview" : "MagnetPiece");
        magnetObj.transform.position = position;

        MagnetPiece magnet = magnetObj.AddComponent<MagnetPiece>();
        magnet.ownerPlayer = playerNumber;
        magnet.isPreview = preview;

        // Sphere collider ekle (fizik için)
        SphereCollider sCol = magnetObj.AddComponent<SphereCollider>();
        sCol.radius = 0.26f;
        if (preview) sCol.enabled = false;

        Color playerColor = GameManager.Instance.GetPlayerColor(playerNumber);

        // == TAŞ GÖRSELLİK: Üç Katmanlı 3D Derinlik ==
        // Katman 1: Ana taş gövdesi (koyu hematit)
        GameObject pebbleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pebbleObj.name = "StonePebble";
        pebbleObj.transform.SetParent(magnetObj.transform, false);
        pebbleObj.transform.localPosition = Vector3.zero;
        float sx = Random.Range(0.48f, 0.58f);
        float sy = Random.Range(0.20f, 0.28f);
        float sz = Random.Range(0.42f, 0.52f);
        pebbleObj.transform.localScale = new Vector3(sx, sy, sz);
        pebbleObj.transform.localRotation = Quaternion.Euler(
            Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        Destroy(pebbleObj.GetComponent<Collider>());
        magnet.meshRenderer = pebbleObj.GetComponent<MeshRenderer>();

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mainMat = new Material(litShader);
        mainMat.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.12f)); // çok koyu antrasit
        mainMat.SetFloat("_Metallic", 0.15f);
        mainMat.SetFloat("_Smoothness", 0.82f);

        // Katman 2: Specular highlight halkası (orta hafif aydınlıkça gri)
        GameObject highlightObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlightObj.name = "StoneHighlight";
        highlightObj.transform.SetParent(magnetObj.transform, false);
        highlightObj.transform.localPosition = new Vector3(-0.05f, 0.06f, -0.05f);
        highlightObj.transform.localScale = new Vector3(sx * 0.55f, sy * 0.55f, sz * 0.55f);
        Destroy(highlightObj.GetComponent<Collider>());
        Material hlMat = new Material(litShader);
        hlMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.0f));
        hlMat.SetFloat("_Metallic", 0f);
        hlMat.SetFloat("_Smoothness", 1.0f);
        hlMat.SetColor("_EmissionColor", new Color(0.7f, 0.75f, 0.85f) * 0.25f);
        hlMat.EnableKeyword("_EMISSION");
        hlMat.SetFloat("_Surface", 1);
        hlMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        hlMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        hlMat.SetInt("_ZWrite", 0);
        hlMat.renderQueue = 3000;
        hlMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        hlMat.SetOverrideTag("RenderType", "Transparent");
        highlightObj.GetComponent<MeshRenderer>().material = hlMat;

        // Katman 3: Alt taraf glow halkacığı
        GameObject glowRingObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowRingObj.name = "StoneGlow";
        glowRingObj.transform.SetParent(magnetObj.transform, false);
        glowRingObj.transform.localPosition = new Vector3(0, -0.04f, 0);
        glowRingObj.transform.localScale = new Vector3(sx * 1.15f, sy * 0.3f, sz * 1.15f);
        Destroy(glowRingObj.GetComponent<Collider>());

        Material glowMat = new Material(litShader);
        Color gc = playerColor * 1.8f;
        gc.a = 0.35f;
        glowMat.SetColor("_BaseColor", gc);
        glowMat.SetColor("_EmissionColor", gc * 2f);
        glowMat.EnableKeyword("_EMISSION");
        glowMat.SetFloat("_Surface", 1);
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glowMat.SetInt("_ZWrite", 0);
        glowMat.renderQueue = 3000;
        glowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glowMat.SetOverrideTag("RenderType", "Transparent");
        glowRingObj.GetComponent<MeshRenderer>().material = glowMat;

        if (preview)
        {
            mainMat.SetFloat("_Surface", 1);
            mainMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mainMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mainMat.SetInt("_ZWrite", 0);
            mainMat.renderQueue = 3000;
            mainMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mainMat.SetOverrideTag("RenderType", "Transparent");
            Color previewColor = new Color(0.12f, 0.12f, 0.14f, 0.55f);
            mainMat.SetColor("_BaseColor", previewColor);
            mainMat.SetColor("_EmissionColor", playerColor * 0.8f);
            mainMat.EnableKeyword("_EMISSION");
        }

        magnet.meshRenderer.material = mainMat;

        // Fizik ayarları
        if (!preview)
        {
            Rigidbody rigidBody = magnetObj.AddComponent<Rigidbody>();
            rigidBody.mass = 1.0f;
            rigidBody.useGravity = false;
            rigidBody.isKinematic = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        }

        return magnet;
    }

    public void SetHighlight(bool highlighted)
    {
        if (meshRenderer == null) return;

        Color baseColor = GameManager.Instance.GetPlayerColor(ownerPlayer);
        if (highlighted)
        {
            meshRenderer.material.SetColor("_EmissionColor", baseColor * 1.2f);
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f); 
        }
        else
        {
            meshRenderer.material.DisableKeyword("_EMISSION");
            meshRenderer.transform.localScale = new Vector3(0.53f, 0.24f, 0.47f); 
        }
    }
}
