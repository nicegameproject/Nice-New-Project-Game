using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCharacter : MonoBehaviour
{
    public Transform TransformRef { get; private set; }

    public Vector3 Position => TransformRef.position;

    void Awake()
    {
        TransformRef = transform;
    }

    void OnEnable()
    {
        if (PlayerRegistry.Instance != null)
            PlayerRegistry.Instance.Register(this);
    }

    void OnDisable()
    {
        if (PlayerRegistry.Instance != null)
            PlayerRegistry.Instance.Unregister(this);
    }
}