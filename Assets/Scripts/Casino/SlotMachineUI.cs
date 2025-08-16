using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CasinoSystem
{
    public class SlotMachineUI : Casino
    {
        [Header("Slot Machine Specific")]
        [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[9];
        [SerializeField] private TextMeshProUGUI jackpotText;
        [SerializeField] private float spinDuration = 1.5f;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private int jackpotIncrement = 10;

        private int jackpot = 1000;
        private readonly string[] symbols = { "1", "2", "3", "4", "5" };
        private readonly int[] symbolMultipliers = { 1, 2, 3, 5, 10 };

        protected override void Start()
        {
            base.Start();
            autoSpinButton.onClick.AddListener(OnAutoSpinClicked);
            UpdateJackpotText();
        }

        public override void PlayGame()
        {
            if (betAmount > PlayerMoney)
            {
                resultText.text = "Nie masz wystarczająco pieniędzy!";
                return;
            }
            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(SpinSlots(betAmount, true));
        }

        public void OnAutoSpinClicked()
        {
            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(AutoSpin(10));
        }

        private IEnumerator AutoSpin(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (betAmount > PlayerMoney)
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
            isGameInProgress = false;
            EnableGameControls();
        }

        private IEnumerator SpinSlots(int bet, bool endSpinOnFinish)
        {
            string[,] finalBoard = new string[3, 3];
            foreach (var t in slotTexts) t.color = Color.white;

            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                for (int i = 0; i < slotTexts.Length; i++)
                    slotTexts[i].text = symbols[UnityEngine.Random.Range(0, symbols.Length)];
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    finalBoard[r, c] = symbols[UnityEngine.Random.Range(0, symbols.Length)];
                    slotTexts[r * 3 + c].text = finalBoard[r, c];
                }

            var winningIndices = new System.Collections.Generic.HashSet<int>();
            int totalWin = 0;
            int baseMultiplier = 10;

            for (int r = 0; r < 3; r++) { if (finalBoard[r, 0] == finalBoard[r, 1] && finalBoard[r, 1] == finalBoard[r, 2]) { totalWin += bet * baseMultiplier * symbolMultipliers[System.Array.IndexOf(symbols, finalBoard[r, 0])]; winningIndices.Add(r * 3); winningIndices.Add(r * 3 + 1); winningIndices.Add(r * 3 + 2); } }
            if (finalBoard[0, 0] == finalBoard[1, 1] && finalBoard[1, 1] == finalBoard[2, 2]) { totalWin += bet * baseMultiplier * symbolMultipliers[System.Array.IndexOf(symbols, finalBoard[0, 0])]; winningIndices.Add(0); winningIndices.Add(4); winningIndices.Add(8); }
            if (finalBoard[0, 2] == finalBoard[1, 1] && finalBoard[1, 1] == finalBoard[2, 0]) { totalWin += bet * baseMultiplier * symbolMultipliers[System.Array.IndexOf(symbols, finalBoard[0, 2])]; winningIndices.Add(2); winningIndices.Add(4); winningIndices.Add(6); }

            PlayerMoney -= bet;
            if (totalWin > 0)
            {
                resultText.text = $"Wygrałeś! Wygrana: {totalWin:N0} zł.";
                PlayerMoney += totalWin;
                foreach (int idx in winningIndices) slotTexts[idx].color = Color.red;
            }
            else
            {
                resultText.text = "Przegrałeś!";
            }

            jackpot += jackpotIncrement;
            UpdateMoneyText();
            UpdateJackpotText();

            // Jackpota Logic
            bool isJackpotWin = true; string firstSymbol = finalBoard[0, 0];
            for (int r = 0; r < 3; r++) { for (int c = 0; c < 3; c++) { if (finalBoard[r, c] != firstSymbol) { isJackpotWin = false; break; } } if (!isJackpotWin) break; }
            if (isJackpotWin && firstSymbol == "5") { PlayerMoney += jackpot; resultText.text = $"JACKPOT! Wygrywasz: {jackpot:N0} zł!"; jackpot = 1000; UpdateJackpotText(); }

            if (endSpinOnFinish)
            {
                isGameInProgress = false;
                EnableGameControls();
            }
        }

        private void UpdateJackpotText()
        {
            jackpotText.text = $"Jackpot: {jackpot:N0} zł";
        }

        protected override void DisableGameControls()
        {
            base.DisableGameControls();
            autoSpinButton.interactable = false;
        }

        protected override void EnableGameControls()
        {
            base.EnableGameControls();
            autoSpinButton.interactable = true;
        }
    }
}