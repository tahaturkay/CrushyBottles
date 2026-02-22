using UnityEngine;

public class Block : MonoBehaviour
{
    public int x; // Griddeki X koordinatı (Sütun)
    public int y; // Griddeki Y koordinatı (Satır)
    public int colorID; // 0:Mavi, 1:Kırmızı, 2:Sarı... (Rengin kimliği)
    // YENİ: Yöneticiye erişim
    public BoardManager board;
    private SpriteRenderer myRenderer;

    // Blok ilk oluştuğunda çalışır
    void Awake()
    {
        myRenderer = GetComponent<SpriteRenderer>();
    }

    // Blokun özelliklerini dışarıdan atayan fonksiyon
    public void Setup(int _x, int _y, int _colorID, Sprite _initialSprite, BoardManager _board)
    {
        x = _x;
        y = _y;
        colorID = _colorID;
        myRenderer.sprite = _initialSprite;
        board = _board; // Yöneticiyi kaydet
        name = $"Block_{x}_{y}"; 
    }

    // YENİ: Tıklanma Olayı
    void OnMouseDown()
    {
        // Yöneticiye "Bana tıklandı!" diye haber ver
        board.BlockClicked(x, y, colorID);
    }

    // Bu fonksiyon, grup büyüklüğüne (count) bakıp resmimi değiştirir
    public void UpdateSprite(int groupSize, BoardManager.ColorData myData, int A, int B, int C)
    {
        if (groupSize > C) 
        {
            GetComponent<SpriteRenderer>().sprite = myData.iconC;
        }
        else if (groupSize > B)
        {
            GetComponent<SpriteRenderer>().sprite = myData.iconB;
        }
        else if (groupSize > A)
        {
            GetComponent<SpriteRenderer>().sprite = myData.iconA;
        }
        else 
        {
            GetComponent<SpriteRenderer>().sprite = myData.iconDefault;
        }
    }

    // Bu fonksiyon bloğu A noktasından B noktasına yumuşakça taşır
    public void MoveToPosition(Vector2 targetPosition, float duration)
    {
        StartCoroutine(AnimateMove(targetPosition, duration));
    }

    // Coroutine: İşlemi zamana yaymak için kullanılır
    System.Collections.IEnumerator AnimateMove(Vector2 target, float duration)
    {
        Vector2 startPosition = transform.position;
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            // Lerp: İki nokta arasını yumuşakça tamamlar
            transform.position = Vector2.Lerp(startPosition, target, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null; // Bir sonraki kareyi bekle
        }

        // Garanti olsun diye tam hedefe oturt
        transform.position = target;
    }
}