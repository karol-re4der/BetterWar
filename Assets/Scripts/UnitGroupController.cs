using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EUnitFormationType
{
    Heap,
    Ordered
}

//public enum EUnitFiringMode
//{
//    AtOrder,
//    AtWill,
//    Salvo
//}

public class UnitGroupController : MonoBehaviour
{
    public float SightRange;
    public Vector3 WeightCenter;
    public PlayerController OwnerPlayer;
    public List<UnitController> Units = new List<UnitController>();
    public GameObject UnitPrefab;

    [Header("Runtime")]
    public bool ReformNeeded = false;
    public int CurrentSize = 0;
    public int CurrentReloaded = 0;
    public float CurrentReloadProgress = 0;
    public int CurrentInShootingPosition = 0;
    public UnitGroupController EnemyTarget;
    public System.DateTime SalvoIssueTime = System.DateTime.MinValue;
    public System.DateTime LastReformTime = System.DateTime.MinValue;
    public bool IsSelected = false;
    public List<UnitAction> UnitActions = new List<UnitAction>();

    [Header("Initial State")]
    public int InitialSize = 100;

    [Header("Settings")]
    public float ReformCheckFrequency = 3f;
    public float SalvoShootersRequired = 0.8f; //in percentage of possible shooters
    public float SalvoShootingWindow = 1f; //in seconds

    public UnitFormation Formation;

    public EUnitFormationType FormationType = EUnitFormationType.Heap;

