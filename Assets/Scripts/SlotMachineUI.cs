using UnityEngine;
using TMPro;
using System.Collections;

public class SlotMachineUI : MonoBehaviour
{
    public Casino casino;
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[9];
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI betAmountText;
    public float spinDuration = 1.5f;

    public int betAmount = 10;
    public int minBet = 1;
    public int maxBet = 100;

    string[] symbols = { "1", "2", "3", "4", "5" };
    int[] symbolMultipliers = { 1, 2, 3, 5, 10 };

    void Start()
    {
        UpdateBetAmountText();
        UpdateMoneyText();
    }

    public void IncreaseBet()
    {
        if (betAmount + 10 <= maxBet && betAmount + 10 <= casino.playerMoney)
            betAmount += 10;
        UpdateBetAmountText();
    }

    public void DecreaseBet()
    {
        if (betAmount - 10 >= minBet)
            betAmount -= 10;
        UpdateBetAmountText();
    }

    public void OnPlaySlotClicked()
    {
        if (betAmount > casino.playerMoney)
        {
            resultText.text = "Nie masz wystarczająco pieniędzy!";
            return;
        }
        StartCoroutine(SpinSlots(betAmount));
    }

    public void OnAutoSpinClicked()
    {
        StartCoroutine(AutoSpin(10));
    }

    IEnumerator AutoSpin(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (betAmount > casino.playerMoney)
            {
                resultText.text = "Brak środków na dalsze spiny!";
                break;
            }
            yield return StartCoroutine(SpinSlots(betAmount));
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator SpinSlots(int bet)
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
                    string symbol = symbols[Random.Range(0, symbols.Length)];
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
                finalBoard[r, c] = symbols[Random.Range(0, symbols.Length)];
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

        casino.playerMoney -= bet;
        if (totalWin > 0)
        {
            resultText.text = $"Wygrałeś! Wygrana: {totalWin} zł.";
            casino.playerMoney += totalWin;
            foreach (int idx in winningIndices)
                slotTexts[idx].color = Color.red;
        }
        else
        {
            resultText.text = "Przegrałeś!";
        }
        UpdateMoneyText();
    }

    void UpdateBetAmountText()
    {
        betAmountText.text = $"Zakład: {betAmount} zł";
    }

    void UpdateMoneyText()
    {
        moneyText.text = $"Stan konta: {casino.playerMoney} zł";
    }
}