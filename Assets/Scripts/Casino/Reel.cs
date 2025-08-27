using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Reel : MonoBehaviour
{
    [SerializeField] private RectTransform symbolsContainer;
    [SerializeField] private GameObject symbolPrefab; 
    [SerializeField] private int visibleSymbols = 3;
    [SerializeField] private float symbolHeight = 100f;
    [SerializeField] private Sprite[] testSprites;

    private List<GameObject> symbolObjects = new List<GameObject>();
    private List<Sprite> symbolSprites = new List<Sprite>();
    public bool IsStopped { get; private set; }

    private void Start()
    {
        if (testSprites != null && testSprites.Length > 0)
            GenerateSymbols(testSprites, 51);
    }

    public void GenerateSymbols(Sprite[] availableSymbols, int totalSymbols)
    {
        if (availableSymbols == null || availableSymbols.Length == 0)
        {
            Debug.LogError("Tablica sprite’ów jest pusta! Nie można wygenerować symboli.");
            return;
        }

        foreach (var obj in symbolObjects)
            Destroy(obj);
        symbolObjects.Clear();
        symbolSprites.Clear();

        for (int i = 0; i < totalSymbols; i++)
        {
            Sprite sprite = availableSymbols[Random.Range(0, availableSymbols.Length)];
            symbolSprites.Add(sprite);

            var go = Instantiate(symbolPrefab, symbolsContainer);
            var img = go.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogError("symbolPrefab nie posiada komponentu Image.");
                Destroy(go);
                continue;
            }
            img.sprite = sprite;
            img.color = Color.white;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, (i - 1) * symbolHeight);
            rt.localScale = Vector3.one;

            symbolObjects.Add(go);
        }

        symbolsContainer.anchoredPosition = Vector2.zero;
    }

    public IEnumerator Spin(float duration, Sprite[] availableSymbols, Sprite[] forcedSymbols = null)
    {
        IsStopped = false;

        ResetScale();

        int minSymbols = 51;
        int totalSymbols = ((minSymbols + visibleSymbols - 1) / visibleSymbols) * visibleSymbols;

        GenerateSymbols(availableSymbols, totalSymbols);

        if (forcedSymbols != null)
        {
            if (forcedSymbols.Length != visibleSymbols)
                Debug.LogWarning($"Reel.Spin: forcedSymbols length {forcedSymbols.Length} != visibleSymbols {visibleSymbols}. Zostanie obcięte/dopełnione.");

            for (int i = 0; i < visibleSymbols; i++)
            {
                Sprite spr = i < forcedSymbols.Length ? forcedSymbols[i] : availableSymbols[0];
                int idx = totalSymbols - visibleSymbols + i;

                if (idx >= 0 && idx < symbolSprites.Count) symbolSprites[idx] = spr;
                if (idx >= 0 && idx < symbolObjects.Count)
                {
                    var img = symbolObjects[idx].GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = spr;
                        img.color = Color.white;
                    }
                }
            }
        }

        float elapsed = 0f;
        float startOffset = 0f;
        float endOffset = (totalSymbols - visibleSymbols) * symbolHeight;

        while (elapsed < duration)
        {
            float offset = Mathf.Lerp(startOffset, endOffset, elapsed / duration);

            for (int i = 0; i < totalSymbols; i++)
            {
                var rt = symbolObjects[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, (i - 1) * symbolHeight - offset);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < visibleSymbols; i++)
        {
            int idx = totalSymbols - visibleSymbols + i;
            var rt = symbolObjects[idx].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, (i - 1) * symbolHeight);
        }

        IsStopped = true;
    }

    public Sprite GetSymbol(int row)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return null;
        var img = symbolObjects[idx].GetComponent<Image>();
        return img != null ? img.sprite : null;
    }

    public void HighlightSymbol(int row, Color color)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return;
        var img = symbolObjects[idx].GetComponent<Image>();
        if (img != null) img.color = color;
    }

    public void ResetHighlight()
    {
        foreach (var obj in symbolObjects)
        {
            var img = obj.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    public void PulseSymbol(int row, float targetScale = 1.75f, float duration = 0.08f, Ease ease = Ease.OutCubic, int loops = -1)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return;

        var rt = symbolObjects[idx].GetComponent<RectTransform>();

        DOTween.Kill(rt);
        rt.localScale = Vector3.one;

        if (loops > 1)
        {
            rt.DOScale(targetScale, duration)
              .SetEase(ease)
              .SetLoops(loops, LoopType.Yoyo)
              .SetTarget(rt);
        }
        else
        {
            float up = duration;
            float down = duration * 0.1f;

            var seq = DOTween.Sequence().SetTarget(rt);
            seq.Append(rt.DOScale(targetScale, up).SetEase(ease));
            seq.Append(rt.DOScale(1f, down).SetEase(Ease.InCubic));
        }
    }

    public void StopPulse(int row)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return;

        var rt = symbolObjects[idx].GetComponent<RectTransform>();
        DOTween.Kill(rt);
        rt.localScale = Vector3.one;
    }

    public void ResetScale()
    {
        foreach (var obj in symbolObjects)
        {
            var rt = obj.GetComponent<RectTransform>();
            DOTween.Kill(rt);
            rt.localScale = Vector3.one;
        }
    }
}