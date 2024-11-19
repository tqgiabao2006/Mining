using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager :MonoBehaviour
{
    private int _currentLevel;

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
}

public interface IState
{
    void Enter();
    void Do();
    void Exit();
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
