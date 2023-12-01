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

    private Vector3 selectionStartPoint = Vector3.zero;

    public List<UnitFormation> formationsInUse = new List<UnitFormation>();
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
                    if (UnitsSelected.Count() > 0)
                    {
                        selectionStartPoint = clickPos;
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        Globals.GetFormationGroupController.Reform(selectionStartPoint, clickPos, UnitsSelected);
                        Globals.GetFormationGroupController.Visualise();
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        Globals.GetFormationGroupController.SendToUnits(UnitsSelected);
                        Globals.GetFormationGroupController.Hide();
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

    #region Action icons
    public void Toggle_FiringMode()
    {
        if (UnitsSelected.Count() == 1)
        {
            if (UnitsSelected.First().FiringMode != EUnitFiringMode.Salvo)
            {
                UnitsSelected.First().FiringMode = EUnitFiringMode.Salvo;
            }
            else
            {
                UnitsSelected.First().FiringMode = EUnitFiringMode.AtWill;
            }
            Globals.GetInterface.RefreshUnitActions(UnitsSelected.First());
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
