using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class Reel : MonoBehaviour
{
    [SerializeField] private RectTransform symbolsContainer;
    [SerializeField] private GameObject symbolPrefab;
    [SerializeField] private int visibleSymbols = 3;
    [SerializeField] private float symbolHeight = 100f;
    private readonly string[] testSymbols = { "‡", "†", "‰", "♣", "♦", "♥", "♠" };

    private List<GameObject> symbolObjects = new List<GameObject>();
    private List<string> symbols = new List<string>();
    public bool IsStopped { get; private set; }

    private void Start()
    {
        GenerateSymbols(testSymbols, 51);
    }

    public void GenerateSymbols(string[] availableSymbols, int totalSymbols)
    {
        if (availableSymbols == null || availableSymbols.Length == 0)
        {
            Debug.LogError("Tablica symboli jest pusta! Nie można wygenerować symboli.");
            return;
        }

        foreach (var obj in symbolObjects)
            Destroy(obj);
        symbolObjects.Clear();
        symbols.Clear();

        for (int i = 0; i < totalSymbols; i++)
        {
            string symbol = availableSymbols[Random.Range(0, availableSymbols.Length)];
            symbols.Add(symbol);

            var go = Instantiate(symbolPrefab, symbolsContainer);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = symbol;
            text.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, (i - 1) * symbolHeight);
            rt.localScale = Vector3.one; 
            symbolObjects.Add(go);
        }

        symbolsContainer.anchoredPosition = Vector2.zero;
    }

    public IEnumerator Spin(float duration, string[] availableSymbols, string[] forcedSymbols = null)
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
                string sym = i < forcedSymbols.Length ? forcedSymbols[i] : availableSymbols[0];
                int idx = totalSymbols - visibleSymbols + i;

                if (idx >= 0 && idx < symbols.Count) symbols[idx] = sym;
                if (idx >= 0 && idx < symbolObjects.Count)
                {
                    var tmp = symbolObjects[idx].GetComponent<TextMeshProUGUI>();
                    tmp.text = sym;
                    tmp.color = Color.white;
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

    public string GetSymbol(int row)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return "";
        return symbolObjects[idx].GetComponent<TextMeshProUGUI>().text;
    }

    public void HighlightSymbol(int row, Color color)
    {
        int idx = symbolObjects.Count - visibleSymbols + row;
        if (idx < 0 || idx >= symbolObjects.Count) return;
        symbolObjects[idx].GetComponent<TextMeshProUGUI>().color = color;
    }

    public void ResetHighlight()
    {
        foreach (var obj in symbolObjects)
            obj.GetComponent<TextMeshProUGUI>().color = Color.white;
    }


    public void PulseSymbol(int row, float targetScale = 1.5f, float duration = 0.08f, Ease ease = Ease.OutCubic, int loops = -1)
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