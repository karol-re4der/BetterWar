using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InterfaceController : MonoBehaviour
{
    public Transform unitSpace;
    public GameObject UnitsPanel;
    public GameObject ActionsPanel;
    public GameObject UnitIconPrefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Unit Icons
    public List<UnitIconController> GetAllUnitIcons()
    {
        return GetComponentsInChildren<UnitIconController>().ToList();
    }

    public int GetIconIndex(UnitIconController icon)
    {
        return GetAllUnitIcons().IndexOf(icon);
    }

    public void RecreateUnitIcons()
    {
        foreach(Transform icon in UnitsPanel.transform)
        {
            GameObject.Destroy(icon.gameObject);
        }
        foreach(UnitGroupController group in PlayerController.GetActivePlayer().UnitGroups)
        {
            CreateUnitIcon(group);
        }
    }

    public UnitIconController GetUnitIcon(UnitController unit)
    {
        return GetUnitIcon(unit.OwnerGroup);
    }

    public UnitIconController GetUnitIcon(UnitGroupController unitGroup)
    {
        foreach (Transform icon in UnitsPanel.transform)
        {
            if (icon.gameObject.GetComponent<UnitIconController>().UnitGroup == unitGroup)
            {
                return icon.gameObject.GetComponent<UnitIconController>();
            }
        }
        return null;
    }


    public void CreateUnitIcon(UnitGroupController unitGroup)
    {
        GameObject newIcon = Instantiate(UnitIconPrefab, UnitsPanel.transform);
        newIcon.GetComponent<UnitIconController>().UnitGroup = unitGroup;
    }

    public void RefreshSelectionIcons(List<UnitGroupController> UnitsSelected)
    {
        foreach (UnitIconController unitIcon in UnitsPanel.GetComponentsInChildren<UnitIconController>())
        {
            if (UnitsSelected.Contains(unitIcon.UnitGroup))
            {
                unitIcon.SetSelected(true);
            }
            else
            {
                unitIcon.SetSelected(false);
            }
        }
        if (UnitsSelected.Count() > 0)
        {
            RefreshUnitActions(UnitsSelected.First());
        }
        else
        {
            RefreshUnitActions(null);
        }
    }

    #endregion

    #region Unit actions
    public void RefreshUnitActions(UnitGroupController unitSelected)
    {
        if (unitSelected)
        {
            foreach (UnitAction action in unitSelected.UnitActions)
            {
                foreach (Transform actionIcon in ActionsPanel.transform)
                {
                    if (actionIcon.gameObject.name.Contains(action.ActionName))
                    {
                        if (actionIcon.gameObject.name.EndsWith(action.GetCurrentState()))
                        {
                            actionIcon.gameObject.GetComponent<BetterToggle>().Enable();
                        }
                        else
                        {
                            actionIcon.gameObject.GetComponent<BetterToggle>().Disable();
                        }
                        actionIcon.gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            foreach(Transform actionIcon in ActionsPanel.transform)
            {
                actionIcon.gameObject.SetActive(false);
            }
        }
    }

    public void ToggleUnitAction(Button button)
    {
        Globals.GetUserControls.ToggleUnitAction(button.gameObject.name);
        RefreshUnitActions(Globals.GetUserControls.UnitsSelected.First());
    }
    #endregion

}
