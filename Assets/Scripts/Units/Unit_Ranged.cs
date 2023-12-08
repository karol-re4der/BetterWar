using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Unit_Ranged : Unit_Melee
{
    [Header("Ranged Stats")]
    public int EnemiesShot = 0;
    public int AlliesShot = 0;
    public int ShotsFired = 0;
    public int ShotsMissed = 0;

    [Header("Ranged Runtime")]
    public float ReloadProgress = 0f;
    public bool ReadyToShoot = true;
    public float IndividualShootingSpeed = 0f;
    public UnitController AimedAt;

    [Header("Ranged Settings")]
    public float ProjectileAccuracy = 1f; //measured in angle deviation
    public float ReloadSpeedVariation = 0.1f; //Percentage deviation from ShootingSpeed
    public float ReloadSpeed = 0.1f;

    [Header("Ranged References")]
    public Transform ShootingPoint;
    public GameObject RangeIndicator;

    [Header("Ranged Times")]
    public System.DateTime LastTryShootTimestamp = System.DateTime.Now;
    public float TryShootFrequency = 1f; //In seconds

    public void Update()
    {
        base.Update();

        if (IsAlive)
        {
            //Shooting
            if ((System.DateTime.Now - LastTryShootTimestamp).TotalMilliseconds > TryShootFrequency * 1000f)
            {
                tryShoot();
                LastTryShootTimestamp = System.DateTime.Now;
            }
            tryReload();

            //Show range if selected
            if (OwnerGroup.IsSelected && IsInShootingPosition())
            {
                HighlightRange();
            }
            else
            {
                HideRange();
            }
        }
    }

    public void Initialize(UnitGroupController owner, int index)
    {
        base.Initialize(owner, index);

        IndividualShootingSpeed += ReloadSpeed + (ReloadSpeed * UnityEngine.Random.Range(-ReloadSpeedVariation, ReloadSpeedVariation));
    }

    #region Shooting
    private void tryShoot()
    {
        if (MovementMode == EUnitMovementMode.Standstill)
        {
            if (IsInShootingPosition())
            {
                if (OwnerGroup.EnemiesInRange != null)
                {
                    if (ReadyToShoot)
                    {
                        if (AimedAt == null)
                        {
                            takeAim(getTargets());
                        }
                        else
                        {
                            TargetAngled = targetUnitAngled(AimedAt);
                            if (TargetAngled)
                            {
                                TargetInRange = targetUnitInRange(AimedAt);
                                if (!TargetInRange)
                                {
                                    takeAim(getTargets());
                                }
                                else
                                {
                                    TargetInSight = targetUnitInSight(AimedAt);
                                    if (!TargetInSight)
                                    {
                                        takeAim(getTargets());
                                    }
                                    else if (OwnerGroup.IsFireAllowed())
                                    {
                                        shoot();
                                    }
                                }
                            }
                            else
                            {
                                takeAim(getTargets());
                            }
                        }
                    }

                }
            }
        }
    }

    private void takeAim(List<UnitGroupController> potentialTargets)
    {
        Globals.GetStats.RegisterEvent("UnitTakeAim", 1);

        List<UnitController> targetUnits = new List<UnitController>();
        potentialTargets.ForEach(x => targetUnits.AddRange(x.Units));
        targetUnits = targetUnits.Where(x => targetUnitAngled(x) && targetUnitInRange(x) && targetUnitInSight(x)).OrderBy(x => Vector3.Distance(getModelShootingPoint(), x.GetModelCenterPoint())).ToList();
        //If enemy is bunched up, aim at front
        if (targetUnits.Count() > 10)
        {
            targetUnits = targetUnits.Take(targetUnits.Count() / 4).ToList();
        }

        //select final target
        if (targetUnits.Count() > 0)
        {
            AimedAt = targetUnits.ElementAt(Random.Range(0, targetUnits.Count()));
            AnimationController.SetBool("IsAiming", true);
        }
        else
        {
            AimedAt = null;
            AnimationController.SetBool("IsAiming", false);
        }

        //Reset rotation
        AngleTowardsTargetPosition = 180;
    }

    private void shoot()
    {
        Globals.GetStats.RegisterEvent("UnitShoot", 1);

        //Reset shooting variables
        ReadyToShoot = false;
        ReloadProgress = 0;

        //Compute path
        RaycastHit hit;

        //Get direction, then apply deviation
        Vector3 dir = (AimedAt.GetModelCenterPoint() - GetModelCenterPoint()).normalized;
        dir = deviateShotDirection(dir, 0, ProjectileAccuracy);

        //Shoot ray
        bool somethingHit = false;
        float timeToHit = 0f;
        if (Physics.Raycast(getModelShootingPoint(), dir, out hit, OwnerGroup.ProjectileEffectiveRange, LayerMask.GetMask("Units"), QueryTriggerInteraction.UseGlobal))
        {
            if (hit.collider != null)
            {
                somethingHit = true;
                timeToHit = Globals.GetProjectileSystem.GetTimeToHit(getModelShootingPoint(), hit.point);
            }
        }

        //Shoot
        if (somethingHit)
        {
            if (hit.collider.transform.parent.gameObject.GetComponent<UnitController>().OwnerGroup.OwnerPlayer != OwnerGroup.OwnerPlayer)
            {
                EnemiesKilled++;
            }
            else
            {
                AlliesKilled++;
            }

            Globals.GetProjectileSystem.NewProjectileAtPoint(getModelShootingPoint(), hit.point, true);
            hit.collider.transform.parent.gameObject.GetComponent<UnitController>().RegisterHit(dir, timeToHit);
        }
        else
        {
            ShotsMissed++;
            Globals.GetProjectileSystem.NewProjectileInDirection(getModelShootingPoint(), dir, true);
        }

        AnimationController.SetTrigger("Attack");
        ShotsFired++;
        AimedAt = null;
    }

    private void tryReload()
    {
        if (ReloadProgress >= 1f)
        {
            ReadyToShoot = true;
            AnimationController.SetBool("IsReloaded", true);

        }
        else
        {
            ReloadProgress += IndividualShootingSpeed * Time.deltaTime;
            AnimationController.SetBool("IsReloaded", false);
        }
    }

    private Vector3 getModelShootingPoint()
    {
        return ShootingPoint.position;
    }

    private Vector3 deviateShotDirection(Vector3 dir, float min, float max)
    {
        // Find random angle between min & max inclusive
        float xNoise = Random.Range(min, max);
        float yNoise = Random.Range(min, max);
        float zNoise = Random.Range(min, max);

        // Convert Angle to Vector3
        Vector3 noise = new Vector3(
          Mathf.Sin(2 * Mathf.PI * xNoise / 360),
          Mathf.Sin(2 * Mathf.PI * yNoise / 360),
          Mathf.Sin(2 * Mathf.PI * zNoise / 360)
        );

        return dir + noise;
    }

    public bool IsInShootingPosition()
    {
        if (GetUnitOnLeft() == null)
        {
            return true;
        }
        else if (GetUnitOnRight() == null)
        {
            return true;
        }
        else if(GetUnitInFront() == null)
        {
            return true;
        }
        else if(GetUnitInBack() == null)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region Highlights

    public void HighlightRange()
    {
        if (OwnerGroup.GroupUnitType == EUnitType.Ranged)
        {
            CancelInvoke("fadeOutRangeHighlight");

            int pointsDensity = 20;

            RangeIndicator.SetActive(true);
            LineRenderer lrend = RangeIndicator.GetComponent<LineRenderer>();

            if (lrend.endWidth < 0.1f)
            {
                lrend.endWidth = 0.1f;
                lrend.startWidth = 0.1f;
            }
            else if (!IsInvoking("fadeInRangeHighlight") && lrend.endWidth < RangeLineWidth)
            {
                CancelInvoke("fadeOutRangeHighlight");
                Invoke("fadeInRangeHighlight", 0.03f);
            }

            float anglePerPoint = SightAngle / pointsDensity * Mathf.Deg2Rad;

            List<Vector3> points = new List<Vector3>();
            points.Add(transform.position + Vector3.RotateTowards(transform.forward, -transform.forward, -(SightAngle / 2 * Mathf.Deg2Rad), 0) * OwnerGroup.SightRange * 0.3f);
            for (int i = 0; i < pointsDensity; i++)
            {
                Vector3 currentHeading = Vector3.RotateTowards(transform.forward, -transform.forward, -(SightAngle / 2 * Mathf.Deg2Rad) + (i * anglePerPoint), 0);
                Vector3 newPoint = transform.position + currentHeading * OwnerGroup.SightRange;

                newPoint.y = Globals.GetTerrain.SampleHeight(newPoint) + 0.1f;

                points.Add(newPoint);
            }
            points.Add(transform.position + Vector3.RotateTowards(transform.forward, -transform.forward, (SightAngle / 2 * Mathf.Deg2Rad), 0) * OwnerGroup.SightRange * 0.3f);

            lrend.positionCount = pointsDensity + 2;
            lrend.SetPositions(points.ToArray());
        }
    }

    private void fadeOutRangeHighlight()
    {
        LineRenderer lrend = RangeIndicator.GetComponent<LineRenderer>();
        float newWidth = lrend.endWidth;
        newWidth *= 0.9f;
        lrend.endWidth = newWidth;
        lrend.startWidth = newWidth;

        if (lrend.endWidth > 0.1f)
        {
            Invoke("fadeOutRangeHighlight", 0.03f);
        }
        else
        {
            RangeIndicator.SetActive(false);
        }
    }

    private void fadeInRangeHighlight()
    {
        LineRenderer lrend = RangeIndicator.GetComponent<LineRenderer>();
        float newWidth = lrend.endWidth;
        newWidth *= 1.1f;
        lrend.endWidth = newWidth;
        lrend.startWidth = newWidth;

        if (lrend.endWidth < RangeLineWidth)
        {
            Invoke("fadeInRangeHighlight", 0.03f);
        }
    }

    public void HideRange()
    {
        if (RangeIndicator.activeSelf && !IsInvoking("fadeOutRangeHighlight"))
        {
            CancelInvoke("fadeInRangeHighlight");
            Invoke("fadeOutRangeHighlight", 0.03f);
        }
    }
    #endregion
}
