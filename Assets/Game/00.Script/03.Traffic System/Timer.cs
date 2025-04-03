using System;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.Building;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using URandom = UnityEngine.Random;
namespace Game._00.Script._04.Timer
{
    public class Timer: SubjectBase
    {
        public enum WeekDay 
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday,
            Sunday
        }

        [Tooltip("Stop after 1 week to determine the week.")]
        [SerializeField] private bool stopTest = false;

        private bool _stopTest;
        
        [SerializeField] private bool isGizmos;
        
        [SerializeField] private float secPerDay;
        
        [ReadOnly] private WeekDay _day;
       
        [SerializeField] private float timeScale;
        
        private float _timeCounter;
        
        private BuildingSpawner _buildingSpawner;

        private int _week;

        private WeekDay _randomDay;

        private bool _hasSpawned; //Check has spawned this week
        
        public WeekDay Day
        {
            get
            {
                return _day;
            }
        }

        public int Week
        {
            get
            {
                return _week;
            }
        }

        public float TimeScale
        {
            get
            {
                return timeScale;
            }
            set
            {
                if (value >= 0)
                {
                    timeScale = value;
                }
            }
        }

        private void Update()
        {
            if (_stopTest)
            {
                return;
            }
            Tick();
        }

        private void Start()
        {
            ObserversSetup();
            _randomDay = PickRandomDay();
            _hasSpawned = false;
            _stopTest = false;
            _week = 1;
        }
        
        private void Tick()
        {
            _timeCounter += Time.deltaTime * timeScale;

            if (_timeCounter >= secPerDay)
            {
                int nextDay = (int)_day + 1;

                if (Enum.IsDefined(typeof(WeekDay), nextDay))
                {
                    _day =  (WeekDay)nextDay;

                    if (_day == _randomDay && !_hasSpawned)
                    {
                        Notify(null, NotificationFlags.DEMAND_BUILDING);
                        Debug.Log("Notify");
                        if (stopTest)
                        { 
                            _stopTest = true;
                        }
                       
                        _hasSpawned = true;
                    }
                }
                else //Week end
                {
                    _randomDay = PickRandomDay();
                    _day = WeekDay.Monday;
                    _week++;
                    _hasSpawned = false;
                }
                
                _timeCounter = 0;
            }
        }

        private void OnDrawGizmos()
        {
            if (!isGizmos)
            {
                return;
            }
            Handles.Label(
                new Vector3(0,0,0),
                _day.ToString(),
                new GUIStyle()
                {
                    fontSize = 20,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.green
                    }
                }
                );
            
            Handles.Label(
                new Vector3(0,40,0),
                "Week " + _week.ToString(),
                new GUIStyle()
                {
                    fontSize = 20,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.green
                    }
                }
                );
        }

        private WeekDay PickRandomDay()
        {
            return (WeekDay)URandom.Range(0, 8);
        }
        public override void ObserversSetup()
        {
            _buildingSpawner = FindObjectOfType<BuildingSpawner>();
            _observers.Add(_buildingSpawner);
        }
        
    }
}