    public void Initialize(PlayerController owner)
    {
        UnitActions.Add(new UnitAction("UnitFireMode", new List<string>() { "AtOrder", "AtWill", "Salvo" }));
        UnitActions.Add(new UnitAction("UnitMovementMode", new List<string>() { "Formation", "Loose" }));

        OwnerPlayer = owner;

        CreateUnit(transform.position, 0, EWeaponType.Flag);
        for (int i = 1; i < CurrentSize; i++)
        {
            if (FormationType == EUnitFormationType.Heap)
            {
                int formationSize = InitialSize / 3;
                Vector3 pos = transform.position;
                pos.x += Random.Range(0, formationSize) - formationSize / 2;
                pos.z += Random.Range(0, formationSize) - formationSize / 2;

                CreateUnit(pos, i, (Random.Range(0, 20) == 5)?EWeaponType.Flag:EWeaponType.Ranged);
            }
            else
            {

                //Formation = Globals.GetFormationGroupController().GetFormationsToUse(1).First();
                //Formation.Recompute(transform.position, Vetor3.left, Globals.GetFormationGroupController());
                //SetFormation(Formation);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSizeCounter();
        UpdateReloadedCounter();
        UpdateWeigthCenter();

        if (EnemyTarget == null || !EnemyInRange(EnemyTarget))
        {
            EnemyTarget = FindClosestEnemy();
        }

        if(ReformNeeded && (System.DateTime.Now-LastReformTime).TotalMilliseconds > ReformCheckFrequency*1000f)
        {
            FillVoidsInFormation();
            ReformNeeded = false;
            LastReformTime = System.DateTime.Now;
        }
    }

    private GameObject CreateUnit(Vector3 position, int index, EWeaponType weaponType)
    {
        GameObject newUnit = Instantiate(UnitPrefab, position, Quaternion.identity, Globals.GetUnitSpace);
        newUnit.GetComponent<UnitController>().Initialize(this, index, weaponType);
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
        foreach (UnitController unit in Units)
        {
            if (Formation.IsShootingPosition(unit.PositionInFormation))
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

    public bool IsFireAllowed()
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
                    foreach (UnitController unit in Units)
                    {
                        if (Formation.IsShootingPosition(unit.PositionInFormation))
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
            Formation.Attached = false;
        }
        Formation = formation;
        Formation.Attached = true;

        foreach (UnitController unit in Units)
        {
            unit.MovementMode = EUnitMovementMode.Standstill;
        }

        int unitIndex = 0;
        foreach (Vector3 pos in formation.Positions.OrderByDescending(x=>Vector3.Distance(WeightCenter, x)))
        {
            UnitController closestUnit = Units.Where(x => x.MovementMode == EUnitMovementMode.Standstill).OrderBy(x => Vector3.Distance(x.transform.position, pos)).First();

            closestUnit.TargetPosition = pos;
            closestUnit.TargetFacing = formation.FacingDirection;

            closestUnit.NewMovementStarted(movementMode);

            closestUnit.PositionInFormation = formation.WorldSpaceToPositionInFormation(pos);

            unitIndex++;
        }
    }

    public void FillVoidsInFormation()
    {
        Globals.GetStats.RegisterEvent("GroupTryFillVoids", 1);

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
                            nextUnit.PositionInFormation = new Vector2(x, y);
                            nextUnit.TargetPosition = Formation.PositionInFormationToWorldSpace(nextUnit.PositionInFormation);
                            nextUnit.NewMovementStarted(EUnitMovementMode.Formation);
                            ReformNeeded = true;
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
                            nextUnit.PositionInFormation = new Vector2(x, y);
                            nextUnit.TargetPosition = Formation.PositionInFormationToWorldSpace(nextUnit.PositionInFormation);
                            nextUnit.NewMovementStarted(EUnitMovementMode.Formation);
                            ReformNeeded = true;
                        }
                    }
                }
            }
        }


        //for(int y = (int)voidPositionInFormation.y+1; y<Formation.Rows; y++)
        //{
        //    UnitController nextUnit = Units.Find(x => x.PositionInFormation.x == voidPositionInFormation.x && x.PositionInFormation.y == y && x.MovementMode==EUnitMovementMode.Standstill);
        //    if (nextUnit != null)
        //    {
        //        RefillVoidInFormation(nextUnit.PositionInFormation, nextUnit.transform.position);

        //        nextUnit.TargetPosition = voidPositionInWorldSpace;
        //        nextUnit.PositionInFormation = voidPositionInFormation;
        //        nextUnit.NewMovementStarted(EUnitMovementMode.Formation);


        //        return;
        //    }
        //}
    }

    private List<UnitGroupController> FindEnemiesInSight()
    {
        List<UnitGroupController> enemyGroupsInRange = new List<UnitGroupController>();

        foreach (PlayerController player in PlayerController.GetPlayers())
        {
            if (player != OwnerPlayer)
            {
                foreach (UnitGroupController group in player.UnitGroups)
                {
                    float groupDistance = Vector3.Distance(WeightCenter, group.WeightCenter);
                    if (groupDistance < SightRange)
                    {
                        enemyGroupsInRange.Add(group);
                    }
                }
            }
        }

        return enemyGroupsInRange;
    }

    private UnitGroupController FindClosestEnemy()
    {
        Globals.GetStats.RegisterEvent("GroupLookForTarget", 1);


        UnitGroupController closestEnemy = null;
        float shortestDistance = float.MaxValue;

        foreach (PlayerController player in PlayerController.GetPlayers())
        {
            if (player != OwnerPlayer)
            {
                foreach (UnitGroupController group in player.UnitGroups)
                {
                    float groupDistance = Vector3.Distance(WeightCenter, group.WeightCenter);
                    if (groupDistance < shortestDistance)
                    {
                        closestEnemy = group;
                        shortestDistance = groupDistance;
                    }
                }
            }
        }

        if (shortestDistance < SightRange)
        {
            return closestEnemy;
        }
        return null;
    }

    private bool EnemyInRange(UnitGroupController enemyGroup)
    {
        float distance = Vector3.Distance(enemyGroup.WeightCenter, WeightCenter);
        return distance < SightRange;
    }

    private void UpdateWeigthCenter()
    {
        WeightCenter = Vector3.zero;
        //if (Units.Count < 10)
        //{
            for (int i = 0; i < Units.Count(); i++)
            {
                WeightCenter += Units[i].transform.position;
            }
            WeightCenter /= Units.Count();
        //}
        //else
        //{
        //    int i = 0;
        //    for (i = 0; i < Units.Count()/2; i++)
        //    {
        //        WeightCenter += Units[i].transform.position;
        //    }
        //    WeightCenter /= i;
        //}

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
}
