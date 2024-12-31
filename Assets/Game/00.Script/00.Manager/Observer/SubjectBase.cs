using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script._00.Manager.Observer
{
    public abstract class SubjectBase: MonoBehaviour, ISubject
    {
        protected List<IObserver> _observers = new();
        
        public void Attach(IObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);   
            }
        }

        private void OnDisable()
        {
            var observersCopy = new List<IObserver>(_observers);
            
            foreach (var observer in observersCopy)
            {
                Detach(observer);
            }
            
        }

        /// <summary>
        /// Attach all observers
        /// Call in start after attack full
        /// </summary>
        public abstract void ObserversSetup();
        
        public void Detach(IObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }
        
        public virtual void Notify(object data, string flag)
        {
            foreach (var observer in _observers)
            {
                observer.OnNotified(data, flag);
            }
        }

        public void NotifySpecific(object data, string flag, IObserver observer)
        {
            observer.OnNotified(data, flag);
        }
    }
}