using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To Specific Chance", fileName = "Item_AddToSpecificChance")]
    public class AddToSpecificChanceItemSO : SlotItemSO
    {
        [Range(0, 6)]
        [SerializeField] private int targetIndex = 0;
        [SerializeField] private float addValue = 10f;

        protected override void ApplyToChances(float[] chances)
        {
            if (chances == null) return;
            if (targetIndex < 0 || targetIndex >= chances.Length) return;

            chances[targetIndex] += addValue;
        }
    }
}