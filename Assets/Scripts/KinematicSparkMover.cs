using UnityEngine;

/// <summary>
/// Çarpışma kıvılcım + şok halkası animatörü
/// </summary>
public class KinematicSparkMover : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public float lifetime = 0.4f;

    // Genişleme efekti (shockwave ring için)
    [HideInInspector] public float expandScale = 0f;   // unit/s genişleme hızı
    [HideInInspector] public bool  expandY     = true; // false ise sadece XZ genişler
    [HideInInspector] public bool  scaleToZero = false;
    [HideInInspector] public bool  fadeOut     = false;

    private float elapsed = 0f;
    private MeshRenderer mr;
    private Vector3 startScale;

    private void Start()
    {
        mr = GetComponentInChildren<MeshRenderer>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        if (t >= 1f) { Destroy(gameObject); return; }

        // Hareket (kıvılcımlar için)
        if (velocity != Vector3.zero)
            transform.position += velocity * Time.deltaTime * (1f - t * 0.6f);

        // Genişleyen halka
        if (expandScale > 0f)
        {
            float s = expandScale * t;
            if (expandY)
                transform.localScale = startScale + Vector3.one * s;
            else
                transform.localScale = startScale + new Vector3(s, 0f, s);
        }

        // Boyut sıfırlanma
        if (scaleToZero)
        {
            float sc = Mathf.Lerp(1f, 0f, t * t);
            transform.localScale = startScale * sc;
        }

        // Emission sönme
        if (fadeOut && mr != null && mr.material != null)
        {
            Color em = mr.material.GetColor("_EmissionColor");
            mr.material.SetColor("_EmissionColor", em * (1f - t));
        }
    }
}
