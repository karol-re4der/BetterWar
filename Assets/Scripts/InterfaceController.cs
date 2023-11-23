using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InterfaceController : MonoBehaviour
{
    public Transform unitSpace;
    public GameObject UnitsPanel;
    public GameObject UnitIconPrefab;

    public BetterToggle Toggle_MovementType;
    public BetterToggle Toggle_FiringType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void RefreshUnitActions(UnitGroupController unitSelected)
    {
        if (unitSelected)
        {
            Toggle_FiringType.gameObject.SetActive(true);
            if (unitSelected.FiringMode == EUnitFiringMode.Salvo)
            {
                Toggle_FiringType.Enable();
            }
            else
            {
                Toggle_FiringType.Disable();
            }
        }
        else
        {
            Toggle_FiringType.gameObject.SetActive(false);
        }
    }

    public void CreateUnitIcon(UnitGroupController unitGroup)
    {
        GameObject newIcon = Instantiate(UnitIconPrefab, UnitsPanel.transform);
        newIcon.GetComponent<UnitIconController>().UnitGroup = unitGroup;
    }

    public UnitIconController GetUnitIcon(UnitController unit)
    {
        return GetUnitIcon(unit.OwnerGroup);
    }

    public UnitIconController GetUnitIcon(UnitGroupController unitGroup)
    {
        foreach(Transform icon in UnitsPanel.transform)
        {
            if (icon.gameObject.GetComponent<UnitIconController>().UnitGroup == unitGroup)
            {
                return icon.gameObject.GetComponent<UnitIconController>();
            }
        }
        return null;
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
        if(UnitsSelected.Count()>0)
        {
            RefreshUnitActions(UnitsSelected.First());
        }
        else
        {
            RefreshUnitActions(null);
        }
    }
}
