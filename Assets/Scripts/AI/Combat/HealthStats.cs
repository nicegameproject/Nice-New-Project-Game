using UnityEngine;

[CreateAssetMenu(menuName = "AI/Stats/Health Stats", fileName = "HealthStats")]
public class HealthStats : ScriptableObject
{
    [Min(1f)] public float BaseMaxHealth = 100f;

    [HideInInspector] public float MaxHealth;
    [HideInInspector] public float CurrentHealth;

    public void InitializeRuntime()
    {
        MaxHealth = Mathf.Max(1f, BaseMaxHealth);
        CurrentHealth = MaxHealth;
    }

    public float HealthPercent => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;
}