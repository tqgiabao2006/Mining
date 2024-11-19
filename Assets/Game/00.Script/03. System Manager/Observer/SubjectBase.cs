using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script._05._Manager
{
    public class SubjectBase: ISubject
    {
        private readonly List<IObserver> _observers = new();

        public void Attach(IObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);   
            }
        }

        public void Detach(IObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }

        public void Notify(object data)
        {
            foreach (var observer in _observers)
            {
                observer.OnNotified(data);
            }
        }
    }
}