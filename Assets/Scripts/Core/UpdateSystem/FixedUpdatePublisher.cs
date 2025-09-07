public class FixedUpdatePublisher : UpdatePublisherBase<IFixedUpdateObserver>
{
    private void FixedUpdate()
    {
        for (_currentIndex = _observers.Count - 1; _currentIndex >= 0; _currentIndex--)
        {
            _observers[_currentIndex].ObservedFixedUpdate();
        }
        _observers.AddRange(_pendingObservers);
        _pendingObservers.Clear();
    }
}