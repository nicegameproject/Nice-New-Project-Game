using UnityEngine;

namespace Game.CasinoSystem
{
    public abstract class SlotItemSO : ScriptableObject
    {
        [SerializeField] private string itemName = "Item Name";
        [SerializeField] private int usesRemaining = 5;

        public string ItemName => itemName;
        public int UsesRemaining => usesRemaining;
        public bool CanUse => usesRemaining > 0;

        public void TryApply(int[] multipliers, float[] chances, bool consumeUse)
        {
            if (!CanUse) return;

            if (multipliers != null) ApplyToMultipliers(multipliers);
            if (chances != null) ApplyToChances(chances);

            if (consumeUse)
            {
                usesRemaining = Mathf.Max(0, usesRemaining - 1);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        protected virtual void ApplyToMultipliers(int[] multipliers) { }
        protected virtual void ApplyToChances(float[] chances) { }


    }
}