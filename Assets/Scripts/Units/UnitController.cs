using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EUnitMovementMode
{
    Formation,
    Loose,
    Standstill,
    Engaging
}

public enum EWeaponType
{
    Melee,
    Ranged,
    Flag
}

public enum EUnitType
{
    Melee,
    Ranged,
    Mounted
}

public class UnitController : MonoBehaviour
{
    public UnitGroupController OwnerGroup;

    [Header("General Stats")]
    public int EnemiesKilled = 0;
    public int AlliesKilled = 0;

    [Header("General Runtime")]
    public bool IsAlive = true;
    public bool TargetInRange = false;
    public bool TargetInSight = false;
    public bool TargetAngled = false;
    public int UnitIndex;
    public Vector3 TargetPosition;
    public Vector3 TargetFacing;
    public float AngleTowardsTargetPosition;
    public float DistanceTowardsTargetPosition;
    public float CurrentSpeed;
    public EUnitMovementMode MovementMode = EUnitMovementMode.Loose;
    public Vector2 PositionInFormation = Vector2.zero;
    public List<UnitController> EngagedBy = new List<UnitController>();
    public UnitController Engaging;

    [Header("General Settings")]
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
    public float RangeLineWidth = 4f;
    public int MaxEngagementsAllowed = 2;

    [Header("General References")]
    public Transform CenterPoint;
    public Animator AnimationController;
    public GameObject Weapon;

    [Header("General Times")]
    public System.DateTime LastRandomActionsTimestamp = System.DateTime.MinValue;
    public System.DateTime LastClearEngagementsTimestamp = System.DateTime.MinValue;
    public float RandomActionsFrequency = 10f; //In seconds


    private float _highlightPhase = 0f;

    public void Initialize(UnitGroupController owner, int index)
    {
        float animOffset = 1f/UnityEngine.Random.Range(0, 10);
        AnimationController.SetFloat("AnimationOffset", animOffset);

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
        transform.Find("Model/Body/Head").GetComponent<MeshRenderer>().materials[0].color = playerColor;
        transform.Find("Model/Body/Hand").GetComponent<MeshRenderer>().materials[0].color = playerColor;
        transform.Find("Model/Body/Torso").GetComponent<MeshRenderer>().materials[0].color = playerColor;
        GetComponent<Outline>().OutlineColor = OwnerGroup.OwnerPlayer.PlayerColor;
    }

    // Update is called once per frame
    public void Update()
    {
        if (IsAlive)
        {
            //Random
            if ((System.DateTime.Now - LastRandomActionsTimestamp).TotalMilliseconds > 1000f * RandomActionsFrequency)
            {
                if (Random.Range(0, 5) == 1)
                {
                    AnimationController.SetTrigger("LookAround");
                }
                LastRandomActionsTimestamp = System.DateTime.Now;
            }

            //Movement
            if (DistanceTowardsTargetPosition != 0)
            {
                moveTowardsTargetPosition();
            }

            if (AngleTowardsTargetPosition != 0)
            {
                rotateTowardsTargetPosition();
            }
        }
    }

    #region Movement
    public void MoveIntoPosition(Vector3 targetPosition, EUnitMovementMode mode = EUnitMovementMode.Loose)
    {
        TargetPosition = targetPosition;

        AngleTowardsTargetPosition = 180;
        DistanceTowardsTargetPosition = 180;
        MovementMode = mode;
    }

    public void ChangeFacing(Vector3 targetFacing)
    {
        TargetFacing = targetFacing;
        AngleTowardsTargetPosition = 180;
    }

