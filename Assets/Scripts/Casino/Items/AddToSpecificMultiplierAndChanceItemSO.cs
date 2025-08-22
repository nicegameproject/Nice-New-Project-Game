using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To Specific Multiplier & Chance", fileName = "Item_AddToSpecificMultiplierAndChance")]
    public class AddToSpecificMultiplierAndChanceItemSO : SlotItemSO
    {
        [Range(0, 6)]
        [SerializeField] private int targetIndex = 0;

        [Header("Values to add")]
        [SerializeField] private int addMultiplierValue = 10;
        [SerializeField] private float addChanceValue = 10f;

        protected override void ApplyToMultipliers(int[] multipliers)
        {
            if (multipliers == null) return;
            multipliers[targetIndex] += addMultiplierValue;
        }

        protected override void ApplyToChances(float[] chances)
        {
            if (chances == null) return;
            chances[targetIndex] += addChanceValue; 
        }
    }
}