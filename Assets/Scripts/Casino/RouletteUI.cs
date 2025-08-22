using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CasinoSystem
{
    public class RouletteUI : Casino
    {
        [Header("Roulette Specific")]
        [SerializeField] private RouletteWheel rouletteWheel;
        [SerializeField] private Button[] betButtons;
        [SerializeField] private TextMeshProUGUI betsPanelText;

        private readonly Dictionary<int, int> PlacedBets = new();

        protected override void OnEnable()
        {
            base.OnEnable(); 
            UpdateBetsPanel();
            ResetButtonColors();
        }

        private void OnDisable()
        {
            if (betButtons != null)
                foreach (var btn in betButtons) btn.onClick.RemoveAllListeners();
        }

        public override void PlayGame()
        {
            if (isGameInProgress) return;

            if (PlacedBets.Count == 0)
            {
                resultText.text = "Pick the numbers or number first!";
                return;
            }

            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(RandomSpin());
        }

        private IEnumerator RandomSpin()
        {
            int randomResultNumber = Random.Range(0, 16);
            yield return StartCoroutine(rouletteWheel.Spin(randomResultNumber));

            bool isWin = PlacedBets.ContainsKey(randomResultNumber);

            if (isWin)
                Win(randomResultNumber);
            else
                Lose(randomResultNumber);

            HighlightOutcome(randomResultNumber, isWin);
            UpdateMoneyText();

            yield return new WaitForSeconds(1f);

            PlacedBets.Clear();
            UpdateBetsPanel();
            ResetButtonColors();
            EnableGameControls();
            isGameInProgress = false;
        }

        private void Win(int number)
        {
            int win = (number == 0)
                ? PlacedBets[number] * 35
                : PlacedBets[number] * 2;

            PlayerMoney += win;
            resultText.text = $"The result was: {number} You won {win} PLN!";
        }

        private void Lose(int number)
        {
            resultText.text = $"The result was: {number} You lost!";
        }

        private void HighlightOutcome(int number, bool isWin)
        {
            var img = betButtons[number].GetComponent<Image>();
            if (img == null) return;
            img.color = isWin ? new Color(1f, 0.84f, 0f) : Color.red;
        }

        public void ToggleBet(int number)
        {
            if (PlacedBets.ContainsKey(number))
            {
                PlayerMoney += PlacedBets[number];
                PlacedBets.Remove(number);
                betButtons[number].GetComponent<Image>().color = Color.white;
                resultText.text = $"Bet on number {number} removed";
            }
            else
            {
                if (PlayerMoney >= betAmount)
                {
                    if (betAmount == 0)
                    {
                        resultText.text = "You cannot bet 0 PLN!";
                        return;
                    }
                    PlayerMoney -= betAmount;
                    PlacedBets[number] = betAmount;
                    betButtons[number].GetComponent<Image>().color = Color.green;
                    resultText.text = $"A bet of {betAmount} PLN has been added to the number {number}";
                }
                else
                {
                    resultText.text = "\r\nInsufficient funds!";
                    return;
                }
            }
            UpdateMoneyText();
            UpdateBetsPanel();
        }

        private void UpdateBetsPanel()
        {
            if (PlacedBets.Count == 0)
            {
                betsPanelText.text = "No bets";
                return;
            }
            betsPanelText.text = "Bets:\n";
            foreach (var bet in PlacedBets.OrderByDescending(b => b.Value))
                betsPanelText.text += $"Number {bet.Key} → {bet.Value} PLN\n";
        }

        private void ResetButtonColors()
        {
            foreach (var btn in betButtons)
                btn.GetComponent<Image>().color = Color.white;
        }

        protected override void DisableGameControls()
        {
            base.DisableGameControls();
            foreach (var btn in betButtons) btn.interactable = false;
        }

        protected override void EnableGameControls()
        {
            base.EnableGameControls();
            foreach (var btn in betButtons) btn.interactable = true;
        }
    }
}