using UnityEngine;
using UnityEngine.UI; // UI elemanlarına erişmek için şart
using UnityEngine.SceneManagement; // Oyunu yeniden başlatmak için

public class GameManager : MonoBehaviour
{
    [Header("Oyun Ayarları")]
    public float timeLimit = 60f; // 1 Dakika
    public int moveLimit = 25;    // 25 Hamle
    
    [Header("UI Bağlantıları")]
    public Text scoreText;
    public Text timeText;
    public Text movesText;
    public Text highScoreText;
    public GameObject gameOverPanel;
    public Text finalScoreText;

    private int currentScore = 0;
    private int currentHighScore = 0;
    public bool isGameActive = true;

    void Start()
    {
        // Oyunu 120 Kare/Saniye hızına zorla
        Application.targetFrameRate = 120;
        // High Score'u hafızadan yükle
        currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = "En İyi: " + currentHighScore;

        UpdateUI();
    }

    void Update()
    {
        if (!isGameActive) return; // Oyun bittiyse süreyi durdur

        // Süreyi geri say
        if (timeLimit > 0)
        {
            timeLimit -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            GameOver(); // Süre bitti!
        }
    }

    // Skoru Artırma Fonksiyonu
    public void AddScore(int points)
    {
        if (!isGameActive) return;

        currentScore += points;
        
        // High Score geçildi mi?
        if (currentScore > currentHighScore)
        {
            currentHighScore = currentScore;
            highScoreText.text = "En İyi: " + currentHighScore;
            PlayerPrefs.SetInt("HighScore", currentHighScore); // Kaydet
        }

        UpdateUI();
    }

    // Hamle Yapma Fonksiyonu
    public void UseMove()
    {
        if (!isGameActive) return;

        moveLimit--;
        UpdateUI();

        if (moveLimit <= 0)
        {
            GameOver(); // Hamle bitti!
        }
    }

    void UpdateUI()
    {
        scoreText.text = "Skor: " + currentScore;
        movesText.text = "Hamle: " + moveLimit;
    }

    void UpdateTimerUI()
    {
        // Süreyi tam sayıya yuvarla (60, 59, 58...)
        timeText.text = "Süre: " + Mathf.CeilToInt(timeLimit).ToString();
    }

    void GameOver()
    {
        isGameActive = false;
        gameOverPanel.SetActive(true); // Paneli aç
        finalScoreText.text = "Skorun: " + currentScore;
    }

    // Butona bağlanacak fonksiyon
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}