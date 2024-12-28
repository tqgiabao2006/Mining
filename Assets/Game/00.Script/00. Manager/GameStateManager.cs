using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00._Core_Assembly_Def;
using Game._00.Script._02._System_Manager.Observer;
using Game._00.Script._05._Manager;
using UnityEngine;

public class GameStateManager : SubjectBase, IObserver
{
    private int _currentLevel;
    private IState _currentState;
    
    private BuildingState _buildingState;
    private NormalState _normalState;
    
    //Chained notifications:
    

    public void Initialize()
    {
        _buildingState = new BuildingState();
        _normalState = new NormalState();
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

    private float _lateupdate = 1.0f;
    private float _currentTime = 0.0f;
    private bool callSpawn;

    private void Start()
    {
        _currentTime = _lateupdate;
    }

    private void Update()
    {
        _currentTime -= Time.deltaTime;
        if (_currentTime <= 0 && !callSpawn)
        {
            Notify(0, NotificationFlags.UpdateLevel);
            callSpawn = true; 
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
