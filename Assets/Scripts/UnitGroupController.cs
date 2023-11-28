using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EUnitFormationType
{
    Heap,
    Ordered
}

public enum EUnitFiringMode
{
    AtOrder,
    AtWill,
    Salvo
}

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
    public EUnitFiringMode FiringMode = EUnitFiringMode.AtOrder;
    public System.DateTime SalvoIssueTime = System.DateTime.MinValue;
    public System.DateTime LastReformTime = System.DateTime.MinValue;

    [Header("Initial State")]
    public int InitialSize = 100;

    [Header("Settings")]
    public float ReformCheckFrequency = 1f;
    public float SalvoShootersRequired = 0.8f; //in percentage of possible shooters
    public float SalvoShootingWindow = 1f; //in seconds

    public UnitFormation Formation;

    public EUnitFormationType FormationType = EUnitFormationType.Heap;

    public void Initialize(PlayerController owner)
    {
        OwnerPlayer = owner;

        CreateUnit(transform.position, true, 0);
        for (int i = 1; i < CurrentSize; i++)
        {
            if (FormationType == EUnitFormationType.Heap)
            {
                int formationSize = InitialSize / 3;
                Vector3 pos = transform.position;
                pos.x += Random.Range(0, formationSize) - formationSize / 2;
                pos.z += Random.Range(0, formationSize) - formationSize / 2;
                CreateUnit(pos, Random.Range(0, 20)==5, i);
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

        if(ReformNeeded &&(System.DateTime.Now-LastReformTime).Seconds > ReformCheckFrequency)
        {
            Reform();
        }
    }

    private GameObject CreateUnit(Vector3 position, bool flag, int index)
    {
        GameObject newUnit = Instantiate(UnitPrefab, position, Quaternion.identity, Globals.GetUnitSpace);
        newUnit.GetComponent<UnitController>().Initialize(this, flag, index);
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

        if (sizeCache != CurrentSize)
        {
            ReformNeeded = true;
        }
    }

    private void UpdateReloadedCounter()
    {
        CurrentReloaded = 0;
        CurrentReloadProgress = 0;
        CurrentInShootingPosition = 0;
        foreach (UnitController unit in Units)
        {
            if (unit.InShootingPosition)
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
        if(FiringMode == EUnitFiringMode.AtWill)
        {
            return true;
        }
        else if (FiringMode == EUnitFiringMode.Salvo)
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
                    if (unit.InShootingPosition)
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
        return false;
    }

    public void SetFormation(UnitFormation formation)
    {
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
        foreach (Vector3 pos in formation.Positions)
        {
            UnitController closestUnit = Units.Where(x => x.MovementMode == EUnitMovementMode.Standstill).OrderBy(x => Vector3.Distance(x.transform.position, pos)).First();

            closestUnit.TargetPosition = pos;
            closestUnit.TargetFacing = formation.FacingDirection;

            closestUnit.NewMovementStarted();
            if (formation.IsShootingPosition(unitIndex))
            {
                closestUnit.InShootingPosition = true;
            }
            else
            {
                closestUnit.InShootingPosition = false;

            }
            unitIndex++;
        }
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
        if (Units.Count < 10)
        {
            for (int i = 0; i < Units.Count(); i++)
            {
                WeightCenter += Units[i].transform.position;
            }
            WeightCenter /= Units.Count();
        }
        else
        {
            for (int i = 0; i < Units.Count(); i+=2)
            {
                WeightCenter += Units[i].transform.position;
            }
            WeightCenter /= Units.Count()/2;
        }

    }

    public void RemoveUnit(UnitController unit)
    {
        Units.Remove(unit);
    }

    public void Reform()
    {
        Formation.Reform(this, Globals.GetFormationGroupController.GetUnitsMargin());
        SetFormation(Formation);
        ReformNeeded = false;
    }
}
