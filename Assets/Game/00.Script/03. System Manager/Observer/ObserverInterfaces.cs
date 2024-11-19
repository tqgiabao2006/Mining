public interface IObserver
{
    void OnNotified(object data); // Handle any type of data
}

public interface ISubject
{
    void Attach(IObserver observer);
    void Detach(IObserver observer);
    void Notify(object data); // Notify with any data
}