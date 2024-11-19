using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script._05._Manager
{
    public class SubjectBase<T>: ISubject<T>
    {
        private readonly List<IObserver<T>> _observers = new();

        public void Attach(IObserver<T> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);   
            }
        }

        public void Detach(IObserver<T> observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }

        public void Notify(T data)
        {
            foreach (var observer in _observers)
            {
                observer.OnNotified(data);
            }
        }
    }
}