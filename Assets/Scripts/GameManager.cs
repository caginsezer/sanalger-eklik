using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ana oyun yöneticisi - Sıra takibi, kazanma kontrolü, ceza sistemi
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Oyun Ayarları")]
    public int magnetsPerPlayer = 18; // 12'den 18'e yükseltildi (Daha zorlayıcı)
    public float attractionCheckDelay = 0.5f; // Çok beklemeye gerek yok
    public float clusterDistance = 1.1f; // Taşların birleşmiş (ceza) sayılması için mesafe bir tık artırıldı (Eski: 0.8f)
    public float maxTurnTime = 10f;
    private float currentTurnTime;

    // Oyun Durumları
    public enum GameState { MainMenu, WaitingForPlacement, CheckingAttraction, GameOver }
    public GameState currentState = GameState.MainMenu;

    // Oyuncu bilgileri
    public int currentPlayer = 1; // 1 veya 2
    public int player1RemainingMagnets;
    public int player2RemainingMagnets;
    public int player1Score = 0;
    public int player2Score = 0;

    // Sahadaki mıknatıslar
    public List<MagnetPiece> placedMagnets = new List<MagnetPiece>();
    
    // Geri Alma (Undo) Verileri
    private MagnetPiece lastPlacedMagnet;
    private int lastPlayerWhoMoved;
    private int lastScoreChange;
    private Coroutine currentCheckCoroutine;

    // Events
    public System.Action<int> OnTurnChanged;
    public System.Action<int, int> OnMagnetsUpdated; // player1Count, player2Count
    public System.Action<int, int> OnScoresUpdated;  // player1Score, player2Score
    public System.Action<string> OnStatusMessage;
    public System.Action<int> OnGameOver; // winner player number
    public System.Action<float> OnTimerUpdated; // normalized time 0 to 1

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // KODDAN ZORLA AYARLA (Inspector ayarlarını ezmesi için)
            magnetsPerPlayer = 18; 
            maxTurnTime = 10f;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Artık oyun hemen başlamıyor. Main Menu çağrılmasını bekleyecek.
        // StartNewGame() çağrısı StartGameFromMenu() fonksiyonuna taşındı.
    }

    public void StartGameFromMenu()
    {
        currentState = GameState.WaitingForPlacement;

        // Önce eski taşları temizle, sonra yeniden oluştur
        var reserveMgr = FindObjectOfType<StoneReserveManager>();
        if (reserveMgr != null)
        {
            reserveMgr.ClearAll();
            reserveMgr.Initialize(magnetsPerPlayer);
        }

        // In-game UI'yı göster
        FindObjectOfType<UIManager>()?.ShowGameUI();

        // SES: Gameplay Fon Müziğini Başlat (Efekt Intro'da çalındı)
        AudioManager.Instance?.Play(AudioManager.SoundType.GameAmbiance);

        StartNewGame();
    }

    private void Update()
    {
        if (currentState == GameState.WaitingForPlacement)
        {
            currentTurnTime -= Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(currentTurnTime / maxTurnTime);
            OnTimerUpdated?.Invoke(normalizedTime);

            if (currentTurnTime <= 0)
            {
                currentTurnTime = 0;
                OnTimerUpdated?.Invoke(0);
                OnStatusMessage?.Invoke($"Süre doldu! Sıra değişiyor...");
                SwitchTurn();
            }
        }
    }

    public void StartNewGame()
    {
        // Sahadaki tüm mıknatısları temizle
        foreach (var magnet in placedMagnets)
        {
            if (magnet != null)
                Destroy(magnet.gameObject);
        }
        placedMagnets.Clear();

        // Değerleri sıfırla
        player1RemainingMagnets = magnetsPerPlayer;
        player2RemainingMagnets = magnetsPerPlayer;
        player1Score = 0;
        player2Score = 0;
        currentPlayer = 1;
        currentTurnTime = maxTurnTime;
        currentState = GameManager.GameState.WaitingForPlacement;

        OnTurnChanged?.Invoke(currentPlayer);
        OnTimerUpdated?.Invoke(1.0f);
        OnMagnetsUpdated?.Invoke(player1RemainingMagnets, player2RemainingMagnets);
        OnScoresUpdated?.Invoke(player1Score, player2Score);
        OnStatusMessage?.Invoke("Oyun Başladı! Oyuncu 1'in sırası.");
    }

    /// <summary>
    /// Mıknatıs yerleştirildiğinde çağrılır
    /// </summary>
    public void OnMagnetPlaced(MagnetPiece magnet)
    {
        if (currentState != GameState.WaitingForPlacement) return;

        magnet.ownerPlayer = currentPlayer;
        magnet.isPlaced = true;
        placedMagnets.Add(magnet);

        // Kalan mıknatısları azalt
        if (currentPlayer == 1)
            player1RemainingMagnets--;
        else
            player2RemainingMagnets--;

        OnMagnetsUpdated?.Invoke(player1RemainingMagnets, player2RemainingMagnets);

        // Geri alma (Undo) için kaydet
        lastPlacedMagnet = magnet;
        lastPlayerWhoMoved = currentPlayer;
        lastScoreChange = 0; // Şimdilik 0, CheckAttractions sonunda güncellenecek

        // Çekim kontrolü başlat
        currentState = GameState.CheckingAttraction;
        currentCheckCoroutine = StartCoroutine(CheckAttractionsAfterDelay());
    }

    private IEnumerator CheckAttractionsAfterDelay()
    {
        OnStatusMessage?.Invoke("Miknatislar kontrol ediliyor...");

        // Mıknatısların fiziksel olarak hareket etmesi için bekle
        yield return new WaitForSeconds(attractionCheckDelay);

        // Kümelenen (çarpışan) mıknatısları bul
        List<MagnetPiece> clusteredMagnets = FindClusteredMagnets();

        if (clusteredMagnets.Count > 0)
        {
            // CEZA: Kümelenen mıknatıslar mevcut oyuncuya geri verilir
            int penaltyCount = clusteredMagnets.Count;

            // Skor Cezası (Taş başına -50 puan)
            int penaltyScore = penaltyCount * 50;
            if (currentPlayer == 1) player1Score = Mathf.Max(0, player1Score - penaltyScore);
            else player2Score = Mathf.Max(0, player2Score - penaltyScore);

            OnStatusMessage?.Invoke($"Taslar birlesdi! Oyuncu {currentPlayer} ceza: {penaltyCount} miknatıs aldi! (-{penaltyScore} Skor)");

            // SES: Ceza/Çarpışma sesi
            AudioManager.Instance?.Play(AudioManager.SoundType.Penalty);

            // Mıknatısları sahadan kaldır ve oyuncuya görsel olarak geri ver
            foreach (var magnet in clusteredMagnets)
            {
                placedMagnets.Remove(magnet);
                Destroy(magnet.gameObject);
                
                // Taş görsel olarak yan panele (rezerv) döner
                StoneReserveManager.Instance?.ReturnStone(currentPlayer);
            }

            if (currentPlayer == 1)
                player1RemainingMagnets += penaltyCount;
            else
                player2RemainingMagnets += penaltyCount;

            OnMagnetsUpdated?.Invoke(player1RemainingMagnets, player2RemainingMagnets);
            OnScoresUpdated?.Invoke(player1Score, player2Score);
        }
        else
        {
            // BAŞARILI YERLEŞTİRME ÖDÜLÜ
            // Zaman bonusu + temel puan
            int baseScore = 100;
            int timeBonus = Mathf.FloorToInt((currentTurnTime / maxTurnTime) * 50);
            earnedScoreAtThisTurn = baseScore + timeBonus;
            int earnedScore = earnedScoreAtThisTurn;

            if (currentPlayer == 1) player1Score += earnedScore;
            else player2Score += earnedScore;

            OnScoresUpdated?.Invoke(player1Score, player2Score);
            OnStatusMessage?.Invoke($"Miknatıs basariyla yerlestirildi! (+{earnedScore} Puan)");
        }

        // Skor değişimini Geri Al (Undo) için sakla (CheckAttractions sonunda)
        lastScoreChange = earnedScoreAtThisTurn;

        // Kazanma kontrolü
        if (CheckWinCondition())
        {
            // Kazanan ekstra 1000 puan alır
            if (player1RemainingMagnets <= 0) player1Score += 1000;
            if (player2RemainingMagnets <= 0) player2Score += 1000;
            OnScoresUpdated?.Invoke(player1Score, player2Score);
            yield break;
        }

        currentCheckCoroutine = null;
        // Sırayı değiştir
        SwitchTurn();
    }

    private int earnedScoreAtThisTurn = 0;

    private List<MagnetPiece> FindClusteredMagnets()
    {
        HashSet<MagnetPiece> clustered = new HashSet<MagnetPiece>();

        for (int i = 0; i < placedMagnets.Count; i++)
        {
            for (int j = i + 1; j < placedMagnets.Count; j++)
            {
                if (placedMagnets[i] == null || placedMagnets[j] == null) continue;

                float distance = Vector3.Distance(
                    placedMagnets[i].transform.position,
                    placedMagnets[j].transform.position
                );

                // Eğer iki mıknatıs çok yakınsa (yapışmışsa)
                if (distance < clusterDistance)
                {
                    clustered.Add(placedMagnets[i]);
                    clustered.Add(placedMagnets[j]);
                }
            }
        }

        return new List<MagnetPiece>(clustered);
    }

    private bool CheckWinCondition()
    {
        if (player1RemainingMagnets <= 0)
        {
            currentState = GameState.GameOver;
            OnStatusMessage?.Invoke("Oyuncu 1 Kazandi!");
            OnGameOver?.Invoke(1);
            
            // SES: Kazanma sesi
            AudioManager.Instance?.Play(AudioManager.SoundType.Win);
            
            return true;
        }
        else if (player2RemainingMagnets <= 0)
        {
            currentState = GameState.GameOver;
            OnStatusMessage?.Invoke("Oyuncu 2 Kazandi!");
            OnGameOver?.Invoke(2);

            // SES: Kazanma sesi
            AudioManager.Instance?.Play(AudioManager.SoundType.Win);

            return true;
        }
        return false;
    }

    private void SwitchTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        currentState = GameState.WaitingForPlacement;
        currentTurnTime = maxTurnTime;

        // SES: Sıra değişim sesi
        AudioManager.Instance?.Play(AudioManager.SoundType.TurnChange);

        OnTurnChanged?.Invoke(currentPlayer);
        OnTimerUpdated?.Invoke(1.0f);
        OnStatusMessage?.Invoke($"Oyuncu {currentPlayer}'in sirasi.");
    }

    /// <summary>
    /// En son yapılan hamleyi geri alır.
    /// </summary>
    public void UndoLastMove()
    {
        Debug.Log($"[GameManager] UndoLastMove calistiriliyor. lastPlacedMagnet: {(lastPlacedMagnet != null ? lastPlacedMagnet.name : "null")}");
        
        if (currentState == GameState.GameOver || lastPlacedMagnet == null) return;

        // 1. Devam eden çekim kontrolünü durdur
        if (currentCheckCoroutine != null)
        {
            StopCoroutine(currentCheckCoroutine);
            currentCheckCoroutine = null;
        }

        // 2. Taşı sahadan kaldır
        placedMagnets.Remove(lastPlacedMagnet);
        Destroy(lastPlacedMagnet.gameObject);

        // 3. Rezerv sayısını ve skorunu geri yükle
        if (lastPlayerWhoMoved == 1)
        {
            player1RemainingMagnets++;
            player1Score = Mathf.Max(0, player1Score - lastScoreChange);
        }
        else
        {
            player2RemainingMagnets++;
            player2Score = Mathf.Max(0, player2Score - lastScoreChange);
        }

        // 4. Taşı görsel olarak rezervine geri ver
        StoneReserveManager.Instance?.ReturnStone(lastPlayerWhoMoved);

        // 5. Sırayı hamleyi yapan oyuncuya geri çevir
        currentPlayer = lastPlayerWhoMoved;
        currentState = GameState.WaitingForPlacement;
        currentTurnTime = maxTurnTime;

        // 6. UI ve Ses
        AudioManager.Instance?.Play(AudioManager.SoundType.ButtonClick);
        OnMagnetsUpdated?.Invoke(player1RemainingMagnets, player2RemainingMagnets);
        OnScoresUpdated?.Invoke(player1Score, player2Score);
        OnTurnChanged?.Invoke(currentPlayer);
        OnStatusMessage?.Invoke($"Hamle geri alindi! Oyuncu {currentPlayer} tekrar oynuyor.");

        lastPlacedMagnet = null; // Sadece bir kez geri alınabilir
    }

    public Color GetPlayerColor(int player)
    {
        if (player == 1)
            return new Color(0.2f, 0.6f, 1f, 1f); // Mavi
        else
            return new Color(1f, 0.5f, 0f, 1f); // Turuncu
    }
}
