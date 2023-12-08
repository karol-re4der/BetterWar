using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UnitGroupController : MonoBehaviour
{
    private GameObject _unitPrefab_Relevant;

    [Header("References")]
    public GameObject UnitPrefab_Melee;
    public GameObject UnitPrefab_Ranged;
    public GameObject UnitPrefab_Mounted;

    [Header("Runtime")]
    public Vector3 WeightCenter;
    public float FormationSpan; //Distance from weight center to the furthest unit
    public bool ReformNeeded = false;
    public int CurrentSize = 0;
    public int CurrentUnitsEngaged = 0;
    public int CurrentReloaded = 0;
    public float CurrentReloadProgress = 0;
    public int CurrentInShootingPosition = 0;
    public List<UnitGroupController> EnemiesInRange;
    public UnitGroupController TargetEnemyOverride;
    public List<UnitController> Units = new List<UnitController>();
    public PlayerController OwnerPlayer;
    public bool IsSelected = false;
    public List<UnitAction> UnitActions = new List<UnitAction>();
    public UnitFormation Formation;
    public bool TargetInRange = false;
    public bool TargetInSight = false;
    public bool TargetAngled = false;

    [Header("Timestamps")]
    public System.DateTime SalvoIssueTime = System.DateTime.MinValue;
    public System.DateTime LastReformTime = System.DateTime.MinValue;
    public System.DateTime LastCountersCheckTime = System.DateTime.MinValue;
    public System.DateTime LastLookForTargetsTime = System.DateTime.MinValue;
    public float ReformCheckFrequency = 4f;
    public float CountersCheckFrequency = 1f;
    public float LookForTargetsFrequency = 1f;

    [Header("Initial State")]
    public int InitialSize = 100;
    public EUnitType GroupUnitType;

    [Header("Settings")]
    public float SightAngle = 120; //In degrees, total angle
    public float SightRange = 100f;
    public float ProjectileEffectiveRange = 200f;
    public float SalvoShootersRequired = 0.8f; //in percentage of possible shooters
    public float SalvoShootingWindow = 1f; //in seconds

    public int HitOnValue = 0;

    public void Initialize(PlayerController owner, EUnitType unitType, Vector3 pos)
    {
        OwnerPlayer = owner;
        GroupUnitType = unitType;

        //Setup actions
        UnitActions.Add(new UnitAction("UnitMovementMode", new List<string>() { "Formation", "Loose" }));

        //differentiate by type
        switch (unitType)
        {
            case EUnitType.Ranged:
                _unitPrefab_Relevant = UnitPrefab_Ranged;
                UnitActions.Add(new UnitAction("UnitFireMode", new List<string>() { "AtOrder", "AtWill", "Salvo" }));
                break;
            case EUnitType.Melee:
                _unitPrefab_Relevant = UnitPrefab_Melee;
                break;
            case EUnitType.Mounted:
                _unitPrefab_Relevant = UnitPrefab_Mounted;
                break;
        }

        //spawn individual units
        for (int i = 0; i < CurrentSize; i++)
        {
            int formationSize = InitialSize / 3;
            Vector3 nextPos = pos;
            nextPos.x += Random.Range(0, formationSize) - formationSize / 2;
            nextPos.z += Random.Range(0, formationSize) - formationSize / 2;

            createUnit(nextPos, i, unitType);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if ((System.DateTime.Now - LastCountersCheckTime).TotalMilliseconds > CountersCheckFrequency * 1000f)
        {
            UpdateSizeCounter();
            UpdateEngagedCounter();
            if (GroupUnitType==EUnitType.Ranged) UpdateReloadedCounter();
            updateWeightCenter();
            updateFormationSpan();

            LastCountersCheckTime = System.DateTime.Now;
        }

        if ((System.DateTime.Now - LastLookForTargetsTime).TotalMilliseconds > LookForTargetsFrequency * 1000f)
        {
            EnemiesInRange = findEnemiesInRange();

            if (TargetEnemyOverride != null)
            {
                if (!enemyInRange(TargetEnemyOverride))
                {
                    TargetEnemyOverride = null;
                }
            }

            LastLookForTargetsTime = System.DateTime.Now;
        }

        if(ReformNeeded && (System.DateTime.Now-LastReformTime).TotalMilliseconds > ReformCheckFrequency*1000f)
        {
            FillVoidsInFormation();
            ReformNeeded = false;
            LastReformTime = System.DateTime.Now;
        }
    }

    private GameObject createUnit(Vector3 position, int index, EUnitType unitType)
    {
        GameObject newUnit = Instantiate(_unitPrefab_Relevant, position, Quaternion.identity, transform);

        //differentiate by type
        switch (unitType)
        {
            case EUnitType.Ranged:
                newUnit.GetComponent<Unit_Ranged>().Initialize(this, index);
                break;
            case EUnitType.Melee:
                newUnit.GetComponent<Unit_Melee>().Initialize(this, index);
                break;
            case EUnitType.Mounted:
                newUnit.GetComponent<Unit_Mounted>().Initialize(this, index);
                break;
        }

        Units.Add(newUnit.GetComponent<UnitController>());
        return newUnit;
    }

    private void UpdateSizeCounter()
    {
        int sizeCache = CurrentSize;
        CurrentSize = 0;
        foreach (UnitController unit in Units)
        {
            CurrentSize++;
        }
    }

    private void UpdateReloadedCounter()
    {
        CurrentReloaded = 0;
        CurrentReloadProgress = 0;
        CurrentInShootingPosition = 0;
        foreach (Unit_Ranged unit in Units)
        {
            if (unit.IsInShootingPosition())
            {
                if (unit.ReloadProgress > 1)
                {
                    CurrentReloaded++;
                    CurrentReloadProgress++;
                }
                else
                {
                    CurrentReloadProgress += unit.ReloadProgress;
                }
                CurrentInShootingPosition++;
            }
        }
    }

    private void UpdateEngagedCounter()
    {
        CurrentUnitsEngaged = 0;
        foreach (UnitController unit in Units)
        {
            if(unit.Engaging || unit.EngagedBy.Count() > 0)
            {
                CurrentUnitsEngaged++;
            }
        }
    }

    public bool IsFireAllowed()
    {
        if (GroupUnitType == EUnitType.Ranged)
        {
            UnitAction fireModeAction = UnitActions.Find(x => x.ActionName.Equals("UnitFireMode"));
            if (fireModeAction != null)
            {
                if (fireModeAction.GetCurrentState().Equals("AtWill"))
                {
                    return true;
                }
                else if (fireModeAction.GetCurrentState().Equals("Salvo"))
                {
                    if ((System.DateTime.Now - SalvoIssueTime).TotalSeconds < SalvoShootingWindow)
                    {
                        return true;
                    }
                    else
                    {
                        int unitsReady = 0;
                        int shootersTotal = 0;
                        foreach (Unit_Ranged unit in Units)
                        {
                            if (unit.IsInShootingPosition())
                            {
                                if (unit.ReadyToShoot)
                                {
                                    unitsReady++;
                                }
                                shootersTotal++;
                            }
                        }
                        if (((float)unitsReady / shootersTotal) >= SalvoShootersRequired)
                        {
                            SalvoIssueTime = System.DateTime.Now;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return false;
    }

    public void SetFormation(UnitFormation formation, bool useRelevantMovementMode)
    {
        EUnitMovementMode movementMode = EUnitMovementMode.Formation;

        if (useRelevantMovementMode)
        {
            UnitAction movementModeAction = UnitActions.Find(x => x.ActionName.Equals("UnitMovementMode"));
            if (movementModeAction != null)
            {
                movementMode = movementModeAction.GetCurrentState().Equals("Loose") ? EUnitMovementMode.Loose : EUnitMovementMode.Formation;
            }
        }

        if (Formation != null)
        {
            Formation.Detach();
        }
        Formation = formation;
        Formation.Attach();

        foreach (UnitController unit in Units)
        {
            unit.MovementMode = EUnitMovementMode.Standstill;
        }

        int unitIndex = 0;
        foreach (Vector3 pos in formation.Positions.OrderByDescending(x=>Vector3.Distance(WeightCenter, x)))
        {
            UnitController closestUnit = Units.Where(x => x.MovementMode == EUnitMovementMode.Standstill).OrderBy(x => Vector3.Distance(x.transform.position, pos)).FirstOrDefault();

            if (closestUnit != null)
            {
                Vector2 coordsInFormation = formation.WorldSpaceToPositionInFormation(pos);
                formation.UnitsAttached[(int)coordsInFormation.x, (int)coordsInFormation.y] = closestUnit;

                closestUnit.MoveIntoPosition(pos, movementMode);
                closestUnit.ChangeFacing(formation.FacingDirection);

                closestUnit.PositionInFormation = formation.WorldSpaceToPositionInFormation(pos);
            }

            unitIndex++;
        }
    }

    public void FillVoidsInFormation()
    {
        Globals.GetStats.RegisterEvent("GroupTryFillVoids", 1);

        if (ReformNeeded)
        {
            //Fill from back to front
            for (int y = 0; y < Formation.Rows; y++)
            {
                for (int x = 0; x < Formation.Columns; x++)
                {
                    UnitController unitAtPosition = Units.Find(u => u.PositionInFormation == new Vector2(x, y));

                    if (unitAtPosition == null)
                    {
                        for (int i = y + 1; i < Formation.Rows; i++)
                        {
                            UnitController nextUnit = Units.Find(u => u.PositionInFormation == new Vector2(x, i));
                            if (nextUnit != null)
                            {
                                Formation.UnitsAttached[x, i] = null;
                                Formation.UnitsAttached[x, y] = nextUnit;

                                nextUnit.PositionInFormation = new Vector2(x, y);
                                nextUnit.MoveIntoPosition(Formation.PositionInFormationToWorldSpace(nextUnit.PositionInFormation), EUnitMovementMode.Formation);
                                break;
                            }
                        }
                    }
                }
            }

            //Fill remaining with closest from back rows
            if (Formation.Columns > 1)
            {
                for (int y = 0; y < Formation.Rows; y++)
                {
                    for (int i = 1; i <= Formation.Columns; i++)
                    {
                        int dir = -1;

                        if (Formation.Columns % 2 == 0)
                        {
                            dir = (i % 2 == 0) ? 1 : -1;
                        }
                        else
                        {
                            dir = (i % 2 == 0) ? -1 : 1;
                        }

                        int x = (int)((Formation.Columns / 2) - (dir * Mathf.Floor(i / 2f)));
                        UnitController unitAtPosition = Units.Find(u => u.PositionInFormation == new Vector2(x, y));

                        if (unitAtPosition == null)
                        {
                            UnitController nextUnit = Units.Where(u => u.PositionInFormation.y > y)?.OrderBy(u => Vector2.Distance(new Vector2(x, y), u.PositionInFormation)).FirstOrDefault();
                            if (nextUnit != null)
                            {
                                Formation.UnitsAttached[(int)nextUnit.PositionInFormation.x, (int)nextUnit.PositionInFormation.y] = null;
                                Formation.UnitsAttached[x, y] = nextUnit;

                                nextUnit.PositionInFormation = new Vector2(x, y);
                                nextUnit.MoveIntoPosition(Formation.PositionInFormationToWorldSpace(nextUnit.PositionInFormation), EUnitMovementMode.Formation);
                            }
                        }
                    }
                }
            }
            ReformNeeded = false;
        }

        if (CurrentUnitsEngaged>0)
        {
            foreach (UnitController unit in Units)
            {
                if (unit.TriesToKeepFormation())
                {
                    unit.MoveIntoPosition(unit.FindDesiredEngagementPositionInFormation(), EUnitMovementMode.Formation);
                }
            }
        }
    }

    private List<UnitGroupController> findEnemiesInRange()
    {
        Globals.GetStats.RegisterEvent("GroupLookForTarget", 1);

        List<UnitGroupController> targets = new List<UnitGroupController>();

        foreach (PlayerController player in PlayerController.GetPlayers())
        {
            if (player != OwnerPlayer)
            {
                foreach (UnitGroupController group in player.UnitGroups)
                {
                    if (enemyInRange(group))
                    {
                        targets.Add(group);
                    }
                }
            }
        }

        return targets;
    }

    private bool enemyInRange(UnitGroupController enemyGroup)
    {
        float distance = Vector3.Distance(enemyGroup.WeightCenter, WeightCenter);
        return distance-enemyGroup.FormationSpan < SightRange;
    }

    private void updateWeightCenter()
    {
        WeightCenter = Vector3.zero;
        for (int i = 0; i < Units.Count(); i++)
        {
            WeightCenter += Units[i].transform.position;
        }
        WeightCenter /= Units.Count();
    }

    private void updateFormationSpan()
    {
        FormationSpan = 0f;
        float dist = 0f;
        foreach(UnitController unit in Units)
        {
            dist = Vector3.Distance(unit.transform.position, WeightCenter);

            if (dist > FormationSpan)
            {
                FormationSpan = dist;
            }
        }
    }

    public void ClearUnitTargets()
    {
        foreach (UnitController unit in Units)
        {
            if(unit is Unit_Ranged)
            {
                ((Unit_Ranged)unit).AimedAt = null;
            }
            unit.Disengage();
        }
    }

    public void RemoveUnit(UnitController unit)
    {
        Units.Remove(unit);
    }

    public void Reform(bool useRelevantMovementMode)
    {
        Formation.Reform(this, Globals.GetFormationGroupController.GetUnitsMargin());
        SetFormation(Formation, useRelevantMovementMode);
    }

    public void HighlightGroup()
    {
        foreach(UnitController unit in Units)
        {
            unit.HighlightUnit();
        }
    }

    public void StopEngaging(UnitController unit)
    {
        //if
    }

    public int UnitsInMelee()
    {
        int result = 0;
        foreach(UnitController unit in Units)
        {
            if(unit.EngagementIsValid())
            {
                result++;
            }
        }

        return result;
    }
}
