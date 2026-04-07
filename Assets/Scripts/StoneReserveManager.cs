using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Yan panel taş yığınlarını yönetir.
/// Taşlar gerçekçi görsel (PNG texture) ile Quad üzerinde gösterilir.
/// Oyuncu 1 (Mavi) = Sol, Oyuncu 2 (Turuncu) = Sağ
/// </summary>
public class StoneReserveManager : MonoBehaviour
{
    public static StoneReserveManager Instance { get; private set; }

    private GameObject[] p1Stones; // YENİ: Yuva sistemli sabit dizi
    private GameObject[] p2Stones;

    // Yan panel pozisyonları (Dikey telefona sığması için merkeze yaklaştırıldı)
    private const float P1_Z = -6.8f; 
    private const float P2_Z =  6.8f; 
    private const float STONE_Y = 0.15f;

    // Izgara ayarları - DAHA CANLI VE ARALIKLI
    private const int COLS = 6;
    private const float COL_SPACING = 0.88f; 
    private const float ROW_SPACING = 0.95f; 

    private Texture2D stoneTexture;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(int stonesPerPlayer)
    {
        p1Stones = new GameObject[stonesPerPlayer];
        p2Stones = new GameObject[stonesPerPlayer];

        // Yan panelleri daha makul boyutlara çekiyoruz (Çakışma engellendi)
        CreateSideBackground(1, new Vector3(0f, -0.05f, P1_Z), new Vector3(7.0f, 0.05f, 3.4f), new Color(0.1f, 0.85f, 1f));
        CreateSideBackground(2, new Vector3(0f, -0.05f, P2_Z), new Vector3(7.0f, 0.05f, 3.4f), new Color(1f, 0.1f, 0.1f));

        CreateReserveStones(1, stonesPerPlayer, P1_Z, p1Stones);
        CreateReserveStones(2, stonesPerPlayer, P2_Z, p2Stones);
    }

    // Texture loading removed: Using procedural 3D materials instead of 2D image

    private void CreateSideBackground(int player, Vector3 pos, Vector3 scale, Color outlineColor)
    {
        // İç dolgu (koyu zemin)
        GameObject bgLayer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bgLayer.name = $"SidePanelBG_P{player}";
        bgLayer.transform.position = pos;
        bgLayer.transform.localScale = scale;
        Destroy(bgLayer.GetComponent<Collider>());

        MeshRenderer r = bgLayer.GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(s);
        // Çok koyu, hafif renkli zemin
        m.SetColor("_BaseColor", outlineColor * 0.15f);
        m.SetFloat("_Smoothness", 0.1f);
        r.material = m;

        // Parlak çerçeve
        GameObject glowLayer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glowLayer.name = $"SidePanelGlow_P{player}";
        glowLayer.transform.position = pos - new Vector3(0, 0.01f, 0); // Altında daha büyük
        glowLayer.transform.localScale = scale + new Vector3(0.4f, 0f, 0.4f);
        Destroy(glowLayer.GetComponent<Collider>());
        
        Material glowMat = new Material(s);
        glowMat.SetColor("_BaseColor", outlineColor);
        glowMat.SetColor("_EmissionColor", outlineColor * 2.5f); // 1.5'den 2.5'e (Daha canlı ve parlak)
        glowMat.EnableKeyword("_EMISSION");
        glowLayer.GetComponent<MeshRenderer>().material = glowMat;
    }

    private void CreateReserveStones(int player, int count, float baseZ, GameObject[] stoneArray)
    {
        int totalRows = Mathf.CeilToInt(count / (float)COLS);
        float totalWidth = (COLS - 1) * COL_SPACING;
        float totalHeight = (totalRows - 1) * ROW_SPACING;

        // Daha canlı renk paleti
        Color tintColor = player == 1 ? new Color(0.1f, 0.85f, 1f) : new Color(1f, 0.1f, 0.1f);

        for (int i = 0; i < count; i++)
        {
            Vector3 slotPos = GetSlotPosition(i, count, baseZ);
            
            // Organik rastgele offset (Çok değil, düzen bozulmasın)
            slotPos.x += Random.Range(-0.05f, 0.05f);
            slotPos.z += Random.Range(-0.05f, 0.05f);

            GameObject stone = Create3DPebble(slotPos, tintColor);
            stone.name = $"ReserveStone_P{player}_S{i}";
            stoneArray[i] = stone;
        }
    }

