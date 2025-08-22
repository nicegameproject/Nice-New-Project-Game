using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CasinoSystem
{
    public class ThreeCupsUI : Casino
    {
        [Header("Three Cups Specific")]
        [SerializeField] private Button cup1Button;
        [SerializeField] private Button cup2Button;
        [SerializeField] private Button cup3Button;
        [SerializeField] private Image ballImage;

        private bool canChooseCup = false;
        private int ballCup = 0;

        protected override void Start()
        {
            base.Start();
            if (ballImage != null) ballImage.gameObject.SetActive(false);
        }

        public override void PlayGame()
        {
            if (betAmount > 0 && betAmount <= PlayerMoney)
            {
                resultText.text = "Where is the ball?";
                isGameInProgress = true;
                DisableGameControls();
                StartCoroutine(ShowBallAnimationCoroutine());
            }
            else
            {
                resultText.text = "The bet cannot be 0 PLN!";
            }
        }

        private IEnumerator ShowBallAnimationCoroutine()
        {
            Button[] cups = { cup1Button, cup2Button, cup3Button };
            ballCup = UnityEngine.Random.Range(1, 4);
            Button selectedCup = cups[ballCup - 1];
            RectTransform rt = selectedCup.GetComponent<RectTransform>();
            float moveDistance = 60f, duration = 0.25f;
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

            if (ballImage != null)
            {
                ballImage.rectTransform.anchoredPosition = startPos - new Vector2(0, 50);
                ballImage.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(0.3f);

            elapsed = 0f;
            while (elapsed < duration)
            {
                rt.anchoredPosition = Vector2.Lerp(upPos, startPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = startPos;

            if (ballImage != null) ballImage.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            StartCoroutine(ShuffleCupsCoroutine());
        }

        private IEnumerator ShuffleCupsCoroutine()
        {
            RectTransform rt1 = cup1Button.GetComponent<RectTransform>(),
                          rt2 = cup2Button.GetComponent<RectTransform>(),
                          rt3 = cup3Button.GetComponent<RectTransform>();

            Vector2[] positions = { rt1.anchoredPosition, rt2.anchoredPosition, rt3.anchoredPosition };
            System.Random rnd = new System.Random();
            int shuffleCount = 12;
            float singleShuffleDuration = 0.075f;

            for (int shuffle = 0; shuffle < shuffleCount; shuffle++)
            {
                for (int i = positions.Length - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    var temp = positions[i];
                    positions[i] = positions[j];
                    positions[j] = temp;
                }

                float elapsed = 0f;
                Vector2 start1 = rt1.anchoredPosition, start2 = rt2.anchoredPosition, start3 = rt3.anchoredPosition;
                Vector2 end1 = positions[0], end2 = positions[1], end3 = positions[2];
                while (elapsed < singleShuffleDuration)
                {
                    float t = elapsed / singleShuffleDuration;
                    rt1.anchoredPosition = new Vector2(Mathf.Lerp(start1.x, end1.x, t), start1.y);
                    rt2.anchoredPosition = new Vector2(Mathf.Lerp(start2.x, end2.x, t), start2.y);
                    rt3.anchoredPosition = new Vector2(Mathf.Lerp(start3.x, end3.x, t), start3.y);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                rt1.anchoredPosition = end1; rt2.anchoredPosition = end2; rt3.anchoredPosition = end3;
            }

            SetCupButtonsInteractable(true);
            canChooseCup = true;
            resultText.text = "Choose a cup!";
        }

        public void ChooseCup(int cupNumber)
        {
            if (!canChooseCup)
            {
                resultText.text = "You need to select a bet and click Start Game!";
                return;
            }

            canChooseCup = false;
            SetCupButtonsInteractable(false);
            StartCoroutine(RevealAndResolveCoroutine(cupNumber));
        }

        private IEnumerator RevealAndResolveCoroutine(int cupNumber)
        {
            Button chosenButton = cupNumber == 1 ? cup1Button : (cupNumber == 2 ? cup2Button : cup3Button);
            RectTransform rt = chosenButton.GetComponent<RectTransform>();
            float moveDistance = 60f, duration = 0.25f;
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

            if (cupNumber == ballCup && ballImage != null)
            {
                ballImage.rectTransform.anchoredPosition = startPos - new Vector2(0, 50);
                ballImage.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(1f);

            elapsed = 0f;
            while (elapsed < duration)
            {
                rt.anchoredPosition = Vector2.Lerp(upPos, startPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = startPos;

            if (ballImage != null) ballImage.gameObject.SetActive(false);

            PlayThreeCups(betAmount, cupNumber);

            isGameInProgress = false;
            EnableGameControls();
        }

        private void PlayThreeCups(int bet, int chosenCup)
        {
            if (bet > PlayerMoney || bet <= 0 || chosenCup < 1 || chosenCup > 3)
            {
                resultText.text = "Invalid bet or cup number (1-3).";
                return;
            }

            PlayerMoney -= bet;

            if (chosenCup == ballCup)
            {
                int win = bet * 2;
                PlayerMoney += win;
                resultText.text = $"Bravo! The ball was under the {ballCup}. Winnings: {win} PLN.";
            }
            else
            {
                resultText.text = $"Unfortunately, the ball was under the {ballCup}. You lost!";
            }
            UpdateMoneyText();
        }

        private void SetCupButtonsInteractable(bool interactable)
        {
            if (cup1Button != null) cup1Button.interactable = interactable;
            if (cup2Button != null) cup2Button.interactable = interactable;
            if (cup3Button != null) cup3Button.interactable = interactable;
        }

        protected override void DisableGameControls()
        {
            base.DisableGameControls();
            SetCupButtonsInteractable(false);
        }

        protected override void EnableGameControls()
        {
            base.EnableGameControls();
            SetCupButtonsInteractable(true);
        }
    }
}
