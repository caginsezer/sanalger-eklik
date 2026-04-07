using UnityEngine;

/// <summary>
/// Oyun sahasını oluşturur - Dairesel alan + ip sınırı (URP uyumlu)
/// Portrait mobil ekrana uygun boyut
/// </summary>
public class BoardSetup : MonoBehaviour
{
    [Header("Saha Ayarları")]
    public float boardRadiusX = 3.9f; // Kenarlarda azıcık boşluk kalması için ufaltıldı
    public float boardRadiusZ = 4.9f; // Taş panelleriyle çakışmayı engellemek için küçültüldü
    public int ropeSegments = 64;
    public float ropeHeight = 0.05f;

    private GameObject boardObject;
    private LineRenderer ropeRenderer;

    private void Awake()
    {
        // Inspector'dan eski büyük değerlerin okunmasını önlemek için zorla eziyoruz:
        boardRadiusX = 3.9f;
        boardRadiusZ = 4.9f;
    }

    private Material CreateURPMaterial(Color color, float smoothness = 0.5f, float metallic = 0f)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (urpShader == null) urpShader = Shader.Find("Sprites/Default");
        
        Material mat = (urpShader != null) ? new Material(urpShader) : null;
        if (mat != null)
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
        }
        return mat;
    }

    private Material CreateUnlitMaterial()
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
        return (unlitShader != null) ? new Material(unlitShader) : null;
    }

    public void CreateBoard()
    {
        CreateTrackRing();    // Önce koyu çevre halkası
        CreatePlayingField(); // Üzeri sarı saha
        CreateRopeBoundary();
        CreateFloor();
        CreatePhysicalBoundary();
    }

    // Referanstaki koyu iç çevre halkası (sarı alan ile sınır arası)
    // Koyu iç çevre halkası
    private void CreateTrackRing()
    {
        GameObject trackObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trackObj.name = "TrackRing";
        trackObj.transform.position = new Vector3(0, -0.01f, 0); 
        trackObj.transform.localScale = new Vector3((boardRadiusX + 0.3f) * 2, 0.04f, (boardRadiusZ + 0.3f) * 2);

        MeshRenderer renderer = trackObj.GetComponent<MeshRenderer>();
        renderer.material = CreateURPMaterial(
            new Color(0.1f, 0.1f, 0.12f),  // Koyu gri siyahımsı çerçeve (referanstaki gibi)
            smoothness: 0.2f,
            metallic: 0f
        );

        Destroy(trackObj.GetComponent<Collider>());
    }

    private void CreatePlayingField()
    {
        boardObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        boardObject.name = "GameBoard";
        boardObject.transform.position = Vector3.zero;
        boardObject.transform.localScale = new Vector3(boardRadiusX * 2, 0.05f, boardRadiusZ * 2);

        MeshRenderer renderer = boardObject.GetComponent<MeshRenderer>();
        // SARI/ALTIN zemin - Hocanın referansına göre
        renderer.material = CreateURPMaterial(
            new Color(0.95f, 0.78f, 0.05f),
            smoothness: 0.4f,
            metallic: 0.1f
        );

        boardObject.GetComponent<Collider>().isTrigger = false;
        Rigidbody boardRb = boardObject.AddComponent<Rigidbody>();
        boardRb.isKinematic = true;
    }

    private void CreateRopeBoundary()
    {
        // Alt Yarı (Neon Vivid Cyan - Oyuncu 1)
        CreateNeonArc("NeonGlow_P1_Bottom", new Color(0.1f, 0.85f, 1f), Mathf.PI, 2f * Mathf.PI);
        
        // Üst Yarı (Neon Bright Red - Oyuncu 2)
        CreateNeonArc("NeonGlow_P2_Top", new Color(1f, 0.1f, 0.1f), 0f, Mathf.PI);

        // Ortadan geçen yatay şerit
        CreateCenterDivideLine();
    }

    private void CreateNeonArc(string name, Color emitColor, float startAngle, float endAngle)
    {
        GameObject arcObj = new GameObject(name);
        arcObj.transform.position = new Vector3(0, ropeHeight + 0.05f, 0);

        LineRenderer lr = arcObj.AddComponent<LineRenderer>();
        int segments = ropeSegments / 2;
        lr.positionCount = segments + 1;
        lr.startWidth = 0.20f; // 0.15'den 0.20'ye (Daha belirgin)
        lr.endWidth = 0.20f;
        lr.loop = false;
        lr.useWorldSpace = true;

        Material mat = CreateUnlitMaterial();
        mat.SetColor("_BaseColor", emitColor);
        mat.SetColor("_EmissionColor", emitColor * 3.0f); // 1.5'den 3.0'a (Ciddi bir neon etkisi)
        mat.EnableKeyword("_EMISSION");
        lr.material = mat;

        float adjX = boardRadiusX + 0.15f; // Siyah dış çerçevenin hemen üstüne/dışına doğru
        float adjZ = boardRadiusZ + 0.15f;
        float angleStep = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (i * angleStep);
            float x = Mathf.Cos(angle) * adjX;
            float z = Mathf.Sin(angle) * adjZ;
            lr.SetPosition(i, new Vector3(x, ropeHeight + 0.06f, z));
        }
    }

    private void CreateCenterDivideLine()
    {
        GameObject lineObj = new GameObject("CenterDivideLine");
        lineObj.transform.position = new Vector3(0, ropeHeight + 0.05f, 0);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.useWorldSpace = true;

        Material mat = CreateUnlitMaterial();
        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
        
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        lr.material = mat;

        // Dikey modda ortadan geçen çizgi yatay (X ekseninde eksi'den artı'ya) olur
        lr.SetPosition(0, new Vector3(-boardRadiusX, ropeHeight + 0.06f, 0));
        lr.SetPosition(1, new Vector3(boardRadiusX, ropeHeight + 0.06f, 0));
    }

    private void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TableFloor";
        floor.transform.position = new Vector3(0, -0.4f, 0); // Masayı biraz daha aşağı alalım
        floor.transform.localScale = new Vector3(4, 1, 4); // Daha da geniş masa (16:9 için)

        MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
        
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = (s != null) ? new Material(s) : null;
        
        if (mat != null)
        {
            // Tahta dokusunu yükle
            try {
                string texPath = System.IO.Path.Combine(Application.dataPath, "Textures", "wood_bg.png");
                if (System.IO.File.Exists(texPath))
                {
                    byte[] fd = System.IO.File.ReadAllBytes(texPath);
                    Texture2D woodTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    woodTex.LoadImage(fd);
                    mat.mainTexture = woodTex;
                    mat.mainTextureScale = new Vector2(4f, 4f); // Ahşap parkeler tekrar etsin
                    mat.SetFloat("_Smoothness", 0.4f); // Hafif cilalı masa
                }
                else
                {
                    // Doku yoksa fallback düz renk
                    mat.SetColor("_BaseColor", new Color(0.28f, 0.16f, 0.07f));
                    mat.SetFloat("_Smoothness", 0.6f);
                }
            } catch {
                mat.SetColor("_BaseColor", new Color(0.28f, 0.16f, 0.07f));
            }
        }
        
        renderer.material = mat;
    }

    private void CreatePhysicalBoundary()
    {
        GameObject boundaryParent = new GameObject("PhysicalBoundary");
        boundaryParent.transform.position = Vector3.zero;

        int colliderCount = 32; // Daha fazla parça ile pürüzsüz elips
        float adjX = boardRadiusX - 0.05f;
        float adjZ = boardRadiusZ - 0.05f;
        float angleStep = 360f / colliderCount;

        for (int i = 0; i < colliderCount; i++)
        {
            float angle = i * angleStep;
            float angleRad = angle * Mathf.Deg2Rad;
            
            // Elips üzerindeki nokta
            Vector3 pos = new Vector3(
                Mathf.Cos(angleRad) * adjX,
                0.2f,
                Mathf.Sin(angleRad) * adjZ
            );

            // Teğet açısı (elips için türevden)
            float nextAngleRad = (angle + 1f) * Mathf.Deg2Rad;
            Vector3 nextPos = new Vector3(Mathf.Cos(nextAngleRad) * adjX, 0.2f, Mathf.Sin(nextAngleRad) * adjZ);
            Vector3 tangent = (nextPos - pos).normalized;
            float lookAngle = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;

            GameObject wall = new GameObject($"Wall_{i}");
            wall.transform.position = pos;
            wall.transform.rotation = Quaternion.Euler(0, lookAngle, 0);
            wall.transform.parent = boundaryParent.transform;

            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3(0.1f, 0.5f, 0.8f);
        }
    }

    public bool IsWithinBounds(Vector3 position)
    {
        // Elips içi kontrol: (x^2 / a^2) + (z^2 / b^2) < 1
        float localX = position.x;
        float localZ = position.z;
        float a = boardRadiusX - 0.2f;
        float b = boardRadiusZ - 0.2f;
        return (localX * localX) / (a * a) + (localZ * localZ) / (b * b) < 1.0f;
    }
}
