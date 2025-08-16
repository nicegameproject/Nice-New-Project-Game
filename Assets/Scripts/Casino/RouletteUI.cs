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
                resultText.text = "Najpierw wybierz numer(y)!";
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
            resultText.text = $"Wypadło: {number} Wygrałeś {win} zł!";
        }

        private void Lose(int number)
        {
            resultText.text = $"Wypadło: {number} Przegrałeś!";
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
                resultText.text = $"Usunięto zakład na numer {number}";
            }
            else
            {
                if (PlayerMoney >= betAmount)
                {
                    if (betAmount == 0)
                    {
                        resultText.text = "Nie możesz obstawić 0 zł!";
                        return;
                    }
                    PlayerMoney -= betAmount;
                    PlacedBets[number] = betAmount;
                    betButtons[number].GetComponent<Image>().color = Color.green;
                    resultText.text = $"Dodano zakład {betAmount} zł na numer {number}";
                }
                else
                {
                    resultText.text = "Brak wystarczających środków!";
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
                betsPanelText.text = "Brak obstawień";
                return;
            }
            betsPanelText.text = "Obstawienia:\n";
            foreach (var bet in PlacedBets.OrderByDescending(b => b.Value))
                betsPanelText.text += $"Numer {bet.Key} → {bet.Value} zł\n";
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