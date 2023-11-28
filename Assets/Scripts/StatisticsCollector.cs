using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StatisticsCollector : MonoBehaviour
{
    private Dictionary<string, float> _stats = new Dictionary<string, float>();
    private DateTime _lastUpdate = DateTime.Now;

    public bool CountStats = true;

    [Header("Group stats")]
    public float GroupTryReformPerSecond = 0;
    public float GroupLookForTargetPerSecond = 0;

    [Header("Unit stats")]
    public float UnitTakeAimPerSecond = 0;
    public float UnitShootPerSecond = 0;

    [Header("Unit stats")]
    public float MarkersInPool = 0;
    public float ProjectilesInPool = 0;
    public float SmokesInPool = 0;




    public void RegisterEvent(string eventName, float value)
    {
        _stats[eventName] += value;
    }


    void Start()
    {
        _stats.Add("GroupTryReform", 0);
        _stats.Add("GroupLookForTarget", 0);
        _stats.Add("UnitTakeAim", 0);
        _stats.Add("UnitShoot", 0);
        _stats.Add("MarkersInPool", 0);
        _stats.Add("SmokesInPool", 0);
        _stats.Add("ProjectilesInPool", 0);

    }

    void Update()
    {
        if (CountStats)
        {
            float timePassed = (DateTime.Now - _lastUpdate).Seconds;
            if (timePassed >= 1)
            {
                GroupTryReformPerSecond = Mathf.Round((GroupTryReformPerSecond+ (_stats["GroupTryReform"]/ timePassed)) / 2);
                GroupLookForTargetPerSecond = Mathf.Round((GroupTryReformPerSecond + (_stats["GroupLookForTarget"] / timePassed)) / 2);

                UnitTakeAimPerSecond = Mathf.Round((GroupTryReformPerSecond + (_stats["UnitTakeAim"] / timePassed)) / 2);
                UnitShootPerSecond = Mathf.Round((GroupTryReformPerSecond + (_stats["UnitShoot"] / timePassed)) / 2);

                MarkersInPool = _stats["MarkersInPool"];
                SmokesInPool = _stats["SmokesInPool"];
                ProjectilesInPool = _stats["ProjectilesInPool"];


                _stats["GroupTryReform"] = 0;
                _stats["GroupLookForTarget"] = 0;
                _stats["UnitTakeAim"] = 0;
                _stats["UnitShoot"] = 0;


                _lastUpdate = DateTime.Now;
            }
        }
    }
}
