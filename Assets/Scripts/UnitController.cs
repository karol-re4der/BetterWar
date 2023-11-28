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

public class UnitController : MonoBehaviour
{
    public UnitGroupController OwnerGroup;

    [Header("Runtime")]
    public bool IsAlive = true;
    public bool InShootingPosition = false;
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


    [Header("Settings")]
    public float ProjectileAccuracy = 1f; //measured in angle deviation
    public float SightAngle = 120;
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


    [Header("References")]
    public Transform CenterPoint;
    public Transform ShootingPoint;


    public void Initialize(UnitGroupController owner, bool flag, int index)
    {
        IndividualShootingSpeed += ReloadSpeed + (ReloadSpeed * UnityEngine.Random.Range(-ReloadSpeedVariation, ReloadSpeedVariation));

        UnitIndex = index;
        OwnerGroup = owner;
        FlagBearer = flag;

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
        transform.Find("Model/Flag/Banner").GetComponent<MeshRenderer>().materials[0].color = playerColor;

        //Set flag
        if (FlagBearer)
        {
            transform.Find("Model/Flag").gameObject.SetActive(true);
            transform.Find("Model/Weapon").gameObject.SetActive(false);
        }

        SwitchAnimation("UnitIdle");
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
            if (DistanceTowardsTargetPosition != 0)
            {
                MoveTowardsTargetPosition();
            }

            if (AngleTowardsTargetPosition != 0)
            {
                RotateTowardsTargetPosition();
            }

            if (MovementMode == EUnitMovementMode.Standstill)
            {
                if (InShootingPosition)
                {
                    if (OwnerGroup.EnemyTarget != null)
                    {
                        if (ReadyToShoot)
                        {
                            if (AimedAt == null)
                            {
                                TakeAim(OwnerGroup.EnemyTarget);
                            }
                            else
                            {
                                TargetAngled = TargetUnitAngled(AimedAt);
                                if (TargetAngled)
                                {
                                    TargetInRange = TargetUnitInRange(AimedAt);
                                    if (!TargetInRange)
                                    {
                                        TakeAim(OwnerGroup.EnemyTarget);
                                    }
                                    else
                                    {
                                        TargetInSight = TargetUnitInSight(AimedAt);
                                        if (!TargetInSight)
                                        {
                                            TakeAim(OwnerGroup.EnemyTarget);
                                        }
                                        else if(OwnerGroup.IsFireAllowed())
                                        {
                                            Shoot();
                                        }
                                    }
                                }
                                else
                                {
                                    TakeAim(OwnerGroup.EnemyTarget);
                                }
                            }
                        }
                        else
                        {
                            Reload();
                        }
                    }
                }
            }
        }
    }

    public void NewMovementStarted()
    {
        AngleTowardsTargetPosition = 180;
        DistanceTowardsTargetPosition = 180;
        MovementMode = EUnitMovementMode.Formation;
    }

    private void RotateTowardsTargetPosition()
    {
        if (MovementMode == EUnitMovementMode.Formation)
            //Rotate towards direction where formation is facing, then sidestep into position
        {
            Quaternion targetRotation = Quaternion.identity;
            if (TargetFacing != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(TargetFacing);
            }
            if (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }
        }
        else if(MovementMode == EUnitMovementMode.Loose)
            //Rotate towards target position, then run
        {
            Quaternion targetRotation = Quaternion.identity;
            if (TargetFacing != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(TargetPosition - transform.position);
            }
            if (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }
        }

        Vector3 targetDir = TargetPosition - transform.position;
        AngleTowardsTargetPosition = Vector3.Angle(targetDir, transform.forward);
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
            }
            else
            {
                MovementMode = EUnitMovementMode.Loose;
            }
        }
    }

    private bool TargetUnitInRange(UnitController unit)
    {
        return Vector3.Distance(unit.transform.position, transform.position) < OwnerGroup.SightRange;
    }

    private bool TargetUnitAngled(UnitController unit)
    {
        Vector3 towardsTarget = unit.GetModelCenterPoint() - GetModelShootingPoint();
        return Vector3.Angle(towardsTarget, TargetFacing) < SightAngle / 2;
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

    private void TakeAim(UnitGroupController targetGroup)
    {
        List<UnitController> targetUnits = new List<UnitController>();
        //targetUnits = targetGroup.Units.OrderBy(x => Vector3.Distance(x.transform.position, transform.position)).Where(x => Vector3.Distance(x.transform.position, transform.position)<OwnerGroup.SightRange).ToList(); //this query it ugly please fix later
        targetUnits = targetGroup.Units.Where(x => TargetUnitAngled(x) && TargetUnitInRange(x) && TargetUnitInSight(x)).OrderBy(x => Vector3.Distance(GetModelShootingPoint(), x.GetModelCenterPoint())).ToList();
        //If enemy is bunched up, aim at front
        if (targetUnits.Count() > 10)
        {
            targetUnits = targetUnits.Take(targetUnits.Count() / 4).ToList();
        }

        //select final target
        if (targetUnits.Count() > 0)
        {
            AimedAt = targetUnits.ElementAt(Random.Range(0, targetUnits.Count()));
        }
        else
        {
            AimedAt = null;
        }
    }

    private void Shoot()
    {
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
        if (Physics.Raycast(GetModelShootingPoint(), dir, out hit, OwnerGroup.SightRange, LayerMask.GetMask("Units"), QueryTriggerInteraction.UseGlobal))
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
            Globals.GetProjectileSystem.NewProjectileAtPoint(GetModelShootingPoint(), hit.point, true);
            hit.collider.transform.parent.gameObject.GetComponent<UnitController>().RegisterHit(dir, timeToHit);
        }
        else
        {
            Globals.GetProjectileSystem.NewProjectileInDirection(GetModelShootingPoint(), dir, true);
        }

        AimedAt = null;
    }

    private void Reload()
    {
        if (ReloadProgress >= 1f)
        {
            ReadyToShoot = true;
        }
        else
        {
            ReloadProgress += IndividualShootingSpeed * Time.deltaTime;
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
        SwitchAnimation("UnitDead");
    }

    private void SwitchAnimation(string animationName)
    {
        transform.Find("Model").gameObject.GetComponent<Animator>().Play(animationName);
    }
}
