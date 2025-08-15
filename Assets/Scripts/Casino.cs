using UnityEngine;

public class Casino : MonoBehaviour
{
    private int playerMoney = 1000;
    public int PlayerMoney { get => playerMoney; set => playerMoney = value; }

    public void PlayRoulette(int bet, int chosenNumber)
    {
        if (bet > playerMoney || bet <= 0 || chosenNumber < 0 || chosenNumber > 36)
        {
            Debug.Log("Nieprawidłowy zakład lub liczba.");
            return;
        }

        playerMoney -= bet;
        int result = Random.Range(0, 37); // 0-36

        if (result == chosenNumber)
        {
            int win = bet * 35;
            playerMoney += win;
            Debug.Log($"Wygrałeś! Wygrana: {win} zł. Stan konta: {playerMoney} zł.");
        }
        else
        {
            Debug.Log($"Przegrałeś! Wypadło: {result}. Stan konta: {playerMoney} zł.");
        }
    }

    public void PlayDice(int bet)
    {
        if (bet > playerMoney || bet <= 0)
        {
            Debug.Log("Nieprawidłowa kwota zakładu.");
            return;
        }

        playerMoney -= bet;
        int dice = Random.Range(1, 7); // 1-6

        if (dice == 6)
        {
            int win = bet * 5;
            playerMoney += win;
            Debug.Log($"Wygrałeś! Wyrzucono 6. Wygrana: {win} zł. Stan konta: {playerMoney} zł.");
        }
        else
        {
            Debug.Log($"Przegrałeś! Wyrzucono: {dice}. Stan konta: {playerMoney} zł.");
        }
    }
}