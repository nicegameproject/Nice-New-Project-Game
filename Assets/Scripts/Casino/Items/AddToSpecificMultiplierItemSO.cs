using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To Specific Multiplier", fileName = "Item_AddToSpecificMultiplier")]
    public class AddToSpecificMultiplierItemSO : SlotItemSO
    {
        [Range(0, 6)]
        [SerializeField] private int targetIndex = 0;  
        [SerializeField] private int addValue = 10;     

        protected override void ApplyToMultipliers(int[] multipliers)
        {
            if (multipliers == null) return;
            if (targetIndex < 0 || targetIndex >= multipliers.Length) return;

            multipliers[targetIndex] += addValue;
        }
    }
}