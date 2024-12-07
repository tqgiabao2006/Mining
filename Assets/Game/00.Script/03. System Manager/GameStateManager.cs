using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

public class GameStateManager : SubjectBase, IObserver
{
    private int _currentLevel;
    private IState _currentState;
    
    private BuildingState _buildingState;
    private NormalState _normalState;
    
    //Chained notifications:
    private RoadManager _roadManager;
    
    //Observesrs:
   private BuildingSpawner _buildingSpawner;

    public void Initialize()
    {
        _buildingState = new BuildingState();
        _normalState = new NormalState();
        _roadManager = GameManager.Instance.RoadManager;
        ObserversSetup();
        
    }
    
    public int CurrentLevel
    {
        get{return _currentLevel;}
        set{_currentLevel = value;}
    }

    private int _currentExp;

    public int CurrentExp
    {
        get {return _currentExp;} 
        set{_currentExp = value;}
    }

    public void UpdateExp(int exp)
    {
        _currentLevel += exp;
    }

    private float _lateupdate = 5.0f;
    private float _currentTime = 0.0f;

    private void Start()
    {
        _currentTime = _lateupdate;
    }

    private void Update()
    {
        _currentTime -= Time.deltaTime;
        if (_currentTime <= 0)
        {
            Test();
        }
    }
    private void Test()
    {
        Notify(0, NotificationFlags.UpdateLevel);
    }

    /// <summary>
    /// Is placing => triggered => _gridManager effect
    /// If new build are spawned or end of placing state => union find check if roads are connected
    /// All classes stop in this, this class will notify all other class
    /// </summary>
    public void OnNotified(object data, string flag)
    {
        // if (flag == NotificationFlags.PlacingState && (bool)data)
        // {
        //     _currentState = _buildingState;
        //     _currentState.Enter();
        //     _currentState.Do();
        //
        // }
        // else
        // {
        //     _currentState.Exit();
        //     _currentState = _normalState;
        // }
    }

    public override void ObserversSetup()
    {
        _buildingSpawner = FindObjectOfType<BuildingSpawner>();
        _observers.Add(_buildingSpawner);
    }
    
    
    
}

public interface IState
{
    void Enter();
    void Do();
    void Exit();
}


public class NormalState : IState
{
    public void Enter()
    {
    }

    public void Do()
    {
    }

    public void Exit()
    {
    }
}


public class BuildingState : IState
{
    public void Enter()
    {
        
    }

    public void Do()
    {
        
    }

    public void Exit()
    {
        
    }
}
