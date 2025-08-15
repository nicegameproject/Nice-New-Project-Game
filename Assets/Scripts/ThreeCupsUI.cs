using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class ThreeCupsUI : MonoBehaviour
{
    [SerializeField] private Casino casino;
    [SerializeField] private Button cup1Button;
    [SerializeField] private Button cup2Button;
    [SerializeField] private Button cup3Button;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private TextMeshProUGUI betAmountText;
    [SerializeField] private Image ballImage;

    private int betAmount = 0;
    private int minBet = 1;
    private int maxBet = 100;
    private bool canChooseCup = false;

    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action OnCupButtonsEnable;
    public event Action OnCupButtonsDisable;

    void Start()
    {
        UpdateMoney();
        UpdateBetAmountText();
        resultText.text = "";

        OnGameStarted += DisableBetAndPlayButtons;
        OnGameEnded += EnableBetAndPlayButtons;
        OnCupButtonsEnable += () => SetCupButtonsInteractable(true);
        OnCupButtonsDisable += () => SetCupButtonsInteractable(false);

        EnableBetAndPlayButtons();
        OnCupButtonsEnable?.Invoke();
        ballImage.gameObject.SetActive(false);
    }

    public void IncreaseBet()
    {
        if (betAmount + 10 <= maxBet && betAmount + 10 <= casino.PlayerMoney)
            betAmount += 10;
        UpdateBetAmountText();
    }

    public void DecreaseBet()
    {
        if (betAmount - 10 >= minBet)
            betAmount -= 10;
        UpdateBetAmountText();
    }

    public void StartGame()
    {
        if (betAmount > 0 && betAmount <= casino.PlayerMoney)
        {
            resultText.text = "Gdzie jest pi³eczka?";
            OnGameStarted?.Invoke();
            OnCupButtonsDisable?.Invoke();
            StartCoroutine(ShowBallAnimationCoroutine());
        }
        else
        {
            resultText.text = "Zaklad nie mo¿e wynisiæ 0 z³!";
        }
    }

    private IEnumerator ShowBallAnimationCoroutine()
    {
        Button[] cups = new Button[] { cup1Button, cup2Button, cup3Button };
        int ballIndex = UnityEngine.Random.Range(0, cups.Length);
        Button selectedCup = cups[ballIndex];
        RectTransform rt = selectedCup.GetComponent<RectTransform>();

        float moveDistance = 60f;
        float duration = 0.25f;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 upPos = startPos + new Vector2(0, moveDistance);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rt.anchoredPosition = Vector2.Lerp(startPos, upPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = upPos;

        ballImage.rectTransform.anchoredPosition = startPos - new Vector2(0, 50);
        ballImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            rt.anchoredPosition = Vector2.Lerp(upPos, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = startPos;
        ballImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        StartCoroutine(ShuffleCupsCoroutine());
    }


    private IEnumerator ShuffleCupsCoroutine()
    {
        OnCupButtonsDisable?.Invoke();

        RectTransform rt1 = cup1Button.GetComponent<RectTransform>();
        RectTransform rt2 = cup2Button.GetComponent<RectTransform>();
        RectTransform rt3 = cup3Button.GetComponent<RectTransform>();

        Vector2[] startPositions = new Vector2[]
        {
            rt1.anchoredPosition,
            rt2.anchoredPosition,
            rt3.anchoredPosition
        };

        Vector2[] positions = new Vector2[3];
        Array.Copy(startPositions, positions, 3);

        System.Random rnd = new System.Random();

        int shuffleCount = 12;
        float singleShuffleDuration = 0.075f; // im mniejsza liczba tym szybciej sie mieszaj¹ kubki

        for (int shuffle = 0; shuffle < shuffleCount; shuffle++)
        {
            // Losowo zamieñ pozycje X
            for (int i = positions.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var temp = positions[i];
                positions[i] = positions[j];
                positions[j] = temp;
            }

            float elapsed = 0f;
            Vector2 start1 = rt1.anchoredPosition;
            Vector2 start2 = rt2.anchoredPosition;
            Vector2 start3 = rt3.anchoredPosition;
            Vector2 end1 = positions[0];
            Vector2 end2 = positions[1];
            Vector2 end3 = positions[2];

            while (elapsed < singleShuffleDuration)
            {
                float t = elapsed / singleShuffleDuration;
                rt1.anchoredPosition = new Vector2(Mathf.Lerp(start1.x, end1.x, t), start1.y);
                rt2.anchoredPosition = new Vector2(Mathf.Lerp(start2.x, end2.x, t), start2.y);
                rt3.anchoredPosition = new Vector2(Mathf.Lerp(start3.x, end3.x, t), start3.y);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt1.anchoredPosition = end1;
            rt2.anchoredPosition = end2;
            rt3.anchoredPosition = end3;
        }

        OnCupButtonsEnable?.Invoke();
        canChooseCup = true;
        resultText.text = "Wybierz kubek!";
    }

    private void SetCupButtonsInteractable(bool interactable)
    {
        cup1Button.interactable = interactable;
        cup2Button.interactable = interactable;
        cup3Button.interactable = interactable;
    }

    public void PlayThreeCups(int bet, int chosenCup)
    {
        if (bet > casino.PlayerMoney || bet <= 0 || chosenCup < 1 || chosenCup > 3)
        {
            resultText.text = "Nieprawid³owy zak³ad lub numer kubka (1-3).";
            return;
        }

        casino.PlayerMoney -= bet;
        int ballCup = UnityEngine.Random.Range(1, 4);

        if (chosenCup == ballCup)
        {
            int win = bet * 2;
            casino.PlayerMoney += win;
            resultText.text = $"Brawo! Pi³eczka by³a pod kubkiem {ballCup}. Wygrana: {win} z³. Stan konta: {casino.PlayerMoney} z³.";
        }
        else
        {
            resultText.text = $"Niestety, pi³eczka by³a pod kubkiem {ballCup}. Przegra³eœ! Stan konta: {casino.PlayerMoney} z³.";
        }
        UpdateMoney();
    }

    public void ChooseCup(int cupNumber)
    {
        if (!canChooseCup)
        {
            resultText.text = "Musisz wybraæ zak³ad i klikn¹æ Start Game!";
            return;
        }

        PlayThreeCups(betAmount, cupNumber);
        canChooseCup = false;
        OnGameEnded?.Invoke();
        OnCupButtonsDisable?.Invoke();
    }

    void UpdateMoney()
    {
        moneyText.text = $"Stan konta: {casino.PlayerMoney} z³";
    }

    void UpdateBetAmountText()
    {
        betAmountText.text = $"Zak³ad: {betAmount} z³";
    }

    void DisableBetAndPlayButtons()
    {
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
        playButton.interactable = false;
    }

    void EnableBetAndPlayButtons()
    {
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        playButton.interactable = true;
    }
}