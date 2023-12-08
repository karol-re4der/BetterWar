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
    public float GroupTryFillVoidsPerSecond = 0;
    public float GroupLookForTargetPerSecond = 0;

    [Header("Unit stats")]
    public float UnitTakeAimPerSecond = 0;
    public float UnitShootPerSecond = 0;
    public float UnitTryEngagePerSecond = 0;

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
        _stats.Add("UnitTryEngage", 0);
        _stats.Add("GroupTryFillVoids", 0);
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
            float timePassed = (float)(DateTime.Now - _lastUpdate).TotalMilliseconds;
            if (timePassed >= 1000f)
            {
                GroupTryFillVoidsPerSecond = Mathf.Round((GroupTryFillVoidsPerSecond + (_stats["GroupTryFillVoids"] / timePassed)) / 2);
                GroupLookForTargetPerSecond = Mathf.Round((GroupLookForTargetPerSecond + (_stats["GroupLookForTarget"] / timePassed)) / 2);

                UnitTakeAimPerSecond = Mathf.Round((UnitTakeAimPerSecond + (_stats["UnitTakeAim"] / timePassed)) / 2);
                UnitShootPerSecond = Mathf.Round((UnitShootPerSecond + (_stats["UnitShoot"] / timePassed)) / 2);
                UnitTryEngagePerSecond = Mathf.Round((UnitTryEngagePerSecond + (_stats["UnitTryEngage"] / timePassed)) / 2);

                MarkersInPool = _stats["MarkersInPool"];
                SmokesInPool = _stats["SmokesInPool"];
                ProjectilesInPool = _stats["ProjectilesInPool"];


                _stats["GroupTryFillVoids"] = 0;
                _stats["GroupLookForTarget"] = 0;
                _stats["UnitTakeAim"] = 0;
                _stats["UnitShoot"] = 0;


                _lastUpdate = DateTime.Now;
            }
        }
    }
}
