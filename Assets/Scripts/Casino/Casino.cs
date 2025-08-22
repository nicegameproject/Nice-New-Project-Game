using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CasinoSystem
{
    public abstract class Casino : MonoBehaviour
    {
        private int playerMoney = 10000;
        public int PlayerMoney { get => playerMoney; set => playerMoney = value; }

        [Header("Base UI Components")]
        [SerializeField] protected TextMeshProUGUI resultText;
        [SerializeField] protected TextMeshProUGUI moneyText;
        [SerializeField] protected TextMeshProUGUI betAmountText;
        [SerializeField] protected Button increaseBetButton;
        [SerializeField] protected Button decreaseBetButton;
        [SerializeField] protected Button playButton;

        [Header("Bet Settings")]
        [SerializeField] protected int minBet = 1;
        [SerializeField] protected int maxBet = 100;
        [SerializeField] protected int betStep = 10;

        protected int betAmount = 0;
        protected bool isGameInProgress = false;

        protected virtual void Start()
        {
            UpdateBetAmountText();
            UpdateMoneyText();
            resultText.text = "";
        }

        protected virtual void OnEnable()
        {
            resultText.text = "";
            UpdateMoneyText();
            UpdateBetAmountText();
            EnableGameControls();
        }

        public abstract void PlayGame();

        public virtual void IncreaseBet()
        {
            if (betAmount + betStep <= maxBet && betAmount + betStep <= PlayerMoney)
            {
                betAmount += betStep;
            }
            UpdateBetAmountText();
        }

        public virtual void DecreaseBet()
        {
            if (betAmount - betStep >= minBet)
            {
                betAmount -= betStep;
            }
            UpdateBetAmountText();
        }

        protected void UpdateMoneyText()
        {
            moneyText.text = $"Account status: {PlayerMoney:N0} zł";
        }

        protected void UpdateBetAmountText()
        {
            betAmountText.text = $"Bet: {betAmount} zł";
        }

        protected virtual void DisableGameControls()
        {
            increaseBetButton.interactable = false;
            decreaseBetButton.interactable = false;
            if (playButton != null) playButton.interactable = false;
        }

        protected virtual void EnableGameControls()
        {
            increaseBetButton.interactable = true;
            decreaseBetButton.interactable = true;
            if (playButton != null) playButton.interactable = true;
        }
    }
}