    protected void rotateTowardsTargetPosition()
    {
        Quaternion targetRotation = Quaternion.identity;
        Vector3 selectedTargetFacing = Vector3.zero;

        if (MovementMode == EUnitMovementMode.Formation || MovementMode == EUnitMovementMode.Standstill || MovementMode == EUnitMovementMode.Engaging)
            //Rotate towards direction where formation is facing, then sidestep into position
        {
            selectedTargetFacing = TargetFacing;
            targetRotation = Quaternion.LookRotation(TargetFacing);
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

    protected void moveTowardsTargetPosition()
    {
        if (TargetPosition != transform.position)
        {
            //Get speed
            float currentMaxSpeed = 0;
            if (MovementMode == EUnitMovementMode.Formation || MovementMode==EUnitMovementMode.Engaging) {
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
            else if (MovementMode == EUnitMovementMode.Loose && DistanceTowardsTargetPosition < MovementModeChangeDistance)
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
            DistanceTowardsTargetPosition = 0;
            MovementMode = EUnitMovementMode.Standstill;
            AnimationController.SetFloat("MovementSpeed", 0);
        }
    }
    #endregion

    #region Targeting
    protected bool targetUnitInRange(UnitController unit)
    {
        return Vector3.Distance(unit.transform.position, transform.position) < OwnerGroup.SightRange;
    }

    protected bool targetUnitAngled(UnitController unit)
    {
        return angleToTarget(unit) < SightAngle / 2;
    }

    protected float angleToTarget(UnitController unit)
    {
        Vector3 towardsTarget = unit.GetModelCenterPoint() - GetModelCenterPoint();
        return Vector3.Angle(towardsTarget, transform.forward);
    }

    protected bool targetUnitInSight(UnitController unit)
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

    protected List<UnitGroupController> getTargets()
    {
        if (OwnerGroup.TargetEnemyOverride!=null)
        {
            List < UnitGroupController > targets = new List<UnitGroupController>();
            targets.Add(OwnerGroup.TargetEnemyOverride);
            return targets;
        }

        return OwnerGroup.EnemiesInRange;
    }
    #endregion

    public Vector3 GetModelCenterPoint()
    {
        return CenterPoint.position;
    }

    #region Unit hit
    public void RegisterHit(Vector3 knockbackDirection, float timeToHit)
    {
        Invoke("onDeath", timeToHit);
    }
    private void onDeath()
    {
        IsAlive = false;
        OwnerGroup.RemoveUnit(this);
        OwnerGroup.ReformNeeded = true;
        Disengage();

        foreach(Collider collider in gameObject.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        AnimationController.SetTrigger("Die");
    }
    #endregion

    #region Highlights
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
    #endregion

    #region Engaging
    public void SetAsEngagementTarget(UnitController engagingUnit)
    {
        if (!EngagedBy.Contains(engagingUnit))
        {
            EngagedBy.Add(engagingUnit);
        }

        //if (Engaging==null && engagingUnit.CanBeEngagedBy(this))
        //{
        //    engagingUnit.SetAsEngagementTarget(this);
        //}
    }

    public bool CanBeEngagedBy(UnitController engagingUnit)
    {
        if (EngagedBy.Contains(engagingUnit))
        {
            return true;
        }
        return EngagedBy.Count() < MaxEngagementsAllowed;
    }

    public virtual void Disengage()
    {
        UnitController target = Engaging;
        if (target != null)
        {
            target.RefreshEngagements();
        }
        Engaging = null;
    }

    public void RefreshEngagements()
    {
        int i = 0;
        while (i < EngagedBy.Count())
        {
            if (!EngagedBy.ElementAt(i).IsAlive)
            {
                EngagedBy.RemoveAt(i);
            }
            else if (EngagedBy.ElementAt(i).Engaging != this)
            {
                EngagedBy.RemoveAt(i);
            }
            i++;
        }

        AnimationController.SetBool("Engaging", EngagedBy.Count()>0 || Engaging!=null);
    }

    public bool EngagementIsValid()
    {
        bool state = true;
        if (Engaging == null)
        {
            state = false;
        }
        else if (!Engaging.IsAlive)
        {
            Disengage();
            state = false;
        }

        return state;
    }
    #endregion

    #region Formation
    public virtual bool TriesToKeepFormation()
    {
        return true;
    }

    public UnitController GetUnitOnLeft()
    {
        int neighbourX = (int)PositionInFormation.x-1;
        if (neighbourX>0 && neighbourX< OwnerGroup.Formation.Columns) {
            return OwnerGroup.Formation.UnitsAttached[neighbourX, (int)PositionInFormation.y];
        }
        return null;
    }

    public UnitController GetUnitOnRight()
    {
        int neighbourX = (int)PositionInFormation.x+1;
        if (neighbourX < OwnerGroup.Formation.Columns)
        {
            return OwnerGroup.Formation.UnitsAttached[neighbourX, (int)PositionInFormation.y];
        }
        return null;
    }

    public UnitController GetUnitInFront()
    {
        int neighbourY = (int)PositionInFormation.y - 1;
        if (neighbourY >= 0 && neighbourY < OwnerGroup.Formation.Rows)
        {
            return OwnerGroup.Formation.UnitsAttached[(int)PositionInFormation.x, neighbourY];
        }
        return null;
    }

    public UnitController GetUnitInBack()
    {
        int neighbourY = (int)PositionInFormation.y + 1;
        if (neighbourY < OwnerGroup.Formation.Rows)
        {
            return OwnerGroup.Formation.UnitsAttached[(int)PositionInFormation.x, neighbourY];
        }
        return null;
    }

    public Vector3 FindDesiredEngagementPositionInFormation()
    {
        Vector3 pos = Vector3.zero;

        List<UnitController> neighbours = new List<UnitController>();
        neighbours.Add(GetUnitOnLeft());
        neighbours.Add(GetUnitOnRight());
        neighbours.Add(GetUnitInFront());
        neighbours.Add(GetUnitInBack());
        float desiredDistance = Globals.GetFormationGroupController.GetUnitsMargin();

        neighbours.RemoveAll(x=>x==null);
        
        if (neighbours.Count()==4)
        {
            foreach (UnitController unit in neighbours)
            {
                pos += unit.transform.position;
            }
        }
        else if(neighbours.Count()>0)
        {
            foreach(UnitController unit in neighbours)
            {
                pos += Vector3.MoveTowards(unit.transform.position, transform.position, desiredDistance);
            }
        }
        else
        {
            return transform.position;
        }

        pos /= neighbours.Count();
        return (pos==Vector3.zero) ? transform.position : pos;
    }
    #endregion
}