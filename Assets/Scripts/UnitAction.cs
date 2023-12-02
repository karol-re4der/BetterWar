using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UnitAction
{
    public string ActionName = "DefaultAction";
    public List<string> ActionStates = new List<string>();
    private int _currentStateIndex = 0;

    public UnitAction(string actionName, List<string> actionStates)
    {
        this.ActionName = actionName;
        this.ActionStates = actionStates;
    }

    public string GetCurrentState()
    {
        return ActionStates.ElementAt(_currentStateIndex);
    }

    public void ChangeState(string stateName)
    {
        _currentStateIndex = ActionStates.IndexOf(stateName);
    }
}
