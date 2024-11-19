namespace Game._00.Script._05._Manager
{
    public interface IObserver<T>
    {
        void OnNotified(T data);
    }

    public interface ISubject<T>
    {
        void Attach(IObserver<T> observer);    // Add an observer
        void Detach(IObserver<T> observer);    // Remove an observer
        void Notify(T data);                   // Notify all observers
    }
}