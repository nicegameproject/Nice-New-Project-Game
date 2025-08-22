using UnityEngine;

namespace Game.CasinoSystem
{
    [CreateAssetMenu(menuName = "Casino/Items/Force High-Multiplier Pattern (Uses)", fileName = "Item_ForceHighPattern")]
    public class ForceHighPatternItemSO : SlotItemSO
    {
        [SerializeField] private float minPatternMultiplier = 7.0f;
        public float MinPatternMultiplier => minPatternMultiplier;
    }
}