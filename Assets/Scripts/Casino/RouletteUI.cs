using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    private int betAmount = 0;
    private int minBet = 1;
    private int maxBet = 100;
    private int chosenNumber = -1;

    private void Start()
    {
        resultText.text = "";

        UpdateMoney();
        UpdateBetAmountText();
        EnableBetAndPlayButtons();

        // przypisanie akcji do przycisków numerów
        for (int i = 0; i < betButtons.Length; i++)
        {
            int number = i;
            betButtons[i].onClick.AddListener(() => SelectNumber(number));
        }

        playButton.onClick.AddListener(PlayGame);
    }

    public void PlayGame()
    {
        if (chosenNumber == -1)
        {
            resultText.text = "Najpierw wybierz numer!";
            return;
        }

        if (betAmount > casino.PlayerMoney || betAmount <= 0)
        {
            resultText.text = "Nieprawidłowy zakład.";
            return;
        }

        casino.PlayerMoney -= betAmount;
        UpdateMoney();

        DisableBetAndPlayButtons();

        StartCoroutine(RandomSpin());
    }


    private IEnumerator RandomSpin()
    {
        int randomResultNumber = Random.Range(0, 16);
        yield return StartCoroutine(rouletteWheel.Spin(randomResultNumber));

        if (randomResultNumber == chosenNumber)
        {
            int win = (randomResultNumber == 0) ? betAmount * 14 : betAmount * 2;
            casino.PlayerMoney += win;
            resultText.text = $"Wypadło: {randomResultNumber} Wygrałeś {win} zł!";
        }
        else
        {
            resultText.text = $"Wypadło: {randomResultNumber} Przegrałeś!";
        }

        UpdateMoney();
        chosenNumber = -1;
        EnableBetAndPlayButtons();
    }



    /*   private IEnumerator RandomSpin()
       {
           yield return StartCoroutine(rouletteWheel.Spin(2));


   *//*        if (result == chosenNumber)
           {
               int win = (result == 0) ? betAmount * 14 : betAmount * 2;
               casino.PlayerMoney += win;
               resultText.text = $"Wypadło: {result} Wygrałeś {win} zł!";
           }
           else
           {
               resultText.text = $"Wypadło: {result} Przegrałeś!";
           }*//*

           UpdateMoney();
           chosenNumber = -1;
           EnableBetAndPlayButtons();
       }*/

    void SelectNumber(int number)
    {
        chosenNumber = number;
        resultText.text = $"Wybrano numer: {chosenNumber}";
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
}
