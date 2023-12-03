using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EUnitMovementMode
{
    Formation,
    Loose,
    Standstill
}

public enum EWeaponType
{
    Melee,
    Ranged,
    Flag
}

public class UnitController : MonoBehaviour
{
    public UnitGroupController OwnerGroup;

    [Header("Stats")]
    public int EnemiesHit = 0;
    public int AlliesHit = 0;
    public int ShotsFired = 0;
    public int ShotsMissed = 0;

    [Header("Runtime")]
    public bool IsAlive = true;
    public bool TargetInRange = false;
    public bool TargetInSight = false;
    public bool TargetAngled = false;
    public float ReloadProgress = 0f;
    public int UnitIndex;
    public UnitController AimedAt;
    public bool ReadyToShoot = true;
    public Vector3 TargetPosition;
    public Vector3 TargetFacing;
    public float AngleTowardsTargetPosition;
    public float DistanceTowardsTargetPosition;
    public float CurrentSpeed;
    public EUnitMovementMode MovementMode = EUnitMovementMode.Loose;
    public float IndividualShootingSpeed = 0f;
    public Vector2 PositionInFormation = Vector2.zero;

    [Header("Settings")]
    public float ProjectileAccuracy = 1f; //measured in angle deviation
    public float SightAngle = 120; //In degrees, total angle
    public float MovementSpeedMax_Loose = 4f;
    public float MovementSpeedMax_Formation = 2f;
    public float MovementAcceleration = 1f;
    public float ClippingDistance = 3f;
    public float MovementModeChangeDistance = 10f;
    public float RotationSpeed = 1f;
    public float HeightVariation = 0.2f;
    public float WidthVariation = 0.1f;
    public float ColorVariation = 0.1f;
    public float ReloadSpeedVariation = 0.1f; //Percentage deviation from ShootingSpeed
    public bool FlagBearer = false;
    public float ReloadSpeed = 0.1f;
    public float RangeLineWidth = 4f;

    [Header("References")]
    public Transform CenterPoint;
    public Transform ShootingPoint;
    public GameObject Weapon_Ranged;
    public GameObject Weapon_Melee;
    public GameObject Weapon_Flag;
    public Animator AnimationController;
    public GameObject RangeIndicator;


    private System.DateTime _lastTryShoot = System.DateTime.Now;
    private float _tryShootEvery = 1f;
    private float _highlightPhase = 0f;



    public void Initialize(UnitGroupController owner, int index, EWeaponType weaponType)
    {
        float animOffset = 1f/UnityEngine.Random.Range(0, 10);
        AnimationController.SetFloat("AnimationOffset", animOffset);

        switch (weaponType)
        {
            case EWeaponType.Ranged:
                Weapon_Ranged.SetActive(true);
                break;
            case EWeaponType.Melee:
                Weapon_Melee.SetActive(true);
                break;
            case EWeaponType.Flag:
                Weapon_Flag.SetActive(true);
                break;
        }

        IndividualShootingSpeed += ReloadSpeed + (ReloadSpeed * UnityEngine.Random.Range(-ReloadSpeedVariation, ReloadSpeedVariation));

        UnitIndex = index;
        OwnerGroup = owner;

        //Set scale
        Vector3 newScale = transform.localScale;

        newScale.y = newScale.y + Random.Range(0, newScale.y * HeightVariation) - (newScale.y * HeightVariation) / 2;
        newScale.x = newScale.x + Random.Range(0, newScale.x * WidthVariation) - (newScale.y * WidthVariation) / 2;
        newScale.z = newScale.x;

        transform.localScale = newScale;

        //Set color
        Color playerColor = OwnerGroup.OwnerPlayer.PlayerColor;
        playerColor = new Color(playerColor.r + Random.Range(-ColorVariation / 2, ColorVariation / 2), playerColor.g + Random.Range(-ColorVariation / 2, ColorVariation / 2), playerColor.b + Random.Range(-ColorVariation / 2, ColorVariation / 2));
        //transform.Find("Model").GetComponent<MeshRenderer>().materials[0].color = playerColor;
        transform.Find("Model/Weapon/Flag/Banner").GetComponent<MeshRenderer>().materials[0].color = playerColor;
        GetComponent<Outline>().OutlineColor = OwnerGroup.OwnerPlayer.PlayerColor;

        //Set flag
        if (FlagBearer)
        {
            transform.Find("Model/Flag").gameObject.SetActive(true);
            transform.Find("Model/Weapon").gameObject.SetActive(false);
        }


    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (IsAlive)
        {
            //Movement
            if (DistanceTowardsTargetPosition != 0)
            {
                MoveTowardsTargetPosition();
            }

            if (AngleTowardsTargetPosition != 0)
            {
                RotateTowardsTargetPosition();
            }
            else if (AimedAt != null)
            {
                if(AngleToTarget(AimedAt)>10){
                    AngleTowardsTargetPosition = 180;
                }
            }

            //Shooting
            if ((System.DateTime.Now - _lastTryShoot).TotalMilliseconds > _tryShootEvery*1000)
            {
                TryShoot();
                _lastTryShoot = System.DateTime.Now;

                //Random idle animation in here for optimalization reasons only
                if(Random.Range(0, 1000) == 1)
                {
                    AnimationController.SetTrigger("LookAround");
                }
            }
            TryReload();

            //Show range if selected
            if(OwnerGroup.IsSelected && OwnerGroup.Formation.IsShootingPosition(PositionInFormation))
            {
                HighlightRange();
            }
            else
            {
                HideRange();
            }
        }
    }

