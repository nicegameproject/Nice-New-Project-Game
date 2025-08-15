using UnityEngine;

public class Casino : MonoBehaviour
{
    private int playerMoney = 1000;
    public int PlayerMoney { get => playerMoney; set => playerMoney = value; }



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