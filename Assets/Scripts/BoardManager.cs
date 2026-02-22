using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    // --- YAPILANDIRMA ---
    [Header("Board Settings")]
    public int width = 8;
    public int height = 10;
    
    [Header("Icon Thresholds")]
    public int conditionA = 4;
    public int conditionB = 7;
    public int conditionC = 9;
    public GameObject explosionPrefab;
    public GameManager gameManager;

    // --- RENK VE İKON SİSTEMİ ---
    [System.Serializable]
    public struct ColorData
    {
        public string name;      
        public Color particleColor;  
        public Sprite iconDefault;  
        public Sprite iconA;        
        public Sprite iconB;        
        public Sprite iconC;        
    }

    public ColorData[] allColors; 
    public GameObject blockPrefab; 

    private Block[,] grid; 
    private bool isShuffling = false; // Karıştırma işlemi yapılıyor mu?

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        grid = new Block[width, height]; 

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateBlock(x, y);
            }
        }
        
        Camera.main.transform.position = new Vector3((float)(width-1)/2, (float)(height-1)/2, -10);
        // TAHTA OLUŞUNCA İKONLARI AYARLA
        UpdateAllIcons();
    }

    void CreateBlock(int x, int y)
    {
        int randomColorIndex = Random.Range(0, allColors.Length);
        ColorData selectedColor = allColors[randomColorIndex];

        GameObject newObj = Instantiate(blockPrefab, new Vector2(x, y), Quaternion.identity);
        newObj.transform.parent = this.transform;

        Block blockScript = newObj.GetComponent<Block>();
        
        if(blockScript != null)
        {
             blockScript.Setup(x, y, randomColorIndex, selectedColor.iconDefault, this);
             grid[x, y] = blockScript;
        }
    }

    // --- PATLATMA MEKANİĞİ ---

    public void BlockClicked(int x, int y, int clickedColorID)
    {
        // --- GÜVENLİK KONTROLÜ ---

        // Eğer oyun devam etmiyorsa (Süre bitti veya Hamle bitti), 
        // buradaki kodların hiçbirini çalıştırma, geri dön.
        if (gameManager != null && !gameManager.isGameActive) 
        {
            return; 
        }

        // Eğer şu an karıştırma animasyonu oynuyorsa tıklamayı yasakla
        if (isShuffling) return;
        
        List<Block> matchedBlocks = new List<Block>();
        bool[,] visited = new bool[width, height];

        // Algoritmayı çalıştır
        FindMatches(x, y, clickedColorID, matchedBlocks, visited);

        // Kural: En az 2 blok varsa patlat
        if (matchedBlocks.Count >= 2)
        {
            Debug.Log($"PATLADI! Toplam {matchedBlocks.Count} blok yok edildi.");
            
            // --- PUAN HESAPLAMA ---
            int count = matchedBlocks.Count;
            float multiplier = 1f; // Varsayılan çarpan (1x)

            // A'dan büyükse 1.2x, B'den büyükse 1.5x, C'den büyükse 2x
            if (count > conditionC)
                multiplier = 2.0f;
            else if (count > conditionB)
                multiplier = 1.5f;
            else if (count > conditionA)
                multiplier = 1.2f;

            // Her blok 100 puan. Formül: (BlokSayısı * 100) * Çarpan
            int baseScore = count * 100;
            int finalScore = Mathf.RoundToInt(baseScore * multiplier);

            // GameManager'a gönder
            if(gameManager != null)
            {
                gameManager.AddScore(finalScore);
                gameManager.UseMove(); // Bir hamle eksilt
            }
            // ------------------------------------

            foreach (Block b in matchedBlocks)
            {
                // Bloğu kapatmadan önce orada bir patlama yarat!
                PlayExplosion(b.transform.position, b.colorID);
                b.gameObject.SetActive(false); // Görünmez yap
                grid[b.x, b.y] = null; // Mantıksal haritadan sil
            }
            // Bloklar yok oldu, şimdi yerçekimini çalıştır!
            ApplyGravity();
            // TAHTA OLUŞUNCA İKONLARI AYARLA
            UpdateAllIcons();

            // --- YENİ EKLENEN KISIM ---
            if (IsDeadlocked())
            {
                StartCoroutine(ShuffleBoardRoutine());
            }
        }
    }

    void FindMatches(int x, int y, int targetColor, List<Block> resultList, bool[,] visited)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (visited[x, y]) return;
        if (grid[x, y] == null || grid[x, y].colorID != targetColor) return;

        visited[x, y] = true;
        resultList.Add(grid[x, y]);

        FindMatches(x + 1, y, targetColor, resultList, visited);
        FindMatches(x - 1, y, targetColor, resultList, visited);
        FindMatches(x, y + 1, targetColor, resultList, visited);
        FindMatches(x, y - 1, targetColor, resultList, visited);
    }

    // --- YERÇEKİMİ VE DOLDURMA ---

   public void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;

            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    Block blockToMove = grid[x, y];
                    grid[x, y] = null;
                    int newY = y - emptyCount;
                    grid[x, newY] = blockToMove;
                    
                    blockToMove.x = x;
                    blockToMove.y = newY;
                    blockToMove.name = $"Block_{x}_{newY}";
                    blockToMove.MoveToPosition(new Vector2(x, newY), 0.2f); 
                }
            }
            RefillColumn(x, emptyCount);
        }
    }

    void RefillColumn(int x, int emptyCount)
    {
        for (int i = 0; i < emptyCount; i++)
        {
            int targetY = height - emptyCount + i;
            
            // Bloğu oluştur (Şu an targetY noktasında oluşuyor)
            CreateBlock(x, targetY);
            
            // Grid'deki yeni bloğu buluyoruz
            Block newBlock = grid[x, targetY];

            // 1. Bloğu ekranın yukarısına taşıyalım (Görünmez bir yere)
            // height + 2 diyerek tavanın biraz üstüne ışınlıyoruz başlangıçta
            newBlock.transform.position = new Vector2(x, targetY + 5); // +5 diyerek iyice yukarı aldık
            
            // 2. Şimdi yerine gitmesini söyleyelim
            newBlock.MoveToPosition(new Vector2(x, targetY), 0.2f);
        }
    }

    // --- İKON GÜNCELLEME SİSTEMİ ---

    void UpdateAllIcons()
    {
        // Hangi bloklara baktığımızı not etmek için bir harita
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Eğer bu kutu boşsa veya zaten baktıysak geç
                if (grid[x, y] == null || visited[x, y]) continue;

                // Yeni bir grup bulduk! Kaç kişi olduklarını sayıyoruz
                List<Block> group = new List<Block>();
                int colorID = grid[x, y].colorID;

                // Flood Fill ile grubu bul
                FindMatches(x, y, colorID, group, visited);

                // Grubun büyüklüğünü al
                int groupSize = group.Count;

                // O rengin tüm resim verilerini (Data) al
                ColorData data = allColors[colorID];

                // Gruptaki HERKESİN resmini güncelle
                foreach (Block b in group)
                {
                    b.UpdateSprite(groupSize, data, conditionA, conditionB, conditionC);
                }
            }
        }
    }
    // Tahtada patlatılacak bir ikili var mı?
    bool IsDeadlocked()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    // Sadece Sağa ve Yukarı bakmak yeterlidir (Çift kontrolü önlemek için)
                    int currentId = grid[x, y].colorID;
                    
                    // Sağımdaki ile aynı mıyım?
                    if (x + 1 < width && grid[x + 1, y] != null && grid[x + 1, y].colorID == currentId)
                        return false; // Hayır, hamle var!
                        
                    // Üstümdeki ile aynı mıyım?
                    if (y + 1 < height && grid[x, y + 1] != null && grid[x, y + 1].colorID == currentId)
                        return false; // Hayır, hamle var!
                }
            }
        }
        // Döngü bittiyse ve hiç 'return false' olmadıysa, hamle yok demektir.
        return true; 
    }

    System.Collections.IEnumerator ShuffleBoardRoutine()
    {
        isShuffling = true; 
        Debug.Log("Deadlock bulundu! Tahta karıştırılıyor...");

        yield return new WaitForSeconds(0.4f);

        // --- YENİ EKLENEN KISIM: Prefabın orijinal boyutunu otomatik al (Örn: 0.7) ---
        float defaultScale = blockPrefab.transform.localScale.x; 
        // -----------------------------------------------------------------------------

        float duration = 0.3f; 
        float elapsed = 0f;
        
        // 1. GÖRSEL EFEKT: Küçülme (Orijinal boyuttan 0'a doğru)
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(defaultScale, 0f, elapsed / duration); 
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                        grid[x, y].transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            yield return null; 
        }

        // 2. MANTIKSAL KARIŞTIRMA
        List<int> allColorsOnBoard = new List<int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                    allColorsOnBoard.Add(grid[x, y].colorID);
            }
        }

        // Renkleri çalkala
        for (int i = 0; i < allColorsOnBoard.Count; i++)
        {
            int temp = allColorsOnBoard[i];
            int randomIndex = Random.Range(i, allColorsOnBoard.Count);
            allColorsOnBoard[i] = allColorsOnBoard[randomIndex];
            allColorsOnBoard[randomIndex] = temp;
        }

        // Yeni renkleri ata
        int iterator = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    int newColorID = allColorsOnBoard[iterator];
                    ColorData data = allColors[newColorID];
                    
                    grid[x, y].colorID = newColorID;
                    grid[x, y].GetComponent<SpriteRenderer>().sprite = data.iconDefault; 
                    
                    iterator++;
                }
            }
        }
        
        UpdateAllIcons(); 

        if (IsDeadlocked())
        {
            StartCoroutine(ShuffleBoardRoutine());
            yield break; 
        }

        // 3. GÖRSEL EFEKT: Büyüme (0'dan Orijinal boyuta doğru)
        elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, defaultScale, elapsed / duration); 
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                        grid[x, y].transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            yield return null;
        }

        // Garanti olsun diye boyutları tam olarak kendi orijinal boyutuna sabitle (Küsürat kalmasın)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                    grid[x, y].transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
            }
        }

        isShuffling = false; 
    }

    void PlayExplosion(Vector2 position, int colorID)
{
    // 1. Efekti yarat
    GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);

    // 2. Rengini ayarla
    ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
    var main = ps.main;

    // O ID'ye ait rengi listeden bul ve ata
    main.startColor = allColors[colorID].particleColor;

    // 3. Temizlik: Efekt bitince (1 saniye sonra) objeyi yok et ki hafıza şişmesin
    Destroy(explosion, 1.0f);
}
}