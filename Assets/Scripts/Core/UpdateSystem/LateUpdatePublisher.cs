public class LateUpdatePublisher : UpdatePublisherBase<ILateUpdateObserver>
{
    private void LateUpdate()
    {
        for (_currentIndex = _observers.Count - 1; _currentIndex >= 0; _currentIndex--)
        {
            _observers[_currentIndex].ObservedLateUpdate();
        }
        _observers.AddRange(_pendingObservers);
        _pendingObservers.Clear();
    }
}