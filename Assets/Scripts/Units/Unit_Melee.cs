using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Unit_Melee : UnitController
{
    [Header("Melee Stats")]
    public int MeleeAttempts = 0;

    [Header("Melee Settings")]
    public float MinMeleeAttackRange = 1f;
    public float MeleeAttackRange = 3f;
    public float MeleeEngageRange = 3f;
    public float MeleeAttackSpeed = 1f;
    public float MeleeEngagementRange = 6f;
    public float MeleeBracingRange = 15f;

    [Header("Melee Runtime")]
    public bool ReadyForMeleeAttack = true;
    public bool TargetInMeleeAttackRange = false;
    public bool TargetInEngagementRange = false;
    public float CooldownProgress = 1f;

    [Header("Melee Times")]
    public System.DateTime TryMeleeAttackTimestamp = System.DateTime.MinValue;
    public System.DateTime EngagingMovementCheckTimestamp = System.DateTime.MinValue;
    public float TryMeleeAttackFrequency = 1f; //In seconds
    public float EngagingMovementCheckFrequency = 0.1f; //In seconds


    public void Update()
    {
        base.Update();

        if (IsAlive)
        {
            if ((System.DateTime.Now - TryMeleeAttackTimestamp).TotalMilliseconds > TryMeleeAttackFrequency * 1000f)
            {
                tryAttack();
                TryMeleeAttackTimestamp = System.DateTime.Now;
            }
            tryCooldown();

            if ((System.DateTime.Now - EngagingMovementCheckTimestamp).TotalMilliseconds > EngagingMovementCheckFrequency * 1000f)
            {
                if((MovementMode == EUnitMovementMode.Engaging))
                {
                    TargetPosition = findDesiredMeleePosition(Engaging);
                }
                EngagingMovementCheckTimestamp = System.DateTime.Now;
            }


        }
    }

    public void Initialize(UnitGroupController owner, int index)
    {
        base.Initialize(owner, index);
    }

    #region Melee engagment/attack
    protected bool targetUnitInBracingRange(UnitController unit)
    {
        return Vector3.Distance(unit.transform.position, transform.position) < MeleeBracingRange;
    }

    protected bool targetUnitInEngagementRange(UnitController unit)
    {
        return Vector3.Distance(unit.transform.position, transform.position) < MeleeEngagementRange;
    }

    protected bool targetUnitInMeleeAttackRange(UnitController unit)
    {
        float dist = Vector3.Distance(unit.transform.position, transform.position);
        return MinMeleeAttackRange < dist && dist < MeleeAttackRange;
    }

    protected Vector3 findDesiredMeleePosition(UnitController targetUnit)
    {
        Vector3 pos = Vector3.zero;
        if (targetUnit != null)
        {
            float distance = Vector3.Distance(transform.position, targetUnit.transform.position);
            if (distance<MinMeleeAttackRange)
            {
                pos = Vector3.MoveTowards(transform.position, targetUnit.transform.position, -(MeleeAttackRange - ClippingDistance));

            }
            else
            {
                pos = Vector3.MoveTowards(targetUnit.transform.position, transform.position, MeleeAttackRange - ClippingDistance);
            }

        }
        return pos;
    }

    public void tryAttack()
    {
        if (EngagementIsValid())
        {
            if (targetUnitInMeleeAttackRange(Engaging) && targetUnitAngled(Engaging))
            {
                if (ReadyForMeleeAttack)
                {
                    attack();
                }
            }
            else
            {
                MoveIntoPosition(findDesiredMeleePosition(Engaging), EUnitMovementMode.Engaging);
                ChangeFacing(Engaging.transform.position-transform.position);
            }
        }
        else
        {
            tryEngage(getTargets());
        }
    }

    private void tryEngage(List<UnitGroupController> potentialTargets)
    {
        Globals.GetStats.RegisterEvent("UnitTryEngage", 1);

        List<UnitController> targetUnits = new List<UnitController>();
        UnitController targetUnit = null;
        potentialTargets.ForEach(x => targetUnits.AddRange(x.Units));
        if (targetUnits != null && targetUnits.Count()>0)
        {
            targetUnits = targetUnits.Where(x => targetUnitInEngagementRange(x) && x.CanBeEngagedBy(this)).ToList();
            if(targetUnits!=null && targetUnits.Count() > 0)
            {
                targetUnit = targetUnits.OrderBy(x => Vector3.Distance(GetModelCenterPoint(), x.GetModelCenterPoint())).First();
            }
        }

        //select final target
        if (targetUnit!=null)
        {
            Engaging = targetUnit;
            Engaging.SetAsEngagementTarget(this);
            AnimationController.SetBool("Engaging", true);
        }
        else
        {
            AnimationController.SetBool("Engaging", Engaging!=null);
        }
    }

    private void attack()
    {
        //Reset engagment variables
        ReadyForMeleeAttack = false;
        CooldownProgress = 0;

        //Engage
        float rand = (float)UnityEngine.Random.Range(0, 10);
        bool somethingHit = rand == OwnerGroup.HitOnValue;

        if (somethingHit)
        {
            EnemiesKilled++;

            Vector3 dir = (Engaging.GetModelCenterPoint() - GetModelCenterPoint()).normalized;
            Engaging.RegisterHit(dir, 1f);
            Disengage();
        }

        AnimationController.SetTrigger("Attack");
        MeleeAttempts++;
    }

    private void tryCooldown()
    {
        if (CooldownProgress >= 1f)
        {
            ReadyForMeleeAttack = true;
        }
        else
        {
            CooldownProgress += MeleeAttackSpeed * Time.deltaTime;
        }
    }
    #endregion

    public override bool TriesToKeepFormation()
    {
        return OwnerGroup.CurrentUnitsEngaged > 0 && EngagedBy.Count()==0 && Engaging==null;
    }
}
