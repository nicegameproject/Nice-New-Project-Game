using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Add +X To All Chances", fileName = "Item_AddToAllChances")]
    public class AddToAllChancesItemSO : SlotItemSO
    {
        [SerializeField] private float addValue = 10f;

        protected override void ApplyToChances(float[] chances)
        {
            for (int i = 0; i < chances.Length; i++)
                chances[i] += addValue;
        }
    }
}