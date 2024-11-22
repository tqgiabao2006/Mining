using System;
using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

public class GameStateManager : MonoBehaviour, IObserver
{
    private int _currentLevel;
    private IState _currentState;
    
    private BuildingState _buildingState;
    private NormalState _normalState;
    
    //Chained notifications:
    private RoadManager _roadManager;

    public void Initialize()
    {
        _buildingState = new BuildingState();
        _normalState = new NormalState();
        _roadManager = GameManager.Instance.RoadManager;
        
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

    /// <summary>
    /// Is placing => triggered => grid effect
    /// If new build are spawned or end of placing state => union find check if roads are connected
    /// All classes stop in this, this class will notify all other class
    /// </summary>
    public void OnNotified(object data, string flag)
    {
        if (flag == NotificationFlags.PlacingState && (bool)data)
        {
            _currentState = _buildingState;
            _currentState.Enter();
            _currentState.Do();

        }
        else
        {
            _currentState.Exit();
            _currentState = _normalState;
        }
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
