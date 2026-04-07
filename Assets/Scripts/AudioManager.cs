using UnityEngine;
using System.Collections;

/// <summary>
/// Tüm oyun seslerini prosedürel sentez ile oluşturan merkezi ses yöneticisi.
/// Hiçbir harici .wav/.mp3 dosyasına ihtiyaç duymaz – sesler tamamen kodla üretilir.
/// Kullanım: AudioManager.Instance.Play(AudioManager.SoundType.StonePlaced);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum SoundType
    {
        MenuAmbiance,
        GameAmbiance, // Yeni: Oyun içi müzik
        ButtonClick,
        GameStart,
        StonePlaced,
        StonePick,
        StoneDrag,
        Collision,
        TurnChange,
        Penalty,
        Win
    }

    private const int SAMPLE_RATE = 44100;

    // Birden fazla ses aynı anda çalabilsin diye havuz
    private AudioSource[] sourcePools;
    private int poolIdx = 0;
    private const int POOL_SIZE = 8;

    // Menü ambient için ayrı looping kaynak
    private AudioSource ambianceSource;
    private bool ambiancePlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitPool()
    {
        sourcePools = new AudioSource[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            sourcePools[i] = go.AddComponent<AudioSource>();
            sourcePools[i].playOnAwake = false;
        }

        // Ambient için özel kaynak
        var ambGo = new GameObject("AmbianceSource");
        ambGo.transform.SetParent(transform);
        ambianceSource = ambGo.AddComponent<AudioSource>();
        ambianceSource.playOnAwake = false;
        ambianceSource.loop = true;
        ambianceSource.volume = 0.18f;
    }

    // ──────────────────────────────────────────────
    //  PUBLIC API
    // ──────────────────────────────────────────────

    public void Play(SoundType type)
    {
        switch (type)
        {
            case SoundType.MenuAmbiance:  StartAmbiance(SoundType.MenuAmbiance); return;
            case SoundType.GameAmbiance:  StartAmbiance(SoundType.GameAmbiance); return;
            case SoundType.ButtonClick:   PlayOneShot(MakeButtonClick(),  0.55f); return;
            case SoundType.GameStart:     PlayOneShot(MakeGameStart(),    0.70f); return;
            case SoundType.StonePlaced:   PlayOneShot(MakeStonePlaced(),  0.75f); return;
            case SoundType.StonePick:     PlayOneShot(MakeStonePick(),    0.55f); return;
            case SoundType.StoneDrag:     PlayOneShot(MakeStoneDrag(),    0.40f); return;
            case SoundType.Collision:     PlayOneShot(MakeCollision(),    0.75f); return;
            case SoundType.TurnChange:    PlayOneShot(MakeTurnChange(),   0.45f); return;
            case SoundType.Penalty:       PlayOneShot(MakePenalty(),      0.65f); return;
            case SoundType.Win:           PlayOneShot(MakeWin(),          0.80f); return;
        }
    }

    public void StopAmbiance()
    {
        if (ambiancePlaying)
        {
            ambianceSource.Stop();
            ambiancePlaying = false;
        }
    }

    // ──────────────────────────────────────────────
    //  PLAYBACK HELPERS
    // ──────────────────────────────────────────────

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null) return;
        AudioSource src = GetFreeSource();
        src.clip   = clip;
        src.volume = volume;
        src.pitch  = 1f;
        src.loop   = false;
        src.Play();
    }

    private AudioSource GetFreeSource()
    {
        // Boşta bir kaynak bul; bulunamazsa sıradan bir sonrakini kullan
        for (int i = 0; i < POOL_SIZE; i++)
        {
            int idx = (poolIdx + i) % POOL_SIZE;
            if (!sourcePools[idx].isPlaying)
            {
                poolIdx = idx;
                return sourcePools[idx];
            }
        }
        poolIdx = (poolIdx + 1) % POOL_SIZE;
        return sourcePools[poolIdx];
    }

    private SoundType? currentAmbianceType = null;
    private void StartAmbiance(SoundType type)
    {
        if (currentAmbianceType == type && ambiancePlaying) return;

        StopAmbiance();

        if (type == SoundType.MenuAmbiance)
        {
            ambianceSource.clip = MakeMenuAmbiance();
            ambianceSource.volume = 0.35f;
        }
        else if (type == SoundType.GameAmbiance)
        {
            ambianceSource.clip = MakeGameAmbiance();
            ambianceSource.volume = 0.28f;
        }

        ambianceSource.Play();
        ambiancePlaying = true;
        currentAmbianceType = type;
    }

    // ──────────────────────────────────────────────
    //  SES ÜRETİCİLERİ
    //  Her metot bir AudioClip döner
    // ──────────────────────────────────────────────

    /// <summary>
    /// Menü arkaplan ambiyansı – Elektronik, pulsing synth döngüsü (~4 sn)
    /// Katmanlı: alçak drone + üst neon frekans
    /// </summary>
    private AudioClip MakeMenuAmbiance()
    {
        float dur    = 16.0f; // Tam bir "Song" uzunluğu
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        float bpm = 124f;
        float beatInterval = 60f / bpm;

        // Akorlar (Frekanslar): Am (A3, C4, E4), F (F3, A3, C4), G (G3, B3, D4), Em (E3, G3, B3)
        float[][] chords = {
            new float[] { 220f, 261.63f, 329.63f }, // Am
            new float[] { 174.61f, 220f, 261.63f }, // F
            new float[] { 196f, 246.94f, 293.66f }, // G
            new float[] { 164.81f, 196f, 246.94f }  // Em
        };

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            int chordIdx = Mathf.Min((int)(t / 4.0f), chords.Length - 1); // 4 saniyede bir akor değişir
            float[] currentChord = chords[chordIdx];

            // 1. Kick: Her vuruşta (0.5s)
            float beatT = t % beatInterval;
            float kickEnv = Mathf.Exp(-beatT * 18f);
            float kick = Mathf.Sin(2 * Mathf.PI * 55f * (1f - beatT * 2f)) * kickEnv * 0.45f;

            // 2. Snare: Her ikinci vuruşta
            float snareT = (t + beatInterval) % (beatInterval * 2f);
            float snareEnv = Mathf.Exp(-snareT * 25f);
            float snare = (Random.value * 2f - 1f) * snareEnv * 0.12f;

            // 3. Bas (Daha ritmik, akor temel notası)
            float bassFreq = currentChord[0] * 0.5f; // Temel ton yarım oktav aşağıda
            float bassGate = (t % (beatInterval / 2f) < (beatInterval / 4f)) ? 1f : 0.6f;
            float bass = Mathf.Sin(2 * Mathf.PI * bassFreq * t) * 0.2f * bassGate;

            // 4. Arpej (Üst nota melodisi)
            float arpInterval = beatInterval / 4f;
            int arpIdx = Mathf.FloorToInt((t / arpInterval) % 4); // 0, 1, 2, 3
            float arpFreq = (arpIdx < 3) ? currentChord[arpIdx] : currentChord[1] * 2f; 
            float arpEnv = Mathf.Exp(-(t % arpInterval) * 12f);
            float arp = Mathf.Sin(2 * Mathf.PI * arpFreq * 2f * t) * arpEnv * 0.15f;

            // 5. Atmosferik Pad (Neon hissi)
            float pad = 0f;
            foreach (float freq in currentChord)
            {
                pad += Mathf.Sin(2 * Mathf.PI * freq * t) * 0.05f;
            }

            data[i] = (kick + snare + bass + arp + pad) * 0.9f;
        }

        ApplyFadeInOut(data, SAMPLE_RATE, 0.15f);
        return MakeClip(data, "RichMenuSong_16s");
    }

    /// <summary>
    /// Oyun içi fon müziği - "Super Smooth" Ambient (16 sn döngü, sıfır çınlama)
    /// </summary>
    private AudioClip MakeGameAmbiance()
    {
        float dur    = 16.0f; 
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];
        
        // Akorlar (Frekanslar): Çok derin, sıcak (Dm7, Am7)
        float[][] chords = {
            new float[] { 73.42f, 87.31f, 110.00f, 130.81f }, // Dm7 (Derin)
            new float[] { 55.00f, 65.41f, 82.41f, 98.00f }    // Am7 (Derin)
        };

        float prevSample = 0f;

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            
            // 8 saniyede bir akor değişir, çok yavaş geçiş (Crossfade hissi)
            float chordT = (t / 8.0f);
            int chordIdx = Mathf.Min(Mathf.FloorToInt(chordT), chords.Length - 1);
            float[] currentChord = chords[chordIdx];
            
            // Pad sentezi: Çoklu detuned sine dalgaları (Sıcaklık verir)
            float pad = 0f;
            foreach (float freq in currentChord) {
                // Her nota için 3 detuned katman
                pad += Mathf.Sin(2 * Mathf.PI * freq * t) * 0.12f;
                pad += Mathf.Sin(2 * Mathf.PI * (freq * 1.002f) * t) * 0.08f;
                pad += Mathf.Sin(2 * Mathf.PI * (freq * 0.998f) * t) * 0.08f;
            }

            // Yavaş "Breathing" (Nefes alma) efekti - Pad'in sesini yavaşça açar kapatır
            float breathe = 0.5f + 0.35f * Mathf.Sin(t * 0.5f);
            
            // 5. Virtual Low-Pass Filter: Keskinlikleri yok et (Çınlamayı engelle)
            // Bir önceki örnekle ortalamasını alarak yüksek frekansları "yutar"
            float currentRaw = pad * breathe;
            float filtered = (currentRaw + prevSample) * 0.5f;
            prevSample = filtered;

            data[i] = Mathf.Clamp(filtered, -1f, 1f) * 0.75f;
        }

        // Başlangıç ve bitişte çok yumuşak bir fade in/out (0.5 saniye)
        ApplyFadeInOut(data, SAMPLE_RATE, 0.5f);
        return MakeClip(data, "SuperSmoothAmbient_V3");
    }

    /// <summary>
    /// Buton tıklama – kısa enerjik whoosh + click transient
    /// </summary>
    private AudioClip MakeButtonClick()
    {
        float dur    = 0.18f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 18f); // Hızlı decay

            // Click transient (yüksek frekans patlayışı)
            float click = Mathf.Sin(2 * Mathf.PI * 1200f * t) * env;

            // Swoosh (frekans hızla düşer)
            float sweepFreq = Mathf.Lerp(800f, 200f, t / dur);
            float sweep = Mathf.Sin(2 * Mathf.PI * sweepFreq * t) * env * 0.5f;

            data[i] = (click + sweep) * 0.85f;
        }

        return MakeClip(data, "ButtonClick");
    }

    /// <summary>
    /// Oyun başlangıcı – yükselen dramatik güç sesi
    /// </summary>
    private AudioClip MakeGameStart()
    {
        float dur    = 1.2f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Pow(1f - (t / dur), 0.5f) * Mathf.Min(t / 0.05f, 1f);

            // Yükselen frekans sweep: 100 → 600 Hz
            float freq = Mathf.Lerp(100f, 600f, t / dur);
            float sweep = Mathf.Sin(2 * Mathf.PI * freq * t);

            // Harmonik katmanı
            float harm = Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * 0.35f;
            float harm2 = Mathf.Sin(2 * Mathf.PI * freq * 3f * t) * 0.15f;

            // Gürültü katmanı (güç hissi verir)
            float noise = (Random.value * 2f - 1f) * 0.08f;

            data[i] = (sweep + harm + harm2 + noise) * env * 0.9f;
        }

        ApplyFadeInOut(data, SAMPLE_RATE, 0.03f);
        return MakeClip(data, "GameStart");
    }

    /// <summary>
    /// Taş yerleştirme – metalik tokmak + manyetik rezonans
    /// </summary>
    private AudioClip MakeStonePlaced()
    {
        float dur    = 0.65f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // 1. Keskin Manyetik "Click" (Üst Frekans)
            float clickEnv = Mathf.Exp(-t * 40f);
            float click = Mathf.Sin(2 * Mathf.PI * 1800f * t) * clickEnv;

            // 2. Fiziksel "Thump" (Alt Frekans - Yere oturma hissi)
            float thumpEnv = Mathf.Exp(-t * 25f);
            float thump = Mathf.Sin(2 * Mathf.PI * 90f * (1f - t * 5f)) * thumpEnv * 1.2f;

            // 3. Manyetik Çınlama (Resonans)
            float resEnv = Mathf.Exp(-t * 8f);
            float ring = Mathf.Sin(2 * Mathf.PI * 440f * t) * resEnv * 0.3f;

            // 4. White noise patlaması (Çok kısa transient)
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 80f) * 0.5f;

            data[i] = Mathf.Clamp(click + thump + ring + noise, -1f, 1f) * 0.9f;
        }

        return MakeClip(data, "StonePlacedHeavy");
    }

    /// <summary>
    /// Taşı rezervden alırken çıkan ses - Yükselen manyetik vınlama
    /// </summary>
    private AudioClip MakeStonePick()
    {
        float dur    = 0.25f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = t / dur;
            
            // Yükselen frekans 300 -> 800 Hz
            float freq = Mathf.Lerp(300f, 800f, progress);
            float env = Mathf.Sin(Mathf.PI * progress); // Ortada şişkin
            
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.7f;
        }

        return MakeClip(data, "StonePickRising");
    }

    /// <summary>
    /// Taşı alma / sürükleme başlangıcı – yumuşak manyetik çekim sesi
    /// </summary>
    private AudioClip MakeStoneDrag()
    {
        float dur    = 0.22f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            // Hafif yükselen whoosh: 250 → 500 Hz
            float freq = Mathf.Lerp(250f, 500f, t / dur);
            float env  = Mathf.Sin(Mathf.PI * t / dur); // Ortada yüksek
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.6f;
        }

        return MakeClip(data, "StoneDrag");
    }

    /// <summary>
    /// Taş çarpışması – güçlü darbe + dağılan enerji
    /// </summary>
    private AudioClip MakeCollision()
    {
        float dur    = 0.70f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // Güçlü impact transient
            float impEnv = Mathf.Exp(-t * 15f);
            float impact = Mathf.Sin(2 * Mathf.PI * 180f * t) * impEnv;

            // Crunch noise
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 20f) * 0.55f;

            // Düşen enerji "boom"
            float boomFreq = Mathf.Lerp(200f, 60f, t / dur);
            float boom = Mathf.Sin(2 * Mathf.PI * boomFreq * t) * Mathf.Exp(-t * 8f) * 0.35f;

            data[i] = Mathf.Clamp((impact + noise + boom) * 0.9f, -1f, 1f);
        }

        return MakeClip(data, "Collision");
    }

    /// <summary>
    /// Sıra değişimi – neon chime (iki ton)
    /// </summary>
    private AudioClip MakeTurnChange()
    {
        float dur    = 0.45f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        float[] notes = { 660f, 880f }; // İki ton: artan
        float noteDur = dur / notes.Length;

        for (int i = 0; i < samps; i++)
        {
            float t       = (float)i / SAMPLE_RATE;
            int   noteIdx = Mathf.Min((int)(t / noteDur), notes.Length - 1);
            float localT  = t - noteIdx * noteDur;
            float env     = Mathf.Exp(-localT * 12f) * Mathf.Min(localT / 0.005f, 1f);

            float freq    = notes[noteIdx];
            float tone    = Mathf.Sin(2 * Mathf.PI * freq * t) * env;
            float harm    = Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * env * 0.2f;

            data[i] = (tone + harm) * 0.8f;
        }

        return MakeClip(data, "TurnChange");
    }

    /// <summary>
    /// Ceza sesi – rahatsız edici inen tone
    /// </summary>
    private AudioClip MakePenalty()
    {
        float dur    = 0.55f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            // İnen sweep: 500 → 120 Hz (bozuk/kötü his)
            float freq = Mathf.Lerp(500f, 120f, t / dur);
            float env  = Mathf.Exp(-t * 4f) * Mathf.Min(t / 0.01f, 1f);

            // Küçük sapma (detuned) – rahatsız edici
            float detuned = Mathf.Sin(2 * Mathf.PI * (freq * 1.04f) * t);
            float main    = Mathf.Sin(2 * Mathf.PI * freq * t);

            data[i] = ((main + detuned * 0.4f) * env) * 0.9f;
        }

        return MakeClip(data, "Penalty");
    }

    /// <summary>
    /// Kazanma sesi – yükselen arpeji + parlak son
    /// </summary>
    private AudioClip MakeWin()
    {
        float dur    = 1.6f;
        int   samps  = Mathf.CeilToInt(SAMPLE_RATE * dur);
        float[] data = new float[samps];

        // Do Majör arpeji: Do-Mi-Sol-Do(oktav)
        float[] freqs   = { 262f, 330f, 392f, 524f, 660f };
        float   noteLen = 0.22f;

        for (int i = 0; i < samps; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            float sample = 0f;
            for (int n = 0; n < freqs.Length; n++)
            {
                float noteStart = n * noteLen;
                float noteEnd   = noteStart + noteLen * 1.4f; // Biraz uzun (reverb hissi)
                if (t >= noteStart && t < noteEnd)
                {
                    float lt   = t - noteStart;
                    float nEnv = Mathf.Exp(-lt * 6f) * Mathf.Min(lt / 0.01f, 1f);
                    sample += Mathf.Sin(2 * Mathf.PI * freqs[n] * t) * nEnv * 0.5f;
                    sample += Mathf.Sin(2 * Mathf.PI * freqs[n] * 2f * t) * nEnv * 0.15f;
                }
            }

            // Genel envelope
            float globalEnv = Mathf.Clamp01(1f - (t - 1.2f) / 0.4f); // Son 0.4sn fade out
            data[i] = Mathf.Clamp(sample * globalEnv, -1f, 1f);
        }

        return MakeClip(data, "Win");
    }

    // ──────────────────────────────────────────────
    //  YARDIMCI METOTLAR
    // ──────────────────────────────────────────────

    private AudioClip MakeClip(float[] samples, string name)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>Başlangıç ve bitişe kısa fade uygular (döngü dikişini gizler)</summary>
    private void ApplyFadeInOut(float[] data, int sampleRate, float fadeDuration)
    {
        int fadeSamples = Mathf.CeilToInt(sampleRate * fadeDuration);
        for (int i = 0; i < fadeSamples && i < data.Length; i++)
        {
            float t = (float)i / fadeSamples;
            data[i] *= t;
            data[data.Length - 1 - i] *= t;
        }
    }
}
