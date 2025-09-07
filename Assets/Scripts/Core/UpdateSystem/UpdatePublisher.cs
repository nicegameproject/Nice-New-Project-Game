using System;

public class UpdatePublisher : UpdatePublisherBase<IUpdateObserver>
{
    private void Update()
    {
        for (_currentIndex = _observers.Count - 1; _currentIndex >= 0; _currentIndex--)
        {
            _observers[_currentIndex].ObservedUpdate();
        }
        _observers.AddRange(_pendingObservers);
        _pendingObservers.Clear();
    }
}