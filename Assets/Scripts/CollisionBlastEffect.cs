using UnityEngine;

/// <summary>
/// Referans görseldeki manyetik çarpışma efektini üretir:
/// Taşlar temas ettiğinde parlak cyan çizgili yıldız patlaması.
/// Kamera tam tepeden (Y=15) baktığı için tüm efektler XZ düzleminde çizilir.
/// </summary>
public class CollisionBlastEffect : MonoBehaviour
{
    // Dışarıdan çağrılacak tek metot
    public static void Spawn(Vector3 worldPos)
    {
        GameObject root = new GameObject("BlastFX");
        root.transform.position = worldPos;
        var fx = root.AddComponent<CollisionBlastEffect>();
        fx.StartEffect(worldPos);
    }

    // ──────────────────────────────────────────────
    private float elapsed = 0f;
    private float lifetime = 0.55f;

    // Işın çizgilerini tutacağız
    private struct Ray
    {
        public LineRenderer lr;
        public Vector3      dir;    // XZ yön (normalize)
        public float        maxLen;
        public float        width;
    }
    private Ray[]      rays;
    private GameObject flashSphere;
    private Material   flashMat;
    private Transform  rootTr;

    private void StartEffect(Vector3 pos)
    {
        rootTr = transform;
        Shader spritesShader = Shader.Find("Sprites/Default");
        Shader litShader     = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

        // ─── 1. MERKEZ PARLAYAN KÜR ───────────────────────────────────
        flashSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashSphere.transform.SetParent(rootTr);
        flashSphere.transform.localPosition = Vector3.zero;
        flashSphere.transform.localScale = Vector3.one * 0.5f;
        Destroy(flashSphere.GetComponent<Collider>());

        flashMat = new Material(litShader);
        Color fc = new Color(0.3f, 0.95f, 1f);
        flashMat.SetColor("_BaseColor", fc);
        flashMat.SetColor("_EmissionColor", fc * 12f);
        flashMat.EnableKeyword("_EMISSION");
        MakeTransparent(flashMat);
        flashSphere.GetComponent<MeshRenderer>().material = flashMat;

        // ─── 2. RADYAL IŞIN ÇİZGİLERİ (XZ düzleminde) ───────────────
        // 8 ana ışın + aralarında 8 ince ışın = 16 çizgi
        int rayCount = 16;
        rays = new Ray[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angleDeg = (360f / rayCount) * i;
            float rad = angleDeg * Mathf.Deg2Rad;
            // Kamera Y'den baktığı için XZ düzleminde çiziyoruz
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

            bool isMajor = (i % 2 == 0); // Kalın ana ışınlar

            GameObject lineObj = new GameObject($"Ray_{i}");
            lineObj.transform.SetParent(rootTr);
            lineObj.transform.position = pos;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            // Çok noktalı (eğri-zigzag çizgi için) — 3 nokta: başlangıç + orta kırık + uç
            lr.positionCount = 3;
            lr.useWorldSpace = true;

            // Kırıklı rota: ortada hafif sapma ile lightning hissi
            float zigOffset = isMajor ? Random.Range(-0.08f, 0.08f) : Random.Range(-0.05f, 0.05f);
            Vector3 perpDir = new Vector3(-dir.z, 0f, dir.x); // dik yön XZ'de

            float maxL = isMajor ? Random.Range(0.9f, 1.4f) : Random.Range(0.4f, 0.8f);

            lr.SetPosition(0, pos);                                            // başlangıç
            lr.SetPosition(1, pos + dir * (maxL * 0.5f) + perpDir * zigOffset); // orta kırık
            lr.SetPosition(2, pos + dir * maxL);                               // uç

            // Kalınlık: baştan uca incelir
            float w = isMajor ? Random.Range(0.12f, 0.20f) : Random.Range(0.05f, 0.10f);
            lr.startWidth = w;
            lr.endWidth   = 0f;
            lr.widthMultiplier = 1f;

            // Renk: ana = cyan, ince = beyaz/cyan arası
            Color lc = isMajor
                ? new Color(0.15f, 0.85f, 1f)
                : new Color(0.7f,  0.95f, 1f);

            // Sprites/Default ile çizgi ışıl ışıl parlar (kamera önünde)
            Material lm = new Material(spritesShader ?? litShader);
            lm.SetColor("_Color", lc);
            lm.EnableKeyword("_EMISSION");
            lr.material = lm;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 10; // Her şeyin üstünde

            rays[i] = new Ray { lr = lr, dir = dir, maxLen = maxL, width = w };
        }

        // 2 sn sonra sil
        Destroy(gameObject, lifetime + 0.1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        if (t >= 1f) { Destroy(gameObject); return; }

        // ─── Merkez küresi ─────────────────────────────────────
        if (flashSphere != null)
        {
            // Anında büyüyüp söner
            float flashT = Mathf.Clamp01(elapsed / 0.15f);
            float flashScale = Mathf.Lerp(0.5f, 2.2f, flashT) * (1f - flashT * 0.6f);
            flashSphere.transform.localScale = Vector3.one * Mathf.Max(0f, flashScale);
            if (flashMat != null)
            {
                float em = Mathf.Lerp(12f, 0f, flashT);
                Color fc = new Color(0.3f, 0.95f, 1f);
                flashMat.SetColor("_EmissionColor", fc * em);
            }
        }

        // ─── Işın çizgileri ────────────────────────────────────
        // Faz: 0→0.4 uzar, 0.4→1 kuyruğu çekilir
        float growT  = Mathf.Clamp01(t / 0.4f);
        float shrinkT = Mathf.Clamp01((t - 0.35f) / 0.65f);

        Vector3 origin = rootTr.position;

        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i].lr == null) continue;

            float tipDist  = rays[i].maxLen * Mathf.SmoothStep(0f, 1f, growT);
            float tailDist = rays[i].maxLen * Mathf.SmoothStep(0f, 1f, shrinkT);

            Vector3 dir = rays[i].dir;
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
            float zigOffset = (i % 2 == 0) ? 0.08f : -0.06f;

            Vector3 tip  = origin + dir * tipDist;
            Vector3 mid  = origin + dir * ((tipDist + tailDist) * 0.5f) + perp * zigOffset * (1f - t);
            Vector3 tail = origin + dir * tailDist;

            rays[i].lr.SetPosition(0, tail);
            rays[i].lr.SetPosition(1, mid);
            rays[i].lr.SetPosition(2, tip);

            // Fade
            float alpha = 1f - (shrinkT * shrinkT);
            float w = rays[i].width * alpha;
            rays[i].lr.startWidth = w;
            rays[i].lr.endWidth   = 0f;

            if (rays[i].lr.material != null)
            {
                Color c = rays[i].lr.material.GetColor("_Color");
                rays[i].lr.material.SetColor("_Color",
                    new Color(c.r, c.g, c.b, Mathf.Max(0f, alpha)));
            }
        }
    }

    private static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
    }
}
