using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RouletteUI : MonoBehaviour
{
    [SerializeField] private Casino casino;
    [SerializeField] private RouletteWheel rouletteWheel;

    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button[] betButtons;
    [SerializeField] private TextMeshProUGUI betAmountText;

    [SerializeField] private TextMeshProUGUI betsPanelText;


    private int betAmount = 0;
    private int minBet = 1;
    private int maxBet = 100;

    private List<int> chosenNumbers = new List<int>();
    private Dictionary<int, int> placedBets = new Dictionary<int, int>();

    private void Start()
    {
        resultText.text = "";

        UpdateMoney();
        UpdateBetAmountText();
        EnableBetAndPlayButtons();

        for (int i = 0; i < betButtons.Length; i++)
        {
            int number = i;
            betButtons[i].onClick.AddListener(() => ToggleBet(number));
        }

        playButton.onClick.AddListener(PlayGame);
    }

    public void PlayGame()
    {
        if (placedBets.Count == 0)
        {
            resultText.text = "Najpierw wybierz numer(y)!";
            return;
        }

        DisableBetAndPlayButtons();
        StartCoroutine(RandomSpin());
    }

    private IEnumerator RandomSpin()
    {
        int randomResultNumber = Random.Range(0, 16);
        yield return StartCoroutine(rouletteWheel.Spin(randomResultNumber));

        if (placedBets.ContainsKey(randomResultNumber))
        {
            int win = (randomResultNumber == 0)
                ? placedBets[randomResultNumber] * 14
                : placedBets[randomResultNumber] * 2;

            casino.PlayerMoney += win;
            resultText.text = $"Wypadło: {randomResultNumber} Wygrałeś {win} zł!";
        }
        else
        {
            resultText.text = $"Wypadło: {randomResultNumber} Przegrałeś!";
        }

        UpdateMoney();
        chosenNumbers.Clear();
        betsPanelText.text = "";
        //placedBets.Clear();
        ResetButtonColors();
        EnableBetAndPlayButtons();


    }

    void ToggleBet(int number)
    {
        if (placedBets.ContainsKey(number))
        {
            casino.PlayerMoney += placedBets[number];
            placedBets.Remove(number);
            betButtons[number].GetComponent<Image>().color = Color.white;
            resultText.text = $"Usunięto zakład na numer {number}";
        }
        else
        {
            if (casino.PlayerMoney >= betAmount)
            {
                casino.PlayerMoney -= betAmount;
                placedBets[number] = betAmount;
                betButtons[number].GetComponent<Image>().color = Color.green;
                resultText.text = $"Dodano zakład {betAmount} zł na numer {number}";
            }
            else
            {
                resultText.text = "Brak wystarczających środków!";
                return;
            }
        }

        UpdateMoney();
        UpdateBetsPanel();
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

    void UpdateBetsPanel()
    {
        if (placedBets.Count == 0)
        {
            betsPanelText.text = "Brak obstawień";
            return;
        }

        betsPanelText.text = "Obstawienia:\n";

        foreach (var bet in placedBets.OrderByDescending(b => b.Value))
        {
            betsPanelText.text += $"Numer {bet.Key} → {bet.Value} zł\n";
        }
    }


    void UpdateBetAmountText()
    {
        betAmountText.text = $"Zakład: {betAmount} zł";
    }

    void UpdateMoney()
    {
        moneyText.text = $"Stan konta: {casino.PlayerMoney} zł";
    }

    void DisableBetAndPlayButtons()
    {
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
        playButton.interactable = false;
        foreach (var btn in betButtons) btn.interactable = false;
    }

    void EnableBetAndPlayButtons()
    {
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        playButton.interactable = true;
        foreach (var btn in betButtons) btn.interactable = true;
    }

    void ResetButtonColors()
    {
        foreach (var btn in betButtons)
            btn.GetComponent<Image>().color = Color.white;
    }
}