    private void TryShoot()
    {
        if (MovementMode == EUnitMovementMode.Standstill)
        {
            if (OwnerGroup.Formation.IsShootingPosition(PositionInFormation))
            {
                if (OwnerGroup.EnemiesInRange != null)
                {
                    if (ReadyToShoot)
                    {
                        if (AimedAt == null)
                        {
                            TakeAim(getTargets());
                        }
                        else
                        {
                            TargetAngled = TargetUnitAngled(AimedAt);
                            if (TargetAngled)
                            {
                                TargetInRange = TargetUnitInRange(AimedAt);
                                if (!TargetInRange)
                                {
                                    TakeAim(getTargets());
                                }
                                else
                                {
                                    TargetInSight = TargetUnitInSight(AimedAt);
                                    if (!TargetInSight)
                                    {
                                        TakeAim(getTargets());
                                    }
                                    else if (OwnerGroup.IsFireAllowed())
                                    {
                                        Shoot();
                                    }
                                }
                            }
                            else
                            {
                                TakeAim(getTargets());
                            }
                        }
                    }
                    
                }
            }
        }
    }

    public void NewMovementStarted(EUnitMovementMode mode = EUnitMovementMode.Loose)
    {
        AngleTowardsTargetPosition = 180;
        DistanceTowardsTargetPosition = 180;
        MovementMode = mode;
    }

    private void RotateTowardsTargetPosition()
    {
        Quaternion targetRotation = Quaternion.identity;
        Vector3 selectedTargetFacing = Vector3.zero;

        if (MovementMode == EUnitMovementMode.Formation || MovementMode == EUnitMovementMode.Standstill)
            //Rotate towards direction where formation is facing, then sidestep into position
        {
            selectedTargetFacing = (AimedAt!=null) ? AimedAt.transform.position - transform.position : TargetFacing;
            targetRotation = Quaternion.LookRotation(selectedTargetFacing);
        }
        else if(MovementMode == EUnitMovementMode.Loose)
            //Rotate towards target position, then run
        {
            selectedTargetFacing = TargetPosition - transform.position;
            targetRotation = Quaternion.LookRotation(selectedTargetFacing);
        }

        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        AngleTowardsTargetPosition = Vector3.Angle(selectedTargetFacing, transform.forward);
    }

