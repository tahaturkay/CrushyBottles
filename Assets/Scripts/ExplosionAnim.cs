using UnityEngine;

public class ExplosionAnim : MonoBehaviour
{
    public float duration = 0.5f; // Ne kadar sürsün?

    void Start()
    {
        // Rengi BoardManager'dan alacağız ama varsayılan olarak çalışsın
        StartCoroutine(AnimatePop());
    }

    System.Collections.IEnumerator AnimatePop()
    {
        float timer = 0;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 2f; // Patlayınca 2 katına çıksın
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color startColor = sr.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // 1. Büyüme Efekti (Küçükten büyüğe)
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

            // 2. Şeffaflaşma (Görünürden Görünmeze)
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1, 0, progress); // Alpha 1'den 0'a iner
            sr.color = newColor;

            yield return null;
        }

        // İş bitince kendini yok et
        Destroy(gameObject);
    }
}