using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To All Multipliers", fileName = "Item_AddToAllMultipliers")]
    public class AddToAllMultipliersItemSO : SlotItemSO
    {
        [SerializeField] private int addValue = 10;

        protected override void ApplyToMultipliers(int[] multipliers)
        {
            for (int i = 0; i < multipliers.Length; i++)
                multipliers[i] += addValue;
        }
    }
}