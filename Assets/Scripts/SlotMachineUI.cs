using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class SlotMachineUI : MonoBehaviour
{
    [SerializeField] private Casino casino;
    [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[9];
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI betAmountText;
    [SerializeField] private TextMeshProUGUI jackpotText;
    [SerializeField] private float spinDuration = 1.5f;

    [SerializeField] private int betAmount = 10;
    [SerializeField] private int minBet = 1;
    [SerializeField] private int maxBet = 100;

    private int jackpot = 0; 
    private int jackpotIncrement = 10; 

    string[] symbols = { "1", "2", "3", "4", "5" };
    int[] symbolMultipliers = { 1, 2, 3, 5, 10 };

    [SerializeField] private Button spinButton;
    [SerializeField] private Button autoSpinButton;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;

    public event Action OnSpinStarted;
    public event Action OnSpinEnded;

    void Start()
    {
        UpdateBetAmountText();
        UpdateMoneyText();
        UpdateJackpotText();
        OnSpinStarted += DisableSpinButtons;
        OnSpinEnded += EnableSpinButtons;
    }

    void DisableSpinButtons()
    {
        spinButton.interactable = false;
        autoSpinButton.interactable = false;    
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
    }

    void EnableSpinButtons()
    {
        spinButton.interactable = true;
        autoSpinButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
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

    public void OnSpinClicked()
    {
        if (betAmount > casino.PlayerMoney)
        {
            resultText.text = "Nie masz wystarczająco pieniędzy!";
            return;
        }
        OnSpinStarted?.Invoke();
        StartCoroutine(SpinSlots(betAmount, true));
    }

    public void OnAutoSpinClicked()
    {
        OnSpinStarted?.Invoke();
        StartCoroutine(AutoSpin(10));
    }

    IEnumerator AutoSpin(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (betAmount > casino.PlayerMoney)
            {
                resultText.text = "Brak środków na dalsze spiny!";
                break;
            }
            yield return StartCoroutine(SpinSlots(betAmount, false));

            if (resultText.text.StartsWith("Wygrałeś!"))
                yield return new WaitForSeconds(2f);
            else
                yield return new WaitForSeconds(0.5f);
        }
        OnSpinEnded?.Invoke();
    }

    IEnumerator SpinSlots(int bet, bool endSpinOnFinish)
    {
        int rows = 3, cols = 3;
        string[,] finalBoard = new string[rows, cols];

        foreach (var t in slotTexts)
            t.color = Color.white;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    string symbol = symbols[UnityEngine.Random.Range(0, symbols.Length)];
                    slotTexts[r * cols + c].text = symbol;
                }
            }
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                finalBoard[r, c] = symbols[UnityEngine.Random.Range(0, symbols.Length)];
                slotTexts[r * cols + c].text = finalBoard[r, c];
            }
        }

        var winningIndices = new System.Collections.Generic.HashSet<int>();
        int totalWin = 0;
        int baseMultiplier = 10;

        // Poziome linie
        for (int r = 0; r < rows; r++)
        {
            if (finalBoard[r, 0] == finalBoard[r, 1] && finalBoard[r, 1] == finalBoard[r, 2])
            {
                string winningSymbol = finalBoard[r, 0];
                int symbolIndex = System.Array.IndexOf(symbols, winningSymbol);
                int symbolMultiplier = symbolMultipliers[symbolIndex];
                totalWin += bet * baseMultiplier * symbolMultiplier;

                winningIndices.Add(r * cols + 0);
                winningIndices.Add(r * cols + 1);
                winningIndices.Add(r * cols + 2);
            }
        }
        // Przekątna główna
        if (finalBoard[0, 0] == finalBoard[1, 1] && finalBoard[1, 1] == finalBoard[2, 2])
        {
            string winningSymbol = finalBoard[0, 0];
            int symbolIndex = System.Array.IndexOf(symbols, winningSymbol);             
            int symbolMultiplier = symbolMultipliers[symbolIndex];
            totalWin += bet * baseMultiplier * symbolMultiplier;

            winningIndices.Add(0);
            winningIndices.Add(4);
            winningIndices.Add(8);
        }
        // Przekątna boczna
        if (finalBoard[0, 2] == finalBoard[1, 1] && finalBoard[1, 1] == finalBoard[2, 0])
        {
            string winningSymbol = finalBoard[0, 2];
            int symbolIndex = System.Array.IndexOf(symbols, winningSymbol);
            int symbolMultiplier = symbolMultipliers[symbolIndex];
            totalWin += bet * baseMultiplier * symbolMultiplier;

            winningIndices.Add(2);
            winningIndices.Add(4);
            winningIndices.Add(6);
        }

        casino.PlayerMoney -= bet;
        if (totalWin > 0)
        {
            resultText.text = $"Wygrałeś! Wygrana: {totalWin:N0} zł.";
            casino.PlayerMoney += totalWin;
            foreach (int idx in winningIndices)
                slotTexts[idx].color = Color.red;
        }
        else
        {
            resultText.text = "Przegrałeś!";
        }
            
        UpdateMoneyText();
        UpdateJackpotText();
        jackpot += jackpotIncrement;
        UpdateJackpotText();

        bool isJackpotWin = true;
        string firstSymbol = finalBoard[0, 0];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (finalBoard[r, c] != firstSymbol)
                {
                    isJackpotWin = false;
                    break;
                }
            }
            if (!isJackpotWin) break;
        }
        if (isJackpotWin && firstSymbol == "5")
        {
            casino.PlayerMoney += jackpot;
            resultText.text = $"JACKPOT! Wygrywasz: {jackpot:N0} zł!";
            jackpot = 1000;
            UpdateJackpotText();
        }
        if (endSpinOnFinish)
            OnSpinEnded?.Invoke();
    }

    void UpdateBetAmountText()
    {
        betAmountText.text = $"Zakład: {betAmount} zł";
    }

    void UpdateMoneyText()
    {
        moneyText.text = $"Stan konta: {(int)casino.PlayerMoney:N0} zł";
    }

    void UpdateJackpotText()
    {
        jackpotText.text = $"Jackpot: {jackpot:N0} zł";
    }
}