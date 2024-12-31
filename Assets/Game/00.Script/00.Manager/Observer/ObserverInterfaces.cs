namespace Game._00.Script._00.Manager.Observer
{
    public interface IObserver
    {
        /// <summary>
        /// object == null stop next chain, object != null => input for next chain
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        void OnNotified(object data, string flag); // Handle any type of data
    }

    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        void Notify(object data, string flag); // Notify with any data
    
        void NotifySpecific(object data, string flag, IObserver observer);
    }
}