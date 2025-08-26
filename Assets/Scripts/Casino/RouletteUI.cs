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
        [SerializeField] private TextMeshProUGUI spinIntervalSecondsText;
        [SerializeField] private GameObject[] rouletteUiGameObjects;

        [Header("Audio")]
        [SerializeField] private RouletteSoundsManager sounds;

        [Header("Auto-Spin")]
        [SerializeField] private bool isPlayerReady = false;
        [SerializeField] private float spinIntervalSeconds = 30f;
        private Coroutine autoSpinRoutine;

        private readonly Dictionary<int, int> PlacedBets = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateBetsPanel();
            ResetButtonColors();

            RouletteUiGameObjects(false);

            if (spinIntervalSecondsText != null)
                spinIntervalSecondsText.text = Mathf.CeilToInt(spinIntervalSeconds).ToString();

            if (isPlayerReady && autoSpinRoutine == null)
                autoSpinRoutine = StartCoroutine(AutoSpinLoop());
        }

        private void OnDisable()
        {
            if (betButtons != null)
                foreach (var btn in betButtons) btn.onClick.RemoveAllListeners();

            isPlayerReady = false;
            if (autoSpinRoutine != null)
            {
                StopCoroutine(autoSpinRoutine);
                autoSpinRoutine = null;
            }
        }

        private void RouletteUiGameObjects(bool isActive)
        {
            foreach (var obj in rouletteUiGameObjects)
                obj.SetActive(isActive);
        }

        public override void PlayGame()
        {
            SetPlayerReady(true);
            RouletteUiGameObjects(true);

            if (spinIntervalSecondsText != null)
                spinIntervalSecondsText.text = Mathf.CeilToInt(spinIntervalSeconds).ToString();
        }

        private void SetPlayerReady(bool ready)
        {
            if (isPlayerReady == ready) return;

            isPlayerReady = ready;
            if (ready)
            {
                if (autoSpinRoutine == null)
                    autoSpinRoutine = StartCoroutine(AutoSpinLoop());
            }
        }

        private IEnumerator AutoSpinLoop()
        {
            if (spinIntervalSecondsText != null)
                spinIntervalSecondsText.text = Mathf.CeilToInt(spinIntervalSeconds).ToString();

            while (isPlayerReady)
            {
                float remaining = spinIntervalSeconds;
                while (isPlayerReady && !isGameInProgress && remaining > 0f)
                {
                    remaining -= Time.deltaTime;
                    int display = Mathf.Max(0, Mathf.CeilToInt(remaining));
                    if (spinIntervalSecondsText != null)
                        spinIntervalSecondsText.text = display.ToString();
                    yield return null;
                }

                if (!isPlayerReady) break;

                if (spinIntervalSecondsText != null)
                    spinIntervalSecondsText.text = "0";

                if (!isGameInProgress)
                {
                    isGameInProgress = true;
                    DisableGameControls();
                    yield return StartCoroutine(RandomSpin());
                }

                if (isPlayerReady && spinIntervalSecondsText != null)
                    spinIntervalSecondsText.text = Mathf.CeilToInt(spinIntervalSeconds).ToString();

                yield return null;
            }

            autoSpinRoutine = null;
        }

        private IEnumerator RandomSpin()
        {
            int randomResultNumber = Random.Range(0, 16);
            yield return StartCoroutine(rouletteWheel.Spin(randomResultNumber));

            bool anyBets = PlacedBets.Count > 0;
            bool isWin = anyBets && PlacedBets.ContainsKey(randomResultNumber);

            if (!anyBets)
                NoBetSpin(randomResultNumber);
            else if (isWin)
                Win(randomResultNumber);
            else
                Lose(randomResultNumber);

            HighlightOutcome(randomResultNumber, isWin, !anyBets);
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

            if (sounds != null) sounds.PlayWin();
        }

        private void Lose(int number)
        {
            resultText.text = $"The result was: {number} You lost!";

            if (sounds != null) sounds.PlayLose();
        }

        private void NoBetSpin(int number)
        {
            if (sounds != null) sounds.PlayLose();
            resultText.text = $"The result was: {number} No bets placed.";
        }

        private void HighlightOutcome(int number, bool isWin, bool noBet = false)
        {
            var img = betButtons[number].GetComponent<Image>();
            if (img == null) return;

            if (noBet)
                img.color = new Color(0.6f, 0.6f, 0.6f);
            else
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
                    resultText.text = "Insufficient funds!";
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
                betsPanelText.text += $"Number {bet.Key} → {bet.Value} PLN";
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