    private void MoveTowardsTargetPosition()
    {
        if (TargetPosition != transform.position)
        {
            //Get speed
            float currentMaxSpeed = 0;
            if (MovementMode == EUnitMovementMode.Formation) {
                currentMaxSpeed = MovementSpeedMax_Formation;
            }
            else if (MovementMode == EUnitMovementMode.Loose)
            {
                currentMaxSpeed = MovementSpeedMax_Loose;
            }
            CurrentSpeed = Mathf.Min(currentMaxSpeed, CurrentSpeed + MovementAcceleration * Time.deltaTime);

            //Move
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, CurrentSpeed*Time.deltaTime);

            //Get distance and switch mode
            DistanceTowardsTargetPosition = Vector3.Distance(transform.position, TargetPosition);
            if (DistanceTowardsTargetPosition < ClippingDistance)
            {
                MovementMode = EUnitMovementMode.Standstill;
                CurrentSpeed = 0;
            }
            else if (DistanceTowardsTargetPosition < MovementModeChangeDistance)
            {
                MovementMode = EUnitMovementMode.Formation;
                CurrentSpeed = MovementSpeedMax_Formation;
                AngleTowardsTargetPosition = 180;
            }
            else
            {
                //MovementMode = EUnitMovementMode.Loose;
            }

            AnimationController.SetFloat("MovementSpeed", currentMaxSpeed > 0 ? CurrentSpeed / currentMaxSpeed : 0);
            //AnimationController.SetBool("IsAiming", false);
        }
        else
        {
            MovementMode = EUnitMovementMode.Standstill;
        }
    }

    private bool TargetUnitInRange(UnitController unit)
    {
        return Vector3.Distance(unit.transform.position, transform.position) < OwnerGroup.SightRange;
    }

    private bool TargetUnitAngled(UnitController unit)
    {
        return AngleToTarget(unit) < SightAngle / 2;
    }

    private float AngleToTarget(UnitController unit)
    {
        Vector3 towardsTarget = unit.GetModelCenterPoint() - GetModelCenterPoint();
        return Vector3.Angle(towardsTarget, TargetFacing);
    }

    private bool TargetUnitInSight(UnitController unit)
    {
        RaycastHit hit;
        if (Physics.Raycast(GetModelCenterPoint(), unit.GetModelCenterPoint()-GetModelCenterPoint(), out hit, OwnerGroup.SightRange, LayerMask.GetMask("Units"), QueryTriggerInteraction.UseGlobal))
        {
            if (hit.collider != null)
            {
                if (hit.collider.transform.parent.gameObject.GetComponent<UnitController>().OwnerGroup.OwnerPlayer!=OwnerGroup.OwnerPlayer)
                {
                    return true;
                }
            }
        }

        return false;

    }

    private List<UnitGroupController> getTargets()
    {
        if (OwnerGroup.TargetEnemyOverride!=null)
        {
            List < UnitGroupController > targets = new List<UnitGroupController>();
            targets.Add(OwnerGroup.TargetEnemyOverride);
            return targets;
        }

        return OwnerGroup.EnemiesInRange;
    }

    private void TakeAim(List<UnitGroupController> potentialTargets)
    {
        Globals.GetStats.RegisterEvent("UnitTakeAim", 1);

        List<UnitController> targetUnits = new List<UnitController>();
        potentialTargets.ForEach(x => targetUnits.AddRange(x.Units));
        targetUnits = targetUnits.Where(x => TargetUnitAngled(x) && TargetUnitInRange(x) && TargetUnitInSight(x)).OrderBy(x => Vector3.Distance(GetModelShootingPoint(), x.GetModelCenterPoint())).ToList();
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

    private void Shoot()
    {
        Globals.GetStats.RegisterEvent("UnitShoot", 1);

        //Reset shooting variables
        ReadyToShoot = false;
        ReloadProgress = 0;

        //Compute path
        RaycastHit hit;

        //Get direction, then apply deviation
        Vector3 dir = (AimedAt.GetModelCenterPoint() - GetModelCenterPoint()).normalized;
        dir = DeviateShotDirection(dir, 0, ProjectileAccuracy);

        //Shoot ray
        bool somethingHit = false;
        float timeToHit = 0f;
        if (Physics.Raycast(GetModelShootingPoint(), dir, out hit, OwnerGroup.ProjectileEffectiveRange, LayerMask.GetMask("Units"), QueryTriggerInteraction.UseGlobal))
        {
            if (hit.collider != null)
            {
                somethingHit = true;
                timeToHit = Globals.GetProjectileSystem.GetTimeToHit(GetModelShootingPoint(), hit.point);
            }
        }

        //Shoot
        if (somethingHit)
        {
            if (hit.collider.transform.parent.gameObject.GetComponent<UnitController>().OwnerGroup.OwnerPlayer != OwnerGroup.OwnerPlayer){
                EnemiesHit++;
            }
            else
            {
                AlliesHit++;
            }

            Globals.GetProjectileSystem.NewProjectileAtPoint(GetModelShootingPoint(), hit.point, true);
            hit.collider.transform.parent.gameObject.GetComponent<UnitController>().RegisterHit(dir, timeToHit);
        }
        else
        {
            ShotsMissed++;
            Globals.GetProjectileSystem.NewProjectileInDirection(GetModelShootingPoint(), dir, true);
        }

        ShotsFired++;
        AimedAt = null;
    }

    private void TryReload()
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

    public Vector3 GetModelCenterPoint()
    {
        return CenterPoint.position;
    }

    public Vector3 GetModelShootingPoint()
    {
        return ShootingPoint.position;
    }

    private Vector3 DeviateShotDirection(Vector3 dir, float min, float max)
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

        return dir+noise;
    }

    public void RegisterHit(Vector3 knockbackDirection, float timeToHit)
    {
        Invoke("OnProjectileHit", timeToHit);
    }
    private void OnProjectileHit()
    {
        IsAlive = false;
        OwnerGroup.RemoveUnit(this);
        OwnerGroup.ReformNeeded = true;

        foreach(Collider collider in gameObject.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        AnimationController.SetTrigger("Die");
    }

    #region highlights
    public void HighlightUnit()
    {
        GetComponent<Outline>().enabled = true;
        _highlightPhase = 0;
        Invoke("fadeOutUnitHighlight", 0.033f);
    }

    private void fadeOutUnitHighlight()
    {
        if (_highlightPhase<180)
        {
            _highlightPhase += (_highlightPhase < 9) ? 8 : 4;
            GetComponent<Outline>().OutlineWidth = Mathf.Sin(Mathf.Deg2Rad*_highlightPhase)*4;
            Invoke("fadeOutUnitHighlight", 0.033f);
        }
        else
        {
            GetComponent<Outline>().OutlineWidth = 0;
            GetComponent<Outline>().enabled = false;
        }
    }

    public void HighlightRange()
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
        else if (!IsInvoking("fadeInRangeHighlight") && lrend.endWidth<RangeLineWidth)
        {
            CancelInvoke("fadeOutRangeHighlight");
            Invoke("fadeInRangeHighlight", 0.03f);
        }

        float anglePerPoint = SightAngle / pointsDensity * Mathf.Deg2Rad;

        List<Vector3> points = new List<Vector3>();
        points.Add(transform.position + Vector3.RotateTowards(transform.forward, -transform.forward, -(SightAngle / 2 * Mathf.Deg2Rad), 0) * OwnerGroup.SightRange * 0.3f);
        for (int i = 0; i < pointsDensity; i++)
        {
            Vector3 currentHeading = Vector3.RotateTowards(transform.forward, -transform.forward, -(SightAngle / 2 * Mathf.Deg2Rad) +(i*anglePerPoint), 0);
            Vector3 newPoint = transform.position + currentHeading*OwnerGroup.SightRange;

            newPoint.y = Globals.GetTerrain.SampleHeight(newPoint)+0.1f;

            points.Add(newPoint);
        }
        points.Add(transform.position + Vector3.RotateTowards(transform.forward, -transform.forward, (SightAngle / 2 * Mathf.Deg2Rad), 0) * OwnerGroup.SightRange * 0.3f);

        lrend.positionCount = pointsDensity+2;
        lrend.SetPositions(points.ToArray());
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