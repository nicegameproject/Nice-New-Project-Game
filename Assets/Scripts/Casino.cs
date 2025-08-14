using UnityEngine;

public class Casino : MonoBehaviour
{
    public int playerMoney = 100;

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

    public void PlayThreeCups(int bet, int chosenCup)
    {
        if (bet > playerMoney || bet <= 0 || chosenCup < 1 || chosenCup > 3)
        {
            Debug.Log("Nieprawidłowy zakład lub numer kubka (1-3).");
            return;
        }

        playerMoney -= bet;
        int ballCup = Random.Range(1, 4); // losuje kubek z piłeczką (1-3)

        if (chosenCup == ballCup)
        {
            int win = bet * 2;
            playerMoney += win;
            Debug.Log($"Brawo! Piłeczka była pod kubkiem {ballCup}. Wygrana: {win} zł. Stan konta: {playerMoney} zł.");
        }
        else
        {
            Debug.Log($"Niestety, piłeczka była pod kubkiem {ballCup}. Przegrałeś! Stan konta: {playerMoney} zł.");
        }
    }
}