    private Vector3 GetSlotPosition(int slotIdx, int totalSlots, float baseZ)
    {
        int totalRows = Mathf.CeilToInt(totalSlots / (float)COLS);
        float totalWidth = (COLS - 1) * COL_SPACING;
        float totalHeight = (totalRows - 1) * ROW_SPACING;

        int row = slotIdx / COLS;
        int col = slotIdx % COLS;

        float offsetX = (row % 2 == 0) ? 0f : (COL_SPACING * 0.45f);
        float x = -totalWidth / 2f + col * COL_SPACING + offsetX;
        float z = baseZ + totalHeight / 2f - row * ROW_SPACING;

        return new Vector3(x, STONE_Y, z);
    }

    /// <summary>
    /// Gerçekçi 3D Hematit taşı (cilalı, parlak koyu renk)
    /// </summary>
    private GameObject Create3DPebble(Vector3 pos, Color glowColor)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = pos;
        // Yassı, asimetrik taş boyutu (Orta kıvam)
        float sx = Random.Range(0.52f, 0.62f);
        float sy = Random.Range(0.22f, 0.30f);
        float sz = Random.Range(0.48f, 0.56f);
        sphere.transform.localScale = new Vector3(sx, sy, sz);

        // Rastgele dönüş (organik görünüm)
        sphere.transform.rotation = Quaternion.Euler(
            Random.Range(-10f, 10f),
            Random.Range(0f, 360f),
            Random.Range(-10f, 10f)
        );

        // Tıklamayla seçilebilmesi için Trigger SphereCollider ekle
        SphereCollider sc = sphere.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.85f; // Görsel boyutuyla uyumlu tıklama alanı

        MeshRenderer r = sphere.GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(s);
        
        // Hematit - Koyu gri/siyah parlak yüzey
        mat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Smoothness", 0.85f); // Çok parlak
        
        r.material = mat;

        return sphere;
    }

    // ── Taş Sayısı ──────────────────────────────────────────────

    /// <summary>
    /// Belirli bir taş nesnesini rezervden sil ve yok et. Başarılıysa true döner.
    /// </summary>
    public bool TryConsumeSpecificStone(int player, GameObject stoneObj)
    {
        var array = player == 1 ? p1Stones : p2Stones;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == stoneObj)
            {
                array[i] = null; // Yuva boşaldı
                if (stoneObj != null) Destroy(stoneObj);
                return true;
            }
        }
        return false;
    }

    public bool TryConsumeStone(int player)
    {
        var array = player == 1 ? p1Stones : p2Stones;
        for (int i = array.Length - 1; i >= 0; i--)
        {
            if (array[i] != null)
            {
                GameObject top = array[i];
                array[i] = null;
                Destroy(top);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Eski uyumluluk: bırakma anında çağrılan versiyon (şimdi no-op, zaten sürükleme başında silindi)
    /// </summary>
    public void RemoveTopStone(int player)
    {
        // Artık sürükleme başında TryConsumeStone çağrılıyor, bu metot boş kalabilir
        // Ama yine de güvenli olsun diye list kontrolü yapıyoruz
        // (GameManager'dan gelen eski çağrılar için)
    }

    /// <summary>
    /// Sürükleme iptal edilince taşı geri ekle
    /// </summary>
    public void ReturnStone(int player)
    {
        var array = player == 1 ? p1Stones : p2Stones;
        float baseZ = player == 1 ? P1_Z : P2_Z;
        Color tint = player == 1 ? new Color(0.1f, 0.85f, 1f) : new Color(1f, 0.1f, 0.1f);

        // İlk boş yuvayı (null) bul
        int emptySlot = -1;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1) return; // Tüm yuvalar dolu!

        Vector3 slotPos = GetSlotPosition(emptySlot, array.Length, baseZ);
        // İade edilen taşı yuvaya tam olarak oturtalım (Overlap engellemek için offset azaldı)
        slotPos.x += Random.Range(-0.02f, 0.02f);
        slotPos.z += Random.Range(-0.02f, 0.02f);

        GameObject stone = Create3DPebble(slotPos, tint);
        stone.name = $"ReserveStone_P{player}_ReturnedS{emptySlot}";
        array[emptySlot] = stone;
    }

    /// <summary>
    /// Belirtilen oyuncu için rezervde taş var mı?
    /// </summary>
    public bool HasStones(int player)
    {
        var array = player == 1 ? p1Stones : p2Stones;
        foreach (var s in array) if (s != null) return true;
        return false;
    }

    public int GetStoneCount(int player)
    {
        var array = player == 1 ? p1Stones : p2Stones;
        int count = 0;
        foreach (var s in array) if (s != null) count++;
        return count;
    }

    public void ClearAll()
    {
        if (p1Stones != null) foreach (var s in p1Stones) if (s) Destroy(s);
        if (p2Stones != null) foreach (var s in p2Stones) if (s) Destroy(s);
        p1Stones = null;
        p2Stones = null;
    }
}
