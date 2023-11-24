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
                        formationsInUse.Clear();
                        formationsInUse = UnitFormation.GetFormationsToUse(UnitsSelected.Count());
                        Debug.Log(formationsInUse.Count());
                        int i = 0;
                        foreach(UnitFormation form in formationsInUse)
                        {
                            form.Initialize(UnitsSelected.ElementAt(i).CurrentSize, 0.5f, clickPos);
                            i++;
                        }
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        Vector3 cachedLeftAnchor = formationsInUse.First().LeftAnchor;
                        float formationMargin = 2f;
                        float totalWeight = formationsInUse.Sum(x => x.MaxFrontage)+(formationsInUse.Count()-1)*formationMargin;
                        float totalWidth = Mathf.Min(Vector3.Distance(formationsInUse.First().LeftAnchor, clickPos), totalWeight);
                        float frontageRequired = formationsInUse.Sum(x => x.MinFrontage) + (formationsInUse.Count() - 1) * formationMargin;
                        float frontageInUse = 0f;

                        foreach (UnitFormation form in formationsInUse)
                        {
                            if (totalWidth > frontageRequired)
                            {
                                float formationPart = form.MaxFrontage / totalWeight;
                                float rightAnchorShift = (formationPart * totalWidth + frontageInUse);

                                Vector3 rightAnchor = Vector3.Lerp(formationsInUse.First().LeftAnchor, clickPos, rightAnchorShift / totalWidth);

                                form.Recompute(rightAnchor, frontageInUse);

                                form.Visualise();

                                frontageInUse = rightAnchorShift + formationMargin;
                            }
                            else
                            {
                                form.Hide();
                            }
                        }
                        Debug.Log("");
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (UnitsSelected.Count() > 0)
                    {
                        int i = 0;
                        foreach (UnitFormation form in formationsInUse)
                        {
                            //if (form.Computed)
                            //{
                            if (!form.IsHidden())
                            {
                                UnitsSelected.ElementAt(i).SetFormation(form);
                                form.Hide();
                            }
                            //}
                            //else
                            //{
                            //    form.Recompute();
                            //    UnitsSelected.First().SetFormation(form);
                            //    form.Hide();
                            //}
                            i++;
                        }
                        formationsInUse.Clear();
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
        UnitsSelected.Clear();

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
        UnitsSelected.Clear();

        RefreshSelectionOnInterface();
    }

    public void SelectUnitByIcon(UnitIconController icon)
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelected();
        }
        UnitsSelected.Add(icon.UnitGroup);

        RefreshSelectionOnInterface();
    }

    public void SelectUnitByClick(UnitController unit)
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelected();
        }
        UnitsSelected.Add(unit.OwnerGroup);
        Globals.GetInterface.GetUnitIcon(unit).SetSelected(true);

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
