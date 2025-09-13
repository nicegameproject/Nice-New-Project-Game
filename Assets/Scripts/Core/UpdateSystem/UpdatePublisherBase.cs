using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-4)]
public abstract class UpdatePublisherBase<TObserver> : MonoBehaviour 
{
    protected static List<TObserver> _observers = new List<TObserver>();
    protected static List<TObserver> _pendingObservers = new List<TObserver>();
    protected static int _currentIndex;
    
    public static void RegisterObserver(TObserver observer)
    {
        _pendingObservers.Add(observer);
    }

    public static void UnregisterObserver(TObserver observer)
    {
        _pendingObservers.Remove(observer);
        _currentIndex--;
    }
}