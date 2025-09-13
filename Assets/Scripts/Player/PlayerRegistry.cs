using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    private readonly List<PlayerCharacter> _players = new List<PlayerCharacter>(4);
    public IReadOnlyList<PlayerCharacter> Players => _players;

    [SerializeField, Tooltip("Liczba graczy (podgl¹d w Inspectorze)")]
    private int _playersCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _playersCount = _players.Count;
    }

    internal void Register(PlayerCharacter player)
    {
        if (player != null && !_players.Contains(player))
        {
            _players.Add(player);
            _playersCount = _players.Count;
        }
    }

    internal void Unregister(PlayerCharacter player)
    {
        if (player != null)
        {
            if (_players.Remove(player))
                _playersCount = _players.Count;
        }
    }

    public Transform GetPlayerTransform()
    {
        for (int i = 0; i < _players.Count; i++)
        {
            var pc = _players[i];
            if (pc != null && pc.TransformRef != null)
                return pc.TransformRef;
        }
        return null;
    }

}