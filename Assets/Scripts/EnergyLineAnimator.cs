using UnityEngine;

/// <summary>
/// Çarpışmadan yayılan radyal enerji çizgilerini animasyonla büyütür ve söndürür
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class EnergyLineAnimator : MonoBehaviour
{
    [HideInInspector] public Vector3 origin;
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public float maxLength = 1.0f;
    [HideInInspector] public float lifetime  = 0.3f;

    private LineRenderer lr;
    private float elapsed = 0f;

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        if (t >= 1f) { Destroy(gameObject); return; }

        // Faz 1 (t < 0.5): hızla uzar
        // Faz 2 (t > 0.5): kuyruktan kaybolur (fade-out + shorten)
        float growT  = Mathf.Clamp01(t / 0.4f);           // 0→0.4 içinde tam uzunluk
        float shrinkT = Mathf.Clamp01((t - 0.4f) / 0.6f); // 0.4→1.0 içinde küçül

        float tipDist  = Mathf.Lerp(0f, maxLength, growT);
        float tailDist = Mathf.Lerp(0f, maxLength, shrinkT);

        Vector3 tip  = origin + direction * tipDist;
        Vector3 tail = origin + direction * tailDist;

        lr.SetPosition(0, tail);
        lr.SetPosition(1, tip);

        // Kalınlık: genişleyip söner
        float fadeAlpha = 1f - shrinkT;
        lr.startWidth = Mathf.Lerp(0.09f, 0f, t) * fadeAlpha;
        lr.endWidth   = 0f;

        // Emission renk sönümü
        if (lr.material != null)
        {
            Color c = lr.material.GetColor("_Color");
            c.a = fadeAlpha;
            lr.material.SetColor("_Color", c);
        }
    }
}
