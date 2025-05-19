using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.Building;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
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

        [Header("Time settings")]
        [Tooltip("Stop after 1 week to determine the week.")]
        [SerializeField] private bool stopTest = false;
        
        [SerializeField] private bool isGizmos;
        
        [SerializeField] private float secPerDay;
        
        [ReadOnly] private WeekDay _day;
          
        [SerializeField] private float timeScale;

        [Header("Weeks update settings")]
        [SerializeField]
        [Range(0, 1)]
        [Tooltip("Increase number of notifications to building spawner over a week")]
        private float increaseFrequency;

        private float _curFrequency;

        private Queue<WeekDay> _notiQueue;
        
        private float _timeCounter;
        
        private BuildingSpawner _buildingSpawner;

        private int _week;

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
            Tick();
        }

        private void Start()
        {
            ObserversSetup();
            
            _notiQueue = new Queue<WeekDay>();

            _curFrequency = 1;
            
            AddRandomDays();
        }

        /// <summary>
        /// Each week, pick a random day to notify to spawn a building
        /// </summary>
        private void Tick()
        {
            _timeCounter += Time.deltaTime * timeScale;
        
            if (_timeCounter >= secPerDay)
            {
                if (Enum.IsDefined(typeof(WeekDay), _day))
                {
                    if (_notiQueue.Count > 0 && _day <= _notiQueue.Peek())
                    {
                        _notiQueue.Dequeue();
                        Notify(null, NotificationFlags.DEMAND_BUILDING);
                    }
                }
                int nextDay = (int)_day + 1;

                if (Enum.IsDefined(typeof(WeekDay), nextDay))
                {
                    _day = (WeekDay)nextDay;
                }
                else // Week ends
                {
                    _curFrequency += increaseFrequency;
                    Notify(null, NotificationFlags.WEEK_PASS);
                    AddRandomDays(Mathf.Max(1, Mathf.FloorToInt(_curFrequency)));
                    _day = WeekDay.Monday;
                    _week++;
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

        /// <summary>
        /// Add random, unique days, in other into the queue
        /// </summary>
        /// <param name="cnt">number of days to notify</param>
        private void AddRandomDays(int cnt = 1)
        {
            //Wrap around days
            cnt = Mathf.Min(cnt, 7);
            
            if (cnt == 1)
            { 
                _notiQueue.Enqueue((WeekDay)UnityEngine.Random.Range(0, 7));
                return;
            }
            
            //Pick random
            List<WeekDay> days = Enum.GetValues(typeof(WeekDay)).Cast<WeekDay>().ToList();

            //Randomly shuffle
            for (int i = days.Count - 1; i >= 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                
                (days[i], days[j]) = (days[j], days[i]);
            }
            
            //Pick first => then sorting
            List<WeekDay> pick =  new List<WeekDay>();
            for (int i = 0; i < cnt; i++)
            {
                pick.Add(days[i]);
            }
            
            pick.Sort();

            foreach (WeekDay day in pick)
            {
                Debug.Log(day.ToString());
                _notiQueue.Enqueue(day);
            }
        }

        public override void ObserversSetup()
        {
            _buildingSpawner = FindObjectOfType<BuildingSpawner>();
            _observers.Add(_buildingSpawner);
        }
        
    }
}