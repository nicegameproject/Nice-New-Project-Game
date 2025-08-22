using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To Multipliers & Chances", fileName = "Item_AddToAll_MultsAndChances")]
    public class AddToAllMultipliersAndChancesItemSO : SlotItemSO
    {
        [SerializeField] private int addMultiplier = 10;
        [SerializeField] private float addChance = 10f;

        protected override void ApplyToMultipliers(int[] multipliers)
        {
            if (multipliers == null) return;
            for (int i = 0; i < multipliers.Length; i++)
                multipliers[i] += addMultiplier;
        }

        protected override void ApplyToChances(float[] chances)
        {
            if (chances == null) return;
            for (int i = 0; i < chances.Length; i++)
                chances[i] += addChance;
        }
    }
}