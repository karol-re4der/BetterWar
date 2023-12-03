using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class UserControlsController : MonoBehaviour
{
    public GameObject FormationPrefab;

    public InterfaceController userInterface;

    public BetterToggle debugToggle1;
    public BetterToggle debugToggle2;
    public BetterToggle debugToggle3;

    private Vector3 _selectionStartPoint = Vector3.zero;
    private DateTime _mouseDownTime = DateTime.MinValue;

    public List<UnitFormation> FormationsInUse = new List<UnitFormation>();
    public List<UnitGroupController> UnitsSelected = new List<UnitGroupController>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 clickPos = GetClickPositionOnMap();
        UnitController unitClicked = GetUnitClicked();
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (clickPos != Vector3.zero)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (debugToggle1.isActive)
                    {
                        PlayerController.GetActivePlayer().SpawnUnit(clickPos);
                    }
                    else if (unitClicked!=null)
                    {
                        if(unitClicked.OwnerGroup.OwnerPlayer == Globals.GetActivePlayer)
                        {
                            SelectUnitByClick(unitClicked);
                        }
                    }
                    else
                    {
                        ClearSelected();
                    }
                }
                #region Selection
                if (Input.GetMouseButtonDown(1))
                {
                    _mouseDownTime = DateTime.Now;
                    if (UnitsSelected.Count() > 0)
                    {
                        _selectionStartPoint = clickPos;
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        bool result = Globals.GetFormationGroupController.Reform(_selectionStartPoint, clickPos, UnitsSelected);
                        if (result)
                        {
                            Globals.GetFormationGroupController.Visualise();
                        }
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        #region targeting
                        if (unitClicked != null && (DateTime.Now - _mouseDownTime).TotalMilliseconds < 200)
                        {
                            if (unitClicked.OwnerGroup.OwnerPlayer != Globals.GetActivePlayer)
                            {
                                OverrideTarget(unitClicked);
                            }
                        }
                        #endregion

                        #region selection
                        else if ((DateTime.Now - _mouseDownTime).TotalMilliseconds < 200)
                        {
                            Globals.GetFormationGroupController.ShiftFormations(clickPos, UnitsSelected);
                        }
                        else if(Globals.GetFormationGroupController.IsValid())
                        {
                            Globals.GetFormationGroupController.SendToUnits(UnitsSelected);
                            Globals.GetFormationGroupController.Hide();
                        }
                        #endregion
                    }
                }
                #endregion
            }
        }
    }

    public Vector3 GetClickPositionOnMap()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Map"), QueryTriggerInteraction.UseGlobal));
        {
            if (hit.collider!=null && hit.collider.gameObject.name.Equals("Terrain"))
            {
                Vector3 hitPoint = hit.point;
                return hitPoint;
            }

        }
        return Vector3.zero;
    }

    public UnitController GetUnitClicked()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Units"), QueryTriggerInteraction.UseGlobal));
        {
            if (hit.collider!=null)
            {
                UnitController unitClicked = hit.collider.transform.parent.gameObject.GetComponent<UnitController>();
                return unitClicked;
            }

        }
        return null;
    }

    public void SwitchPlayer()
    {
        ClearSelected();

        if (debugToggle2.isActive)
        {
            PlayerController.SwitchPlayer(PlayerController.GetPlayers().Find(x => x.PlayerName.Equals("Default Player")));
        }
        else
        {
            PlayerController.SwitchPlayer(PlayerController.GetPlayers().Find(x => x.PlayerName.Equals("Default AI")));
        }
    }

    #region Targeting
    public void OverrideTarget(UnitController unit)
    {
        foreach(UnitGroupController group in UnitsSelected)
        {
            group.TargetEnemyOverride = unit.OwnerGroup;
            group.ClearUnitTargets();
        }
    }
    #endregion

    #region Action icons
    public void ToggleUnitAction(string toggleName)
    {
        foreach(UnitGroupController unit in UnitsSelected)
        {
            UnitAction relevantAction = unit.UnitActions.Find(x => toggleName.Contains(x.ActionName));
            string relevantState = relevantAction.ActionStates.Find(x => toggleName.EndsWith(x));

            if (!string.IsNullOrEmpty(relevantState))
            {
                relevantAction.ChangeState(relevantState);
            }
        }
    }
    #endregion

    #region Selection
    public void ClearSelected()
    {
        foreach(UnitGroupController group in UnitsSelected)
        {
            group.IsSelected = false;
        }
        UnitsSelected.Clear();


        RefreshSelectionOnInterface();
    }

    public void SelectUnitByIcon(UnitIconController icon)
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelected();
        }

        if (UnitsSelected.Count > 0)
        {
            int iconIndex = Globals.GetInterface.GetIconIndex(icon);
            int firstSelectedIndex = Globals.GetInterface.GetIconIndex(Globals.GetInterface.GetAllUnitIcons().First(x => UnitsSelected.Contains(x.UnitGroup)));
            for (int i = Mathf.Min(iconIndex, firstSelectedIndex); i <= Mathf.Max(iconIndex, firstSelectedIndex); i++)
            {
                UnitIconController nextIcon = Globals.GetInterface.GetAllUnitIcons().ElementAt(i);
                if (!UnitsSelected.Contains(nextIcon.UnitGroup))
                {
                    UnitsSelected.Add(nextIcon.UnitGroup);
                    nextIcon.UnitGroup.HighlightGroup();
                    nextIcon.UnitGroup.IsSelected = true;
                }
            }
        }
        else
        {
            icon.UnitGroup.IsSelected = true;
            UnitsSelected.Add(icon.UnitGroup);
            icon.UnitGroup.HighlightGroup();
        }

        RefreshSelectionOnInterface();
    }

    public void SelectUnitByClick(UnitController unit)
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelected();
        }
        if (!UnitsSelected.Contains(unit.OwnerGroup))
        {
            UnitsSelected.Add(unit.OwnerGroup);
            unit.OwnerGroup.IsSelected = true;
            unit.OwnerGroup.HighlightGroup();
            Globals.GetInterface.GetUnitIcon(unit).SetSelected(true);
        }


        RefreshSelectionOnInterface();
    }


    public void DeselectUnitByIcon(UnitIconController icon)
    {
        UnitsSelected.Remove(icon.UnitGroup);

        RefreshSelectionOnInterface();
    }

    public void RefreshSelectionOnInterface()
    {
        Globals.GetInterface.RefreshSelectionIcons(UnitsSelected);
    }
    #endregion